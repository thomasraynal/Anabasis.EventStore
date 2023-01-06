using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anabasis.ProtoActor.AggregateActor;
using Anabasis.ProtoActor.System;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class GracefullyStopActorMessage
    {
        public static readonly GracefullyStopActorMessage Instance = new();
        private GracefullyStopActorMessage()
        {
        }
    }
}
