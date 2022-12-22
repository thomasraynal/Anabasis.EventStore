using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public interface IBufferingStrategy
    {
        void Reset();
        bool ShouldConsumeBuffer(object currentMessage, object[] messageBuffer, IContext context);
        bool ShouldConsumeBuffer(BufferTimeoutDelayMessage timeoutMessage, object[] messageBuffer, IContext context);
    }
}
