using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Queue
{
    public class DispatchQueueConfiguration
    {
        public DispatchQueueConfiguration(Func<IEvent, Task> onEventReceived, int messageBatchSize, int queueMaxSize, bool crashAppOnError = true)
        {
            OnEventReceived = onEventReceived;
            MessageBatchSize = messageBatchSize;
            QueueMaxSize = queueMaxSize;
            CrashAppOnError = crashAppOnError;
        }

        public Func<IEvent, Task> OnEventReceived { get; }
        public int MessageBatchSize { get; }
        public int QueueMaxSize { get; }
        public bool CrashAppOnError { get; }
    }
}
