using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Definitions
{
    public class SubscriptionDefinition
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public SqlFilter Filter { get; set; }
    }
}
