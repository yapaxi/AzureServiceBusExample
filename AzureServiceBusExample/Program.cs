using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Autofac;
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
            _container = AutofacBuilder.Build();
            _killAllTokenSource = _container.Resolve<CancellationTokenSource>();
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
