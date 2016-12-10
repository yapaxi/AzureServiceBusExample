using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing.Handlers
{
    public class R1OrderRequestHandler : IMessageHandler<R1OrderRequest>
    {
        private readonly IStorage _storage;

        public R1OrderRequestHandler(IStorage storage)
        {
            _storage = storage;
        }

        public async Task Handle(R1OrderRequest message)
        {
            Console.WriteLine($"{nameof(R1OrderRequestHandler)}: Saved message {message.Key}");
            await _storage.Save(message);
        }
    }
}
