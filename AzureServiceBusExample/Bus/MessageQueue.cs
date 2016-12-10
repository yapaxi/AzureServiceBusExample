using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    public class MessageQueue<TMessage>
    {
        private readonly QueueClient _client;

        public MessageQueue(QueueClient client)
        {
            Log("created");
            _client = client;
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
