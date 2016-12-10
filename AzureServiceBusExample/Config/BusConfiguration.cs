using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Config
{
    
    public class BusConfiguration
    {
        public string ConnectionString { get; set; }

        public string OrderRequestQueueName { get; set; }
    }
}
