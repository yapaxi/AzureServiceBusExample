using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing.Handlers
{
    public class JetOrderRequestHandler : IMessageHandler<JetOrderRequest, R1OrderRequest>
    {
        public R1OrderRequest Handle(JetOrderRequest message)
        {
            Log($"handeled message body: {nameof(message.JetVenueOrderId)}={message.JetVenueOrderId}");
            return new R1OrderRequest()
            {
                VenueOrderId = message.JetVenueOrderId
            };
        }

        private void Log(string message)
        {
            Console.WriteLine($"{GetType().Name}: {message}");
        }
    }
}
