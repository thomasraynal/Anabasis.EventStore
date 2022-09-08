using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
        private readonly object _synclock = new ();

        public RoundRobinDispatcherStrategy()
        {
            _rand = new Random(Guid.NewGuid().GetHashCode());
            _workerDispatchQueuesByTimestampUsage = new List<TimestampedResource<IWorkerDispatchQueue>>();
        }

        public override Task<(bool isDispatchQueueAvailable, IWorkerDispatchQueue? workerDispatchQueue)> Next(double timeoutInSeconds = 30.0)
        {

            (bool isDispatchQueueAvailable, IWorkerDispatchQueue? workerDispatchQueue) result;

            lock (_synclock)
            {

                IWorkerDispatchQueue? workerDispatchQueue = null;

                var timeoutDate = DateTime.UtcNow.AddSeconds(timeoutInSeconds);

                while (null == workerDispatchQueue)
                {
                    if (DateTime.Now < timeoutDate)
                    {
                        break;
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

                }

                result = (null != workerDispatchQueue, workerDispatchQueue);

                return Task.FromResult(result);

            }
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
