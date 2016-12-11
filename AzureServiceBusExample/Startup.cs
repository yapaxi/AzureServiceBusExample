using Autofac;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Clients;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Bus.Messages.ShippedOrders;
using AzureServiceBusExample.Processing;
using AzureServiceBusExample.Storages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample
{
    public class Startup
    {
        private readonly IContainer _container;
        private readonly NamespaceManager _globalNS;
        private readonly CancellationTokenSource _killAllTokenSource;

        public Startup(IContainer container)
        {
            _container = container;
            _globalNS = _container.Resolve<NamespaceManager>();
            _killAllTokenSource = _container.Resolve<CancellationTokenSource>();
        }

        public void CreateRegisteredServiceBusObjects(bool recreateObjects)
        {
            var current = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[CloudManager] Creating cloud service bus objects");
            Console.ForegroundColor = ConsoleColor.White;

            foreach (var s in _container.Resolve<IEnumerable<QueueDescription>>())
            {
                CreateQueue(s, recreateObjects);
            }

            foreach (var s in _container.Resolve<IEnumerable<TopicDescription>>())
            {
                CreateTopic(s, recreateObjects);
            }

            foreach (var s in _container.Resolve<IEnumerable<Tuple<SubscriptionDescription, SqlFilter>>>())
            {
                CreateSubscription(s, recreateObjects);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\r\n\r\n[CloudManager] Cloud objects created\r\n\r\n");

            Console.ForegroundColor = current;
        }

        public Task RunProcessors()
        {
            return Task.WhenAll(_container.Resolve<IEnumerable<IMessageProcessor>>().Select(e => e.Task).ToArray());
        }

        public async Task PollMarketplaces()
        {
            var jet = _container.Resolve<QueueMessageClient<JetOrderRequest>>();
            var amazon = _container.Resolve<QueueMessageClient<AmazonOrderRequest>>();
            var rnd = new Random((int)DateTime.Now.Ticks);

            for (int i = 0; i < 10; i++)
            {
                if (_killAllTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (rnd.Next(5) > 3)
                {
                    var r1 = new JetOrderRequest() { JetVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"[MM] Received order from Jet: {r1.JetVenueOrderId}");
                    await jet.SendMesage(r1);
                }
                else
                {
                    var r2 = new AmazonOrderRequest() { AmazonVenueOrderId = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    Console.WriteLine($"[MM] Received order from Amazon: {r2.AmazonVenueOrderId}");
                    await amazon.SendMesage(r2);
                }

                Thread.Sleep(100);
            }

            _killAllTokenSource.Cancel();
        }

        public async Task ShipOrders()
        {
            var storage = _container.Resolve<IStorage>();
            var shippedOrderTopic = _container.Resolve<TopicMessageClient<ShippedOrder>>();

            var rnd = new Random((int)DateTime.Now.Ticks);
            var shipped = new HashSet<string>();
            while (!_killAllTokenSource.IsCancellationRequested)
            {
                var orders = (await storage.GetAll<R1OrderRequest>()).Where(e => !shipped.Contains(e.Key));

                foreach (var order in orders)
                {
                    Console.WriteLine($"[R1-SHIPPER] Shipped order {order.Key}");
                    shipped.Add(order.Key);
                    await shippedOrderTopic.SendMesage(new ShippedOrder()
                    {
                        MarketplaceName = order.MarketplaceName,
                        VenueOrderLineId = order.VenueOrderId
                    });
                }

                Thread.Sleep(100);
            }
        }

        private void CreateSubscription(Tuple<SubscriptionDescription, SqlFilter> s, bool recreateObjects)
        {
            var path = s.Item1.TopicPath;
            if (!_globalNS.SubscriptionExists(path, s.Item1.Name))
            {
                Console.WriteLine($"[CloudManager] Creating route to {path} for {s.Item1.Name}");
                _globalNS.CreateSubscription(s.Item1, s.Item2);
            }
            else if (recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating route to {path} for {s.Item1.Name}");
                _globalNS.DeleteSubscription(path, s.Item1.Name);
                _globalNS.CreateSubscription(s.Item1, s.Item2);
            }
            else
            {
                Console.WriteLine($"[CloudManager] Route to {path} for {s.Item1.Name} already exists");
            }
        }

        private void CreateTopic(TopicDescription s, bool recreateObjects)
        {
            var path = s.Path;
            if (!_globalNS.TopicExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating topic for {path}");
                _globalNS.CreateTopic(s);
            }
            else if (recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating topic for {path}");
                _globalNS.DeleteTopic(path);
                _globalNS.CreateTopic(s);
            }
            else
            {
                Console.WriteLine($"[CloudManager] Topic for {path} already exists");
            }
        }

        private void CreateQueue(QueueDescription s, bool recreateObjects)
        {
            var path = s.Path;
            if (!_globalNS.QueueExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating queue for {path}");
                _globalNS.CreateQueue(s);
            }
            else if (recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating queue for {path}");
                _globalNS.DeleteQueue(path);
                _globalNS.CreateQueue(s);
            }
            else
            {
                Console.WriteLine($"[CloudManager] Queue for {path} already exists");
            }
        }
    }
}
