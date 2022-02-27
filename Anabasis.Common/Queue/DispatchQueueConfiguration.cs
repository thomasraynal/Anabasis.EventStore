using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Queue
{
    public class DispatchQueueConfiguration<TMessage>
    {
        public DispatchQueueConfiguration(Func<TMessage, Task> onEventReceived, int messageBatchSize, int queueMaxSize)
        {
            OnEventReceived = onEventReceived;
            MessageBatchSize = messageBatchSize;
            QueueMaxSize = queueMaxSize;
        }

        public Func<TMessage, Task> OnEventReceived { get; }
        public int MessageBatchSize { get; }
        public int QueueMaxSize { get; }
    }
}
