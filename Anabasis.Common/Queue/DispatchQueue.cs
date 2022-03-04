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
        private readonly IKillSwitch _killSwitch;

        public bool IsFaulted { get; private set; }
        public ILogger Logger { get; }

        public DispatchQueue(DispatchQueueConfiguration dispatchQueueConfiguration, ILoggerFactory loggerFactory, IKillSwitch killSwitch = null)
        {

            Logger = loggerFactory?.CreateLogger(GetType());

            _killSwitch = killSwitch ?? new KillSwitch();

            _workQueue = new FlushableBlockingCollection<IMessage>(dispatchQueueConfiguration.MessageBatchSize, dispatchQueueConfiguration.QueueMaxSize);

            _onEventReceived = dispatchQueueConfiguration.OnEventReceived;

            _dispatchQueueConfiguration = dispatchQueueConfiguration;

            _workProc = Task.Run(HandleWork, CancellationToken.None);

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
                    catch (Exception exception)
                    {

                        Logger?.LogError(exception, $"An error occured during the message consumption process: {message.ToJson()}");

                        await message.NotAcknowledge();

                        if (_dispatchQueueConfiguration.CrashAppOnError)
                        {
                            IsFaulted = true;

                            _workQueue.SetAddingCompleted();

                            var remainingMessages = _workQueue.Flush();

                            while (remainingMessages.TryDequeue(out var remainingMessage))
                            {
                                await remainingMessage.NotAcknowledge();
                            }

                            _killSwitch.KillMe(exception);
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
