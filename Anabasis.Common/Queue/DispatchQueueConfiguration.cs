using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Queue
{
    public class DispatchQueueConfiguration
    {
        public DispatchQueueConfiguration(Func<IEvent, Task> onEventReceived, int messageBatchSize, int queueMaxSize)
        {
            OnEventReceived = onEventReceived;
            MessageBatchSize = messageBatchSize;
            QueueMaxSize = queueMaxSize;
        }

        public Func<IEvent, Task> OnEventReceived { get; }
        public int MessageBatchSize { get; }
        public int QueueMaxSize { get; }
    }
}
