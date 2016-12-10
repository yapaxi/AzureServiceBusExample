using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages
{
    public class ShippedOrder : ITopicFilteredMessage
    {
        public string FilterValue => MarketplaceName;

        public string MarketplaceName { get; set; }
        public string VenueOrderLineId { get; set; }
    }
}
