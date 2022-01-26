using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public class DispatchQueue<TMessage> : IDisposable, IDispatchQueue<TMessage>
    {

        private readonly Func<TMessage, Task> _onEventReceived;
        private readonly FlushableBlockingCollection<TMessage> _workQueue;
        private readonly Task _workProc;

        public DispatchQueue(Func<TMessage, Task> onEventReceived, int messageBatchSize, int queueMaxSize)
        {
            _workQueue = new FlushableBlockingCollection<TMessage>(messageBatchSize, queueMaxSize);
            _workProc = Task.Run(HandleWork, CancellationToken.None);
            _onEventReceived = onEventReceived;
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
                    _onEventReceived(message).Wait();
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
