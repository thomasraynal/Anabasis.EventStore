using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Queue
{
    public class ProtoActorPoolDispatchQueueConfiguration : IProtoActorPoolDispatchQueueConfiguration
    {
        public ProtoActorPoolDispatchQueueConfiguration(int messageBufferMaxSize, bool crashAppOnError)
        {
            MessageBufferMaxSize = messageBufferMaxSize;
            CrashAppOnError = crashAppOnError;
        }

        public int MessageBufferMaxSize { get; }

        public double MessageBufferAbsoluteTimeoutInSecond { get; }

        public double MessageBufferSlidingTimeoutInSecond { get; }

        public bool CrashAppOnError { get; }
    }
}
