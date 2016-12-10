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
    public class InputOutputMessageQueueProcessor<TInputMessage, TOutputMessage> : IMessageProcessor
    {
        private readonly IMessageSource<TInputMessage> _inputQueue;
        private readonly IMessageDestination<TOutputMessage> _outputQueue;
        private readonly CancellationToken _token;
        private readonly IMessageHandler<TInputMessage, TOutputMessage> _handler;
        private readonly Task _completionTask;

        public InputOutputMessageQueueProcessor(
            IMessageHandler<TInputMessage, TOutputMessage> handler,
            IMessageSource<TInputMessage> inputQueue,
            IMessageDestination<TOutputMessage> outputQueue,
            CancellationTokenSource tokenSource)
        {
            _inputQueue = inputQueue;
            _outputQueue = outputQueue;
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
                using (var message = await _inputQueue.ReceiveMessage())
                {
                    Log($"received message: sn={message.SequenceNumber}");

                    var input = message.GetBody<TInputMessage>();

                    var output = await _handler.Handle(input);

                    Log($"sent message to output: sn={message.SequenceNumber}");
                    await _outputQueue.SendMesage(output);

                    await message.CompleteAsync();
                    Log($"message is complete: sn={message.SequenceNumber}");
                }
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{typeof(TInputMessage).Name} -> {typeof(TOutputMessage).Name} processor: {message}");
        }
    }
}
