using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Messages;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Processing;
using AzureServiceBusExample.Processing.Handlers;
using AzureServiceBusExample.Storages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Autofac
{
    public static class AutofacBuilder
    {
        public static IContainer Build()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var busConnectionString = File.ReadAllText(Path.Combine(userProfile, ".connectionStrings", "bus.key"));

            var nsManager = NamespaceManager.CreateFromConnectionString(busConnectionString);
            
            var builder = new ContainerBuilder();

            builder.Register(e => MessagingFactory.CreateFromConnectionString(busConnectionString))
                .SingleInstance();

            RegisterMessageQueue<AmazonOrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageQueue<JetOrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageQueue<R1OrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageTopic<ShippedOrder>(busConnectionString, nsManager, builder, e => e.MarketplaceName);

            CreateSubscriptionByPropertyValue<ShippedOrder, AmazonShippedOrderHandler>(
                busConnectionString, nsManager, builder,
                e => e.MarketplaceName, AmazonOrderRequestHandler.MarketplaceName);

            CreateSubscriptionByPropertyValue<ShippedOrder, JetShippedOrderHandler>(
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

            // r1 -> marketplace
            builder.RegisterType<AmazonShippedOrderHandler>();
            builder.RegisterType<JetShippedOrderHandler>();

            builder.RegisterInstance(new Storage()).As<IStorage>();

            var killAllTokenSource = new CancellationTokenSource();

            builder.RegisterInstance(killAllTokenSource);

            return builder.Build();
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

            builder.RegisterType<MessageQueue<TMessage>>()
                .SingleInstance()
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
            var filterPropertyName = GetSelectorPropertyName(selector);
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
            
            builder.RegisterType<MessageTopic<TMessage>>()
                .WithParameter(new TypedParameter(typeof(string), filterPropertyName))
                .SingleInstance()
                .As<IMessageDestination<TMessage>>()
                .AsSelf();
        }

        private static void CreateSubscriptionByPropertyValue<TMessage, THandler>(
            string busConnectionString,
            NamespaceManager nsManager,
            ContainerBuilder builder,
            Expression<Func<TMessage, string>> selector,
            string filterValue)
            where THandler : IMessageHandler<TMessage>
        {
            var topicName = typeof(TMessage).FullName;
            var propertyName = GetSelectorPropertyName(selector);
            var filter = new SqlFilter($"{propertyName} = '{filterValue}'");

            if (nsManager.SubscriptionExists(topicName, filterValue))
            {
                nsManager.DeleteSubscription(topicName, filterValue);
            }

            Console.WriteLine($"Creating route to {topicName} for {filterValue}");
            nsManager.CreateSubscription(topicName, filterValue, filter);

            builder
                .RegisterType<MessageSubscription<TMessage>>()
                .WithParameter(new TypedParameter(typeof(string), filterValue))
                .Named<MessageSubscription<TMessage>>(filterValue)
                .SingleInstance()
                .As<IMessageSource<TMessage>>()
                .AsSelf();

            builder.Register(e => new InputMessageSubscriptionProcessor<TMessage, THandler>(
                handler: e.Resolve<THandler>(),
                subscription: e.ResolveNamed<MessageSubscription<TMessage>>(filterValue),
                tokenSource: e.Resolve<CancellationTokenSource>()
            ))
            .SingleInstance()
            .As<IMessageProcessor>();
        }

        private static string GetSelectorPropertyName<TMessage>(Expression<Func<TMessage, string>> selector)
        {
            var memberExpression = (MemberExpression)selector.Body;
            var propertyName = memberExpression.Member.Name;
            return propertyName;
        }
    }
}
