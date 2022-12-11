using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public interface IBufferingStrategy
    {
        void Reset();
        bool ShouldConsumeBuffer(object message, IContext context);
        bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context);
    }
}
