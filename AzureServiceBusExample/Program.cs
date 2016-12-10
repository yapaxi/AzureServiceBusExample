using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Messages;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Config;
using AzureServiceBusExample.Processing;
using AzureServiceBusExample.Processing.Handlers;
using AzureServiceBusExample.Storages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample
{
    public class Program
    {
        static Program()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var busConnectionString = File.ReadAllText(Path.Combine(userProfile, ".connectionStrings", "bus.key"));

            var nsManager = NamespaceManager.CreateFromConnectionString(busConnectionString);

            var builder = new ContainerBuilder();

            RegisterMessageQueue<AmazonOrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageQueue<JetOrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageQueue<R1OrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageTopic<ShippedOrder>(busConnectionString, nsManager, builder, e => e.MarketplaceName);

            CreateSubsctiptionByPropertyValue<ShippedOrder, AmazonShippedOrderHandler>(
                busConnectionString, nsManager, builder,
                e => e.MarketplaceName, AmazonOrderRequestHandler.MarketplaceName);

            CreateSubsctiptionByPropertyValue<ShippedOrder, JetShippedOrderHandler>(
                busConnectionString, nsManager, builder,
                e => e.MarketplaceName, JetOrderRequestHandler.MarketplaceName);

            // mm -> r1
            builder.RegisterType<AmazonOrderRequestHandler>().As<IMessageHandler<AmazonOrderRequest, R1OrderRequest>>();
            builder.RegisterType<JetOrderRequestHandler>().As<IMessageHandler<JetOrderRequest, R1OrderRequest>>();
            builder.RegisterType<InputOutputMessageQueueProcessor<AmazonOrderRequest, R1OrderRequest>>().As<IMessageProcessor>();
            builder.RegisterType<InputOutputMessageQueueProcessor<JetOrderRequest, R1OrderRequest>>().As<IMessageProcessor>();

            // r1 -> r1
            builder.RegisterType<R1OrderRequestHandler>().As<IMessageHandler<R1OrderRequest>>();
            builder.RegisterType<InputMessageQueueProcessor<R1OrderRequest>>().As<IMessageProcessor>();

            // r1 -> customers
            builder.RegisterType<AmazonShippedOrderHandler>();
            builder.RegisterType<JetShippedOrderHandler>();
            
            builder.RegisterInstance(new Storage()).As<IStorage>();

            var killAllTokenSource = new CancellationTokenSource();

            builder.RegisterInstance(killAllTokenSource);

            _killAllTokenSource = killAllTokenSource;
            _container = builder.Build();
        }

        private static void RegisterMessageQueue<TMessage>(string busConnectionString, NamespaceManager nsManager, ContainerBuilder builder)
        {
            var type = typeof(TMessage);
            if (!nsManager.QueueExists(type.FullName))
            {
                Console.WriteLine($"Creating queue for {typeof(TMessage).Name}");
                nsManager.CreateQueue(type.FullName);
            }
            else
            {
                Console.WriteLine($"Recreating queue for {typeof(TMessage).Name}");
                nsManager.DeleteQueue(type.FullName);
                nsManager.CreateQueue(type.FullName);
            }
            builder.Register(e => new MessageQueue<TMessage>(QueueClient.CreateFromConnectionString(busConnectionString, type.FullName)))
                .As<IMessageDestination<TMessage>>()
                .As<IMessageSource<TMessage>>()
                .AsSelf();
        }

        private static void RegisterMessageTopic<TMessage>(
            string busConnectionString, NamespaceManager nsManager, ContainerBuilder builder,
            Expression<Func<TMessage, string>> selector)
            where TMessage : ITopicFilteredMessage
        {
            var type = typeof(TMessage);
            var propertyName = GetSelectorPropertyName(selector);
            if (!nsManager.TopicExists(type.FullName))
            {
                Console.WriteLine($"Creating topic for {typeof(TMessage).Name}");
                nsManager.CreateTopic(type.FullName);
            }
            else
            {
                Console.WriteLine($"Recreating topic for {typeof(TMessage).Name}");
                nsManager.DeleteTopic(type.FullName);
                nsManager.CreateTopic(type.FullName);
            }

            builder.Register(e => new MessageTopic<TMessage>(propertyName, TopicClient.CreateFromConnectionString(busConnectionString, type.FullName)))
                .As<IMessageDestination<TMessage>>()
                .AsSelf();
        }

        private static void CreateSubsctiptionByPropertyValue<TMessage, THandler>(
            string busConnectionString,
            NamespaceManager nsManager,
            ContainerBuilder builder,
            Expression<Func<TMessage, string>> selector,
            string value)
            where THandler : IMessageHandler<TMessage>
        {
            var topicName = typeof(TMessage).FullName;
            var propertyName = GetSelectorPropertyName(selector);
            var filter = new SqlFilter($"{propertyName} = '{value}'");

            if (nsManager.SubscriptionExists(topicName, value))
            {
                nsManager.DeleteSubscription(topicName, value);
            }

            Console.WriteLine($"Creating route to {topicName} for {value}");
            nsManager.CreateSubscription(topicName, value, filter);

            builder.Register(e => new MessageSubscription<TMessage>(SubscriptionClient.CreateFromConnectionString(busConnectionString, topicName, value)))
                .Named<MessageSubscription<TMessage>>(value)
                .As<IMessageSource<TMessage>>()
                .AsSelf();

            builder.Register(e => new InputMessageSubscriptionProcessor<TMessage, THandler>(
                handler: e.Resolve<THandler>(),
                subscription: e.ResolveNamed<MessageSubscription<TMessage>>(value),
                tokenSource: e.Resolve<CancellationTokenSource>()
            )).As<IMessageProcessor>();
        }

        private static string GetSelectorPropertyName<TMessage>(Expression<Func<TMessage, string>> selector)
        {
            var memberExpression = (MemberExpression)selector.Body;
            var propertyName = memberExpression.Member.Name;
            return propertyName;
        }

        private static readonly IContainer _container;
        private static readonly CancellationTokenSource _killAllTokenSource;

        public static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            var processors = _container.Resolve<IEnumerable<IMessageProcessor>>();

            RunOrderGenerator();

            var shipper = RunOrderShipper();

            Task.WaitAll(processors.Select(e => e.Task).Concat(new[] { shipper }).ToArray());

            Console.WriteLine("All processors closed");
        }

        private static Task RunOrderShipper()
        {
            return Task.Factory.StartNew(() => ShipRandomOrdersFromR1(
                _container.Resolve<IStorage>(),
                _container.Resolve<MessageTopic<ShippedOrder>>()),
                TaskCreationOptions.LongRunning
            );
        }

        private static void RunOrderGenerator()
        {
            var t1 = Task.Factory
                .StartNew(() =>
                    PollJetApiToQueue(
                        _container.Resolve<MessageQueue<JetOrderRequest>>(),
                        _container.Resolve<MessageQueue<AmazonOrderRequest>>()),
                    TaskCreationOptions.LongRunning
                )
                .ContinueWith(e => _killAllTokenSource.Cancel());
        }

        private static void PollJetApiToQueue(
            MessageQueue<JetOrderRequest> jetOrderRequestQueue,
            MessageQueue<AmazonOrderRequest> amazonOrderRequestQueue)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < 1000; i++)
            {
                if (_killAllTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (rnd.Next(5) == 3)
                {
                    var r1 = new JetOrderRequest() { JetVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"Received order from jet: {r1.JetVenueOrderId}");
                    jetOrderRequestQueue.SendMesage(r1).Wait();
                }
                else
                {
                    var r2 = new AmazonOrderRequest() { AmazonVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"Received order from amazon: {r2.AmazonVenueOrderId}");
                    amazonOrderRequestQueue.SendMesage(r2).Wait();
                }

                Thread.Sleep(2000);
            }
        }

        private static void ShipRandomOrdersFromR1(IStorage storage, MessageTopic<ShippedOrder> shippedOrderTopic)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            var shipped = new HashSet<string>();
            while (!_killAllTokenSource.IsCancellationRequested)
            {
                var orders = storage.GetAll<R1OrderRequest>()
                    .Result
                    .Where(e => !shipped.Contains(e.Key))
                    .Where(e => rnd.Next(5) == 2);

                foreach (var order in orders)
                {
                    Console.WriteLine($"$$$$$ shipped order {order.Key}");
                    shipped.Add(order.Key);
                    shippedOrderTopic.SendMesage(new ShippedOrder()
                    {
                        MarketplaceName = order.MarketplaceName,
                        VenueOrderLineId = order.VenueOrderId
                    }).Wait();
                }

                Thread.Sleep(100);
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _killAllTokenSource.Cancel();
            e.Cancel = true;
        }
    }
}
