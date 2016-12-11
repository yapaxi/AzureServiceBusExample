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

        public Task SendMesageAsync(BrokeredMessage message)
        {
            return _client.SendAsync(message);
        }

        public void SendMesage(BrokeredMessage message)
        {
            _client.Send(message);
        }

        public Task<BrokeredMessage> ReceiveMessageAsync(TimeSpan timeout = default(TimeSpan))
        {
            return timeout == default(TimeSpan)
                    ? _client.ReceiveAsync()
                    : _client.ReceiveAsync(timeout);
        }

        public Task<BrokeredMessage> ReceiveMessage(long sequenceNumber)
        {
            return _client.ReceiveAsync(sequenceNumber);
        }
    }
}
