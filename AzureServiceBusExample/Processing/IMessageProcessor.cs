using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing
{
    public interface IMessageProcessor
    {
        Task Task { get; }
    }
}
