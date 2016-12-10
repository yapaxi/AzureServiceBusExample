using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    public class MessageSubscription<TMessage> : IMessageSource<TMessage>
    {
        private readonly SubscriptionClient _client;
        private readonly MessagingFactory _factory;

        public MessageSubscription(MessagingFactory factory, string filterValue)
        {
            _client = factory.CreateSubscriptionClient(typeof(TMessage).FullName, filterValue);
            _factory = factory;
            Log("created");
        }

        public async Task<BrokeredMessage> ReceiveMessage()
        {
            Log($"receiving message...");
            var message = await _client.ReceiveAsync();
            Log($"received message: sn={message.SequenceNumber}");
            return message;
        }

        private void Log(string message)
        {
            Console.WriteLine($"{typeof(TMessage).Name} subscription for {_client.Name}: {message}");
        }
    }
}
