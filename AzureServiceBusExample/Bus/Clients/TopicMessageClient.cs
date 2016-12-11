using AzureServiceBusExample.Bus.Messages;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Clients
{
    class TopicMessageClient<TMessage> : IMessageDestination<TMessage>
        where TMessage : ITopicFilteredMessage
    {
        private readonly string _filterPropertyName;
        private readonly TopicClient _client;
        private readonly MessagingFactory _factory;

        public TopicMessageClient(MessagingFactory factory, string filterPropertyName, EnvironmentNamespaceManager ns)
        {
            _factory = factory;
            _filterPropertyName = filterPropertyName;
            _client = factory.CreateTopicClient(ns.ResolvePath<TMessage>());
        }

        public Task SendMesage(TMessage message)
        {
            var m = new BrokeredMessage(message);
            m.Properties[_filterPropertyName] = message.FilterValue;
            return _client.SendAsync(m);
        }

        public Task SendMesageAsync(BrokeredMessage message)
        {
            return _client.SendAsync(message);
        }

        public void SendMesage(BrokeredMessage message)
        {
            _client.Send(message);
        }
    }
}
