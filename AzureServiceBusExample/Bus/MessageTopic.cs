using AzureServiceBusExample.Bus.Messages;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    class MessageTopic<TMessage> : IMessageDestination<TMessage>
        where TMessage : ITopicFilteredMessage
    {
        private readonly string _filterName;
        private readonly TopicClient _client;
        private readonly MessagingFactory _factory;

        public MessageTopic(MessagingFactory factory, string filterName)
        {
            _filterName = filterName;
            _client = factory.CreateTopicClient(typeof(TMessage).FullName);
            _factory = factory;
            Log("created");
        }

        public async Task SendMesage(TMessage message)
        {
            Log($"sending message: {JsonConvert.SerializeObject(message)}");
            var m = new BrokeredMessage(message);
            m.Properties[_filterName] = message.FilterValue;
            await _client.SendAsync(m);
            Log($"sent message: {JsonConvert.SerializeObject(message)}");
        }
        
        private static void Log(string message)
        {
            Console.WriteLine($"{typeof(TMessage).Name} topic: {message}");
        }
    }
}
