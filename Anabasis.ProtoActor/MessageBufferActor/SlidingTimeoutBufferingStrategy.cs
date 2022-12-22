using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class SlidingTimeoutBufferingStrategy : IBufferingStrategy
    {
        private readonly TimeSpan _absoluteTimeout;
        private readonly TimeSpan _slidingTimeout;
        private DateTime _lastMessageBufferizedUtcDate;

        public SlidingTimeoutBufferingStrategy(TimeSpan absoluteTimeout, TimeSpan slidingTimeout)
        {
            _absoluteTimeout = absoluteTimeout;
            _slidingTimeout = slidingTimeout;
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;
        }

        public void Reset()
        {
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;
        }

        public bool ShouldConsumeBuffer(object message, object[] messageBuffer, IContext context)
        {
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;

            return false;

        }

        public bool ShouldConsumeBuffer(BufferTimeoutDelayMessage timeoutMessage, object[] messageBuffer, IContext context)
        {

            if (_lastMessageBufferizedUtcDate.Add(_absoluteTimeout) >= DateTime.UtcNow)
            {
                if (_lastMessageBufferizedUtcDate.Add(_slidingTimeout) >= DateTime.UtcNow)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
