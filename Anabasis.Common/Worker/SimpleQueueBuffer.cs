using Anabasis.Common.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Worker
{
    public class SimpleQueueBuffer : IQueueBuffer
    {
        private readonly IQueueBufferStrategy _queueBufferStrategy;
        private readonly ConcurrentQueue<IMessage> _concurrentQueue;

        public DateTime LastDequeuedUtcDate { get; }
        public DateTime LastEnqueuedUtcDate { get; }
        public int LastEnqueuedSlindingIntervalInSeconds { get; }

        public SimpleQueueBuffer(IQueueBufferStrategy queueBufferStrategy)
        {
            _queueBufferStrategy = queueBufferStrategy;
            _concurrentQueue = new ConcurrentQueue<IMessage>();

            LastDequeuedUtcDate = DateTime.MinValue;
            LastEnqueuedUtcDate = DateTime.MinValue;
            LastEnqueuedSlindingIntervalInSeconds = int.MaxValue;
        }

        public bool CanAdd => _concurrentQueue.Count < _queueBufferStrategy.BufferMaxSize;

        public bool CanDequeue()
        {
            var now = DateTime.UtcNow;

            if(now > LastDequeuedUtcDate.AddSeconds(_queueBufferStrategy.BufferAbsoluteTimeoutInSecond))
            {

                if (LastEnqueuedSlindingIntervalInSeconds > _queueBufferStrategy.BufferSlindingTimeoutInSecond)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
