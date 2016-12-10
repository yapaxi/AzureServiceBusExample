using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Config;
using AzureServiceBusExample.Processing;
using AzureServiceBusExample.Processing.Handlers;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            
            RegisterMessageQueue<JetOrderRequest>(busConnectionString, nsManager, builder);
            RegisterMessageQueue<R1OrderRequest>(busConnectionString, nsManager, builder);

            builder.RegisterType<JetOrderRequestHandler>().As<IMessageHandler<JetOrderRequest, R1OrderRequest>>();
            builder.RegisterType<MessageProcessor<JetOrderRequest, R1OrderRequest>>().As<IMessageProcessor>();

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
                nsManager.CreateQueue(type.FullName);
            }
            builder.Register(e => new MessageQueue<TMessage>(QueueClient.CreateFromConnectionString(busConnectionString, type.FullName)));
        }

        private static readonly IContainer _container;
        private static readonly CancellationTokenSource _killAllTokenSource;

        public static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            var processors = _container.Resolve<IEnumerable<IMessageProcessor>>();
            var jetOrderRequestQueue = _container.Resolve<MessageQueue<JetOrderRequest>>();

            PollJetApiToQueue(jetOrderRequestQueue);
            
            _killAllTokenSource.Cancel();

            Task.WaitAll(processors.Select(e => e.Task).ToArray());

            Console.WriteLine("All processors closed");
        }

        private static void PollJetApiToQueue(MessageQueue<JetOrderRequest> jetOrderRequestQueue)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (_killAllTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var orderRequest = new JetOrderRequest() { JetVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                Console.WriteLine($"Received order from jet: {orderRequest.JetVenueOrderId}");
                jetOrderRequestQueue.SendMesage(orderRequest).Wait();
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
