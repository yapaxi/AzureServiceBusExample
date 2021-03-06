﻿using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Clients
{
    public interface IMessageDestination<in TMessage>
    {
        Task SendMesage(TMessage message);
        Task SendMesageAsync(BrokeredMessage message);
        void SendMesage(BrokeredMessage message);
    }
}
