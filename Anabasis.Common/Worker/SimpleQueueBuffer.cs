using Anabasis.Common.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public class SimpleQueueBuffer : IQueueBuffer
    {

        private readonly ConcurrentQueue<IMessage> _concurrentQueue;
        private readonly int _bufferMaxSize;
        private readonly double _bufferAbsoluteTimeoutInSecond;
        private readonly double _bufferSlidingTimeoutInSecond;

        public DateTime LastDequeuedUtcDate { get; set; }
        public DateTime LastEnqueuedUtcDate { get; set; }
      
        public SimpleQueueBuffer(int bufferMaxSize,
            double bufferAbsoluteTimeoutInSecond,
            double bufferSlidingTimeoutInSecond)
        {

            _concurrentQueue = new ConcurrentQueue<IMessage>();

            _bufferMaxSize = bufferMaxSize;
            _bufferAbsoluteTimeoutInSecond = bufferAbsoluteTimeoutInSecond;
            _bufferSlidingTimeoutInSecond = bufferSlidingTimeoutInSecond;

            LastDequeuedUtcDate = DateTime.MinValue;
            LastEnqueuedUtcDate = DateTime.MinValue;
          
        }

        public bool CanAdd => _concurrentQueue.Count < _bufferMaxSize;

        public void Push(IMessage message)
        {
            LastEnqueuedUtcDate = DateTime.UtcNow;
           
            _concurrentQueue.Enqueue(message);
        }

        public IMessage[] Pull()
        {
            if (!CanDequeue())
                throw new InvalidOperationException("Pull operation not possible");

            var messageBatch = new List<IMessage>();

            while (!_concurrentQueue.IsEmpty && messageBatch.Count < _bufferMaxSize)
            {
                var hasDequeued = _concurrentQueue.TryDequeue(out IMessage message);

                messageBatch.Add(message);
            }

            LastDequeuedUtcDate = DateTime.UtcNow;

            return messageBatch.ToArray();
        }

        public bool CanDequeue()
        {
            var now = DateTime.UtcNow;

            if (_concurrentQueue.IsEmpty) return false;

            if (now > LastDequeuedUtcDate.AddSeconds(_bufferAbsoluteTimeoutInSecond))
            {
                var secondsSincelastEnqueudMessage = (now - LastEnqueuedUtcDate).TotalSeconds;

                if (secondsSincelastEnqueudMessage < _bufferSlidingTimeoutInSecond)
                {
                    return false;
                }

                return true;

            }

            return _concurrentQueue.Count >= _bufferMaxSize;
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
