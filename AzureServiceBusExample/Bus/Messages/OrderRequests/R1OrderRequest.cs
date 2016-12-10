using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages.OrderRequests
{
    public class R1OrderRequest : IMessage
    {
        public string MarketplaceName { get; set; }

        public string VenueOrderId { get; set; }

        public string Key => $"{MarketplaceName}-{VenueOrderId}";
    }
}
