using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages.OrderRequests
{
    public class AmazonOrderRequest : IMessage
    {
        public string AmazonVenueOrderId { get; set; }

        public string Key => AmazonVenueOrderId;
    }
}
