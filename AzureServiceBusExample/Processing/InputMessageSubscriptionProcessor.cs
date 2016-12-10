using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Processing.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Processing
{
    public class InputMessageSubscriptionProcessor<TInputMessage, TMessageHandler> : IMessageProcessor
        where TMessageHandler : IMessageHandler<TInputMessage>
    {
        private readonly MessageSubscription<TInputMessage> _subscription;
        private readonly CancellationToken _token;
        private readonly TMessageHandler _handler;
        private readonly Task _completionTask;

        public InputMessageSubscriptionProcessor(
            TMessageHandler handler,
            MessageSubscription<TInputMessage> subscription,
            CancellationTokenSource tokenSource)
        {
            _subscription = subscription;
            _token = tokenSource.Token;
            _handler = handler;

            _completionTask = Task.Factory.StartNew(Run, tokenSource, TaskCreationOptions.LongRunning);
        }

        public Task Task => _completionTask;

        private async Task Run(object state)
        {
            try
            {
                await RunInternal();
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.Flatten().ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task RunInternal()
        {
            while (!_token.IsCancellationRequested)
            {
                using (var message = await _subscription.ReceiveMessage())
                {
                    Log($"received message: sn={message.SequenceNumber}");

                    var input = message.GetBody<TInputMessage>();

                    await _handler.Handle(input);
                    
                    await message.CompleteAsync();
                    Log($"message is complete: sn={message.SequenceNumber}");
                }
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{typeof(TInputMessage).Name} subscription processor: {message}");
        }
    }
}
