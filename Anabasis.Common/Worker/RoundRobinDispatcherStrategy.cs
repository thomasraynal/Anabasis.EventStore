using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        public override async Task<(bool isDispatchQueueAvailable, IWorkerDispatchQueue? workerDispatchQueue)> Next(double timeoutInSeconds = 30.0)
        {
            IWorkerDispatchQueue? workerDispatchQueue = null;

            var timeoutDate = DateTime.UtcNow.AddSeconds(timeoutInSeconds);

            while (null == workerDispatchQueue)
            {
                if (DateTime.Now < timeoutDate)
                {
                    return (false, null);
                }

                _workerDispatchQueuesByTimestampUsage.Sort();   

                foreach (var timestampedResource in _workerDispatchQueuesByTimestampUsage)
                {
                    if (timestampedResource.Resource.CanPush())
                    {
                        workerDispatchQueue = timestampedResource.Resource;
                        timestampedResource.Timestamp = DateTime.UtcNow;

                        break;
                    }

                }

                await Task.Delay(100);
            }

            return (true, workerDispatchQueue);
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
