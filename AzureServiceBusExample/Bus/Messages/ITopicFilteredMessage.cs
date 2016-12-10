using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus.Messages
{
    public interface ITopicFilteredMessage
    {
        string FilterValue { get; }
    }
}
