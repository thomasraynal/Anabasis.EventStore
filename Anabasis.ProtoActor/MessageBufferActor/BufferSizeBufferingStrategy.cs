using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class BufferSizeBufferingStrategy : IBufferingStrategy
    {
        private readonly long _bufferMaxSize;

        public BufferSizeBufferingStrategy(long bufferMaxSize)
        {
            _bufferMaxSize = bufferMaxSize;
        }

        public void Reset()
        {
        }

        public bool ShouldConsumeBuffer(object message, object[] messageBuffer, IContext context)
        {
            return messageBuffer.Length >= _bufferMaxSize;
        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, object[] messageBuffer, IContext context)
        {
            return messageBuffer.Length >= _bufferMaxSize;
        }
    }
}
