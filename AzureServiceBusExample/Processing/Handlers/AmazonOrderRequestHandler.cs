using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing.Handlers
{
    public class AmazonOrderRequestHandler : IMessageHandler<AmazonOrderRequest, R1OrderRequest>
    {
        public const string MarketplaceName = "Amazon";

        public async Task<R1OrderRequest> Handle(AmazonOrderRequest message)
        {
            Log($"handeled message body: {nameof(message.AmazonVenueOrderId)}={message.AmazonVenueOrderId}");
            return new R1OrderRequest()
            {
                VenueOrderId = message.AmazonVenueOrderId,
                MarketplaceName = MarketplaceName
            };
        }

        private void Log(string message)
        {
            Console.WriteLine($"{GetType().Name}: {message}");
        }
    }
}
