using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages.OrderRequests
{
    public class AmazonOrderRequest
    {
        public string AmazonVenueOrderId { get; set; }
    }
}
