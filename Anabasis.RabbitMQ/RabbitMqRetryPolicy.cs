using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqRetryPolicy : RetryPolicy
    {
        public RabbitMqRetryPolicy(int retryCount = 10) : base(new RabbitMqTransientErrorDetectionStrategy(), retryCount) { }
        public RabbitMqRetryPolicy(RetryStrategy retryStrategy) : base(new RabbitMqTransientErrorDetectionStrategy(), retryStrategy) { }
        public RabbitMqRetryPolicy(int retryCount, TimeSpan retryInterval) : base(new RabbitMqTransientErrorDetectionStrategy(), retryCount, retryInterval) { }
        public RabbitMqRetryPolicy(int retryCount, TimeSpan initialInterval, TimeSpan increment) : base(new RabbitMqTransientErrorDetectionStrategy(), retryCount, initialInterval, increment) { }
        public RabbitMqRetryPolicy(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff) : base(new RabbitMqTransientErrorDetectionStrategy(), retryCount, minBackoff, maxBackoff, deltaBackoff) { }
    }
}
