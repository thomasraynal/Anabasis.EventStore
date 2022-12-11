using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public class BufferSizeBufferingStrategy : IBufferingStrategy
    {
        private long _currentBufferSize;
        private readonly long _bufferMaxSize;

        public BufferSizeBufferingStrategy(long bufferMaxSize)
        {
            _bufferMaxSize = bufferMaxSize;
        }

        public void Reset()
        {
            _currentBufferSize = 0;
        }

        public bool ShouldConsumeBuffer(object message, IContext context)
        {
            _currentBufferSize++;

            return _currentBufferSize >= _bufferMaxSize;
        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context)
        {
            return _currentBufferSize >= _bufferMaxSize;
        }
    }
}
