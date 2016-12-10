using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    public class MessageQueue<TMessage> : IMessageDestination<TMessage>, IMessageSource<TMessage>
    {
        private readonly QueueClient _client;
        private readonly MessagingFactory _factory;

        public MessageQueue(MessagingFactory factory)
        {
            Log("created");
            _factory = factory;
            _client = factory.CreateQueueClient(typeof(TMessage).FullName);
        }

        public async Task SendMesage(TMessage message)
        {
            Log($"sending message: {JsonConvert.SerializeObject(message)}");
            await _client.SendAsync(new BrokeredMessage(message));
            Log($"sent message: {JsonConvert.SerializeObject(message)}");
        }

        public async Task<BrokeredMessage> ReceiveMessage()
        {
            Log($"receiving message...");
            var message = await _client.ReceiveAsync();
            Log($"received message: sn={message.SequenceNumber}");
            return message;
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{typeof(TMessage).Name} Queue: {message}");
        }
    }
}
