using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageHandlerActor
{
    public interface IMessageHandlerActorConfiguration
    {
        bool SwallowUnkwownEvents { get; }
        TimeSpan IdleTimeoutFrequency { get; }
    }
}
