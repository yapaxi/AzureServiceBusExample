using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Clients
{
    public class SubscriptionMessageClient<TMessage> : IMessageSource<TMessage>
    {
        private readonly SubscriptionClient _client;
        private readonly MessagingFactory _factory;

        public SubscriptionMessageClient(MessagingFactory factory, string filterValue, EnvironmentNamespaceManager ns)
        {
            _factory = factory;
            _client = factory.CreateSubscriptionClient(ns.ResolvePath<TMessage>(), filterValue);
        }

        public Task<BrokeredMessage> ReceiveMessage()
        {
            return _client.ReceiveAsync();
        }
    }
}
