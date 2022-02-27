using Anabasis.Common.Queue;
using System;
using System.Reactive.Concurrency;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public class DispatchQueue<TMessage> : IDispatchQueue<TMessage>, IDisposable
    {

        private readonly Func<TMessage, Task> _onEventReceived;
        private readonly FlushableBlockingCollection<TMessage> _workQueue;
        private readonly Task _workProc;

        public DispatchQueue(DispatchQueueConfiguration<TMessage> dispatchQueueConfiguration)
        {
            _workQueue = new FlushableBlockingCollection<TMessage>(dispatchQueueConfiguration.MessageBatchSize, dispatchQueueConfiguration.QueueMaxSize);
            _workProc = Task.Run(HandleWork, CancellationToken.None);
            _onEventReceived = dispatchQueueConfiguration.OnEventReceived;
        }

        public void Enqueue(TMessage message)
        {
            _workQueue.Add(message);
        }

        private void HandleWork()
        {
            foreach (var messages in _workQueue.GetConsumingEnumerable())
            {
                foreach (var message in messages)
                {
                    try
                    {
                        _onEventReceived(message).Wait();
                    }
                    catch(Exception exception)
                    {
                        Scheduler.Default.Schedule(() => ExceptionDispatchInfo.Capture(exception).Throw());

                        throw;
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
