using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class AbsoluteTimeoutBufferingStrategy : IBufferingStrategy
    {
        private readonly TimeSpan _bufferConsumptionTimeout;
        private DateTime _lastConsumeBufferExecutionUtcDate;

        public AbsoluteTimeoutBufferingStrategy(TimeSpan bufferConsumptionTimeout)
        {
            _bufferConsumptionTimeout = bufferConsumptionTimeout;
            _lastConsumeBufferExecutionUtcDate = DateTime.UtcNow;
        }

        public void Reset()
        {
            _lastConsumeBufferExecutionUtcDate = DateTime.UtcNow;
        }

        public bool ShouldConsumeBuffer(object message, object[] messageBuffer, IContext context)
        {
            return DateTime.UtcNow >= _lastConsumeBufferExecutionUtcDate.Add(_bufferConsumptionTimeout);
        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, object[] messageBuffer, IContext context)
        {
            return DateTime.UtcNow >= _lastConsumeBufferExecutionUtcDate.Add(_bufferConsumptionTimeout);
        }
    }
}
