using Autofac;
using AzureServiceBusExample.Bus;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample
{
    public class CloudManager
    {
        private readonly IContainer _container;
        private readonly EnvironmentNamespaceManager _envNS;
        private readonly NamespaceManager _globalNS;
        private readonly bool _recreateObjects;

        public CloudManager(IContainer container, bool recreateObjects)
        {
            _container = container;
            _recreateObjects = recreateObjects;
            _envNS = _container.Resolve<EnvironmentNamespaceManager>();
            _globalNS = _container.Resolve<NamespaceManager>();
        }

        public void CreateRegisteredServiceBusObjects()
        {
            Console.WriteLine("[CloudManager] Creating cloud service bus objects");
            
            foreach (var s in _container.Resolve<IEnumerable<QueueDescription>>())
            {
                CreateQueue(s);
            }

            foreach (var s in _container.Resolve<IEnumerable<TopicDescription>>())
            {
                CreateTopic(s);
            }

            foreach (var s in _container.Resolve<IEnumerable<Tuple<SubscriptionDescription, SqlFilter>>>())
            {
                CreateSubscription(s);
            }
        }

        private void CreateSubscription(Tuple<SubscriptionDescription, SqlFilter> s)
        {
            var path = s.Item1.TopicPath;
            if (!_globalNS.SubscriptionExists(path, s.Item1.Name))
            {
                Console.WriteLine($"[CloudManager] Creating route to {path} for {s.Item1.Name}");
                _globalNS.CreateSubscription(s.Item1, s.Item2);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating route to {path} for {s.Item1.Name}");
                _globalNS.DeleteSubscription(path, s.Item1.Name);
                _globalNS.CreateSubscription(s.Item1, s.Item2);
            }
        }

        private void CreateTopic(TopicDescription s)
        {
            var path = s.Path;
            if (!_globalNS.TopicExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating topic for {path}");
                _globalNS.CreateTopic(s);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating topic for {path}");
                _globalNS.DeleteTopic(path);
                _globalNS.CreateTopic(s);
            }
        }

        private void CreateQueue(QueueDescription s)
        {
            var path = s.Path;
            if (!_globalNS.QueueExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating queue for {path}");
                _globalNS.CreateQueue(s);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating queue for {path}");
                _globalNS.DeleteQueue(path);
                _globalNS.CreateQueue(s);
            }
        }
    }
}
