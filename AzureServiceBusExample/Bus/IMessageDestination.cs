using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Bus
{
    public interface IMessageDestination<in TMessage>
    {
        Task SendMesage(TMessage message);
    }
}
