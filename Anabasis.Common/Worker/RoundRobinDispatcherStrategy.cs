using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Worker
{
    public class RoundRobinDispatcherStrategy : BaseDispatcherStrategy
    {
        class TimestampedResource<T> : IComparable<TimestampedResource<T>>
        {
            public TimestampedResource(T resource)
            {
                Timestamp = DateTime.UtcNow;
                Resource = resource;
            }

            public DateTime Timestamp { get; set; }
            public T Resource { get; set; }

            public int CompareTo(TimestampedResource<T> other)
            {
                if (other == null)
                    return 1;

                else
                    return this.Timestamp.CompareTo(other.Timestamp);
            }
        }

        private Random _rand;
        private List<TimestampedResource<IWorkerDispatchQueue>> _workerDispatchQueuesByTimestampUsage;

        public RoundRobinDispatcherStrategy()
        {
            _rand = new Random(Guid.NewGuid().GetHashCode());
            _workerDispatchQueuesByTimestampUsage = new List<TimestampedResource<IWorkerDispatchQueue>>();
        }

        public override IWorkerDispatchQueue Next(int timeoutInSeconds = 30)
        {
            IWorkerDispatchQueue? workerDispatchQueue = null;

            var timeoutDate = DateTime.UtcNow.AddSeconds(timeoutInSeconds);

            while (null == workerDispatchQueue)
            {
                if (DateTime.Now < timeoutDate)
                {
                    throw new TimeoutException($"Unable to find an available instance in {timeoutInSeconds} seconds");
                }

                _workerDispatchQueuesByTimestampUsage.Sort();

                foreach (var timestampedResource in _workerDispatchQueuesByTimestampUsage)
                {
                    if (timestampedResource.Resource.CanEnqueue())
                    {
                        workerDispatchQueue = timestampedResource.Resource;
                        timestampedResource.Timestamp = DateTime.UtcNow;

                        break;
                    }

                }
            }

            return workerDispatchQueue;
        }

        protected override void OnInitialize()
        {
            var now = DateTime.UtcNow;

            foreach (var workerDispatchQueue in WorkerDispatchQueues)
            {
                _workerDispatchQueuesByTimestampUsage.Add(new TimestampedResource<IWorkerDispatchQueue>(workerDispatchQueue));
            }
        }
    }
}
