using Anabasis.Common.Queue;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public class DispatchQueue : IDispatchQueue
    {

        private readonly Func<IEvent, Task> _onEventReceived;
        private readonly DispatchQueueConfiguration _dispatchQueueConfiguration;
        private readonly FlushableBlockingCollection<IMessage> _workQueue;
        private readonly Task _workProc;

        public bool IsFaulted { get; private set; }
        public ILogger Logger { get; private set; }

        public DispatchQueue(DispatchQueueConfiguration dispatchQueueConfiguration, ILoggerFactory loggerFactory)
        {

            Logger = loggerFactory?.CreateLogger(GetType());

            _workQueue = new FlushableBlockingCollection<IMessage>(dispatchQueueConfiguration.MessageBatchSize, dispatchQueueConfiguration.QueueMaxSize);

            _onEventReceived = dispatchQueueConfiguration.OnEventReceived;

            _dispatchQueueConfiguration = dispatchQueueConfiguration;

            _workProc = Task.Run(HandleWork, CancellationToken.None).ContinueWith(task =>
               {
                   _workQueue.Dispose();

                   KillSwitch.KillMe(task.Exception);

               }, TaskContinuationOptions.OnlyOnFaulted);

        }

        public void Enqueue(IMessage message)
        {
            _workQueue.Add(message);
        }

        private async void HandleWork()
        {
            foreach (var messages in _workQueue.GetConsumingEnumerable())
            {
                foreach (var message in messages)
                {
                    try
                    {
                        if (IsFaulted)
                        {
                            await message.NotAcknowledge();
                            continue;
                        }

                        await _onEventReceived(message.Content);

                        await message.Acknowledge();

                    }
                    catch(Exception exception)
                    {
                        if (_dispatchQueueConfiguration.CrashAppOnError)
                        {
                            IsFaulted = true;

                            throw;
                        }
                        else
                        {
                            Logger?.LogError(exception, "An error occured during the message consumption process.");

                            await message.NotAcknowledge();
                        }

                    }
                }
            }
        }

        public void Dispose()
        {
            _workQueue.WaitUntilIsEmpty();
            _workQueue.Dispose();
        }

        public bool CanEnqueue()
        {
            return _workQueue.CanAdd;
        }

    }
}
