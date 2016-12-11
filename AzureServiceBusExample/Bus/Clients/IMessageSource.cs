using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Clients
{
    public interface IMessageSource<out TMessage>
    {
        Task<BrokeredMessage> ReceiveMessageAsync(TimeSpan timeout = default(TimeSpan));
        Task<BrokeredMessage> ReceiveMessage(long sequenceNumber);
    }
}
