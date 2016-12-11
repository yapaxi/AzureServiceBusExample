using AzureServiceBusExample.Bus.Messages;
using AzureServiceBusExample.Bus.Messages.ShippedOrders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing.Handlers
{
    public class JetShippedOrderHandler : IMessageHandler<ShippedOrder>
    {
        public async Task Handle(ShippedOrder message)
        {
            // convert to jet confirmation
            // send order confirmation via marketplace api;
            Console.WriteLine($"\r\n\r\n [JET-SHIPPED] {message.MarketplaceName} received order {message.VenueOrderLineId} confirmation \r\n\r\n");
        }
    }
}
