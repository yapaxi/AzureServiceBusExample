using Autofac;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Definitions;
using Microsoft.ServiceBus;
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
            
            foreach (var s in _container.Resolve<IEnumerable<QueueDefinition>>())
            {
                CreateQueue(s);
            }

            foreach (var s in _container.Resolve<IEnumerable<TopicDefinition>>())
            {
                CreateTopic(s);
            }

            foreach (var s in _container.Resolve<IEnumerable<SubscriptionDefinition>>())
            {
                CreateSubscription(s);
            }
        }

        private void CreateSubscription(SubscriptionDefinition s)
        {
            var path = _envNS.ResolvePath(s.Type);

            if (!_globalNS.SubscriptionExists(path, s.Name))
            {
                Console.WriteLine($"[CloudManager] Creating route to {path} for {s.Name}");
                _globalNS.CreateSubscription(path, s.Name, s.Filter);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating route to {path} for {s.Name}");
                _globalNS.DeleteSubscription(path, s.Name);
                _globalNS.CreateSubscription(path, s.Name, s.Filter);
            }
        }

        private void CreateTopic(TopicDefinition s)
        {
            var path = _envNS.ResolvePath(s.Type);
            if (!_globalNS.TopicExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating topic for {s.Type.Name}");
                _globalNS.CreateTopic(path);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating topic for {s.Type.Name}");
                _globalNS.DeleteTopic(path);
                _globalNS.CreateTopic(path);
            }
        }

        private void CreateQueue(QueueDefinition s)
        {
            var path = _envNS.ResolvePath(s.Type);
            if (!_globalNS.QueueExists(path))
            {
                Console.WriteLine($"[CloudManager] Creating queue for {s.Type.Name}");
                _globalNS.CreateQueue(path);
            }
            else if (_recreateObjects)
            {
                Console.WriteLine($"[CloudManager] Recreating queue for {s.Type.Name}");
                _globalNS.DeleteQueue(path);
                _globalNS.CreateQueue(path);
            }
        }
    }
}
