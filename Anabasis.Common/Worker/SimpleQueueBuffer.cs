using Anabasis.Common.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public enum QueueBufferStatus
    {
        Available = 0,
        Busy = 1
    }

    public class SimpleQueueBuffer : IQueueBuffer
    {

        private readonly ConcurrentQueue<IMessage> _concurrentQueue;
        private readonly int _bufferMaxSize;
        private readonly double _bufferAbsoluteTimeoutInSecond;
        private readonly double _bufferSlidingTimeoutInSecond;

        public DateTime LastDequeuedUtcDate { get; private set; }
        public DateTime LastEnqueuedUtcDate { get; private set; }

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

        public bool CanPush => _concurrentQueue.Count < _bufferMaxSize;

        public void Push(IMessage message)
        {
            if (!CanPush)
            {
                throw new InvalidOperationException("Push operation not possible");
            }
  
            LastEnqueuedUtcDate = DateTime.UtcNow;
           
            _concurrentQueue.Enqueue(message);
        }

        public IMessage[] TryPush(IMessage[] messages, out IMessage[] unProcessedMessages)
        {
            LastEnqueuedUtcDate = DateTime.UtcNow;

            var unProcessedMessagesList = new List<IMessage>();
            var processedMessagesList = new List<IMessage>();

            foreach (var message in messages)
            {
                if (CanPush)
                {
                    _concurrentQueue.Enqueue(message);

                    processedMessagesList.Add(message);
                }
                else
                {
                    unProcessedMessagesList.Add(message);
                }
            }

            unProcessedMessages = unProcessedMessagesList.ToArray();

            return processedMessagesList.ToArray();

        }

        public bool TryPull(out IMessage[] pulledMessages, int? maxNumberOfMessage = null)
        {
            if (CanPull)
            {
                pulledMessages = Pull(maxNumberOfMessage);
            }
            else
            {
                pulledMessages = Array.Empty<IMessage>();
            }

            return pulledMessages.Length > 0;
        }

        public IMessage[] Pull(int? maxNumberOfMessage = null)
        {
            if (!CanPull)
            {
                throw new InvalidOperationException("Pull operation not possible");
            }

            var messageBatch = new List<IMessage>();

            while (!_concurrentQueue.IsEmpty && messageBatch.Count < _bufferMaxSize)
            {
                var hasDequeued = _concurrentQueue.TryDequeue(out IMessage message);

                if (hasDequeued)
                {
                    messageBatch.Add(message);

                    if (null != maxNumberOfMessage && messageBatch.Count == maxNumberOfMessage.Value)
                    {
                        break;
                    }
                }
            }

            LastDequeuedUtcDate = DateTime.UtcNow;

            return messageBatch.ToArray();
        }

        public bool HasMessages
        {
            get
            {
                return _concurrentQueue.Count > 0;
            }
        }

        public bool CanPull
        {
            get
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
        }

        public async Task<IMessage[]> Flush(bool nackMessages)
        {
            var messages = new List<IMessage>();

            if (nackMessages)
            {
                while (!_concurrentQueue.IsEmpty)
                {
                    var hasDequeued = _concurrentQueue.TryDequeue(out var message);

                    messages.Add(message);

                    if (hasDequeued)
                    {
                        await message.NotAcknowledge();
                    }
                }
            }
            else
            {
                _concurrentQueue.Clear();
            }

            return messages.ToArray();

        }

        public void Dispose()
        {
            Flush(true).Wait();
        }

    }
}
