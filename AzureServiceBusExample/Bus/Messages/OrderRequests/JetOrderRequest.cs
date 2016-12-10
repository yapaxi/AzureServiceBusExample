using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages.OrderRequests
{
    public class JetOrderRequest : IMessage
    {
        public string JetVenueOrderId { get; set; }

        public string Key => JetVenueOrderId;
    }
}
