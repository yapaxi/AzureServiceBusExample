using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Autofac;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Clients;
using AzureServiceBusExample.Bus.Messages;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Bus.Messages.ShippedOrders;
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
            _container = new AutofacBuilder("global").Build();
            _killAllTokenSource = _container.Resolve<CancellationTokenSource>();
            _cloudManager = new CloudManager(_container, recreateObjects: false);
        }

        private static readonly IContainer _container;
        private static readonly CancellationTokenSource _killAllTokenSource;
        private static readonly CloudManager _cloudManager;

        public static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            _cloudManager.CreateRegisteredServiceBusObjects();
            
            var processors = _container.Resolve<IEnumerable<IMessageProcessor>>();
            var generator = RunOrderGenerator();
            var shipper = RunOrderShipper();

            Task.WaitAll(processors.Select(e => e.Task).Concat(new[] { shipper }).ToArray());

            Console.WriteLine("All processors closed");
        }

        
        private static Task RunOrderShipper()
        {
            return Task.Factory.StartNew(() => ShipRandomOrdersFromR1(
                _container.Resolve<IStorage>(),
                _container.Resolve<TopicMessageClient<ShippedOrder>>()),
                TaskCreationOptions.LongRunning
            );
        }

        private static Task RunOrderGenerator()
        {
            return Task.Factory
                .StartNew(() =>
                    PollJetApiToQueue(
                        _container.Resolve<QueueMessageClient<JetOrderRequest>>(),
                        _container.Resolve<QueueMessageClient<AmazonOrderRequest>>()),
                    TaskCreationOptions.LongRunning
                )
                .ContinueWith(e => _killAllTokenSource.Cancel());
        }

        private static void PollJetApiToQueue(
            QueueMessageClient<JetOrderRequest> jetOrderRequestQueue,
            QueueMessageClient<AmazonOrderRequest> amazonOrderRequestQueue)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < 1000; i++)
            {
                if (_killAllTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (rnd.Next(5) > 3)
                {
                    var r1 = new JetOrderRequest() { JetVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"[MM] Received order from Jet: {r1.JetVenueOrderId}");
                    jetOrderRequestQueue.SendMesage(r1).Wait();
                }
                else
                {
                    var r2 = new AmazonOrderRequest() { AmazonVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"[MM] Received order from Amazon: {r2.AmazonVenueOrderId}");
                    amazonOrderRequestQueue.SendMesage(r2).Wait();
                }

                Thread.Sleep(2000);
            }
        }

        private static void ShipRandomOrdersFromR1(IStorage storage, TopicMessageClient<ShippedOrder> shippedOrderTopic)
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
                    Console.WriteLine($"[R1-SHIPPER] Shipped order {order.Key}");
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
