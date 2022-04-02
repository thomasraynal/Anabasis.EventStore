using Anabasis.Common.Queue;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public class DispatchQueue : IDispatchQueue
    {

        private readonly Func<IEvent, Task> _onEventReceived;
        private readonly DispatchQueueConfiguration _dispatchQueueConfiguration;
        private readonly FlushableBlockingCollection<IMessage> _workQueue;
        private readonly Thread _thread;
        private readonly IKillSwitch _killSwitch;

        public bool IsFaulted { get; private set; }
        public ILogger Logger { get; }
        public string Owner { get; }
        public string Id { get; }

        public DispatchQueue(string ownerId, DispatchQueueConfiguration dispatchQueueConfiguration, ILoggerFactory loggerFactory= null, IKillSwitch killSwitch = null)
        {

            Logger = loggerFactory?.CreateLogger(GetType());
            Owner = ownerId;
            Id = $"{nameof(DispatchQueue)}_{ownerId}_{Guid.NewGuid()}";

            _killSwitch = killSwitch ?? new KillSwitch();

            _workQueue = new FlushableBlockingCollection<IMessage>(dispatchQueueConfiguration.MessageBatchSize, dispatchQueueConfiguration.QueueMaxSize);

            _onEventReceived = dispatchQueueConfiguration.OnEventReceived;

            _dispatchQueueConfiguration = dispatchQueueConfiguration;
            
            _thread = new Thread(HandleWork)
            {
                IsBackground = true,
                Name = Id,
            };

            _thread.Start();

            Logger.LogDebug("{0} started", Id);
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

                        Logger?.LogError(exception, $"An exception occured during the message consumption process: {message.ToJson()}");

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
            _workQueue.Dispose();
            _thread?.Join();
        }

        public bool CanEnqueue()
        {
            return _workQueue.CanAdd;
        }

    }
}
