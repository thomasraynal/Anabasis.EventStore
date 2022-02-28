using Anabasis.Common.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public class DispatchQueue : IDispatchQueue
    {

        private readonly Func<IEvent, Task> _onEventReceived;
        private readonly FlushableBlockingCollection<IMessage> _workQueue;
        private readonly Task _workProc;

        public DispatchQueue(DispatchQueueConfiguration dispatchQueueConfiguration)
        {
            _workQueue = new FlushableBlockingCollection<IMessage>(dispatchQueueConfiguration.MessageBatchSize, dispatchQueueConfiguration.QueueMaxSize);

            _onEventReceived = dispatchQueueConfiguration.OnEventReceived;

            _workProc = Task.Run(HandleWork, CancellationToken.None).ContinueWith(task =>
               {
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
                    await _onEventReceived(message.Content);
                    await message.Acknowledge();
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
