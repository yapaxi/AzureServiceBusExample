using AzureServiceBusExample.Autofac;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Clients
{
    public class QueueMessageClient<TMessage> : IMessageDestination<TMessage>, IMessageSource<TMessage>
    {
        private readonly QueueClient _client;
        private readonly MessagingFactory _factory;

        public QueueMessageClient(MessagingFactory factory, EnvironmentNamespaceManager ns)
        {
            _factory = factory;
            _client = factory.CreateQueueClient(ns.ResolvePath<TMessage>());
        }

        public Task SendMesage(TMessage message)
        {
            return _client.SendAsync(new BrokeredMessage(message));
        }

        public Task<BrokeredMessage> ReceiveMessage()
        {
            return _client.ReceiveAsync();
        }
    }
}
