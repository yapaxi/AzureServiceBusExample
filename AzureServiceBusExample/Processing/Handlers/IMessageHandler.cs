using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing.Handlers
{
    public interface IMessageHandler<in TInputMessage, TOutputMessage>
    {
        Task<TOutputMessage> Handle(TInputMessage message);
    }

    public interface IMessageHandler<in TInputMessage>
    {
        Task Handle(TInputMessage message);
    }
}
