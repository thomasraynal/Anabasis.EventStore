using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageHandlerActor
{
    public class MessageHandlerActorConfiguration : IMessageHandlerActorConfiguration
    {
        public MessageHandlerActorConfiguration(bool swallowUnkwownEvents = true, TimeSpan? idleTimeoutFrequency = null)
        {
            SwallowUnkwownEvents = swallowUnkwownEvents;
            IdleTimeoutFrequency = idleTimeoutFrequency ?? TimeSpan.FromSeconds(30);
        }

        public bool SwallowUnkwownEvents { get; set; }
        public TimeSpan IdleTimeoutFrequency { get; set; }
    }
}
