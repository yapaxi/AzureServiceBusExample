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
        Task<BrokeredMessage> ReceiveMessage();
    }
}
