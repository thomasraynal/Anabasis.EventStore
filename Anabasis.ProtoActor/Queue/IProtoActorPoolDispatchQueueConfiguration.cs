using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Queue
{
    public interface IProtoActorPoolDispatchQueueConfiguration
    {
        int MessageBufferMaxSize { get; }
        bool CrashAppOnError { get; }
    }
}
