using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class MessageBufferActorConfiguration : MessageHandlerActorConfiguration, IMessageBufferActorConfiguration
    {

        public MessageBufferActorConfiguration(TimeSpan reminderSchedulingDelay, bool swallowUnkwownEvents = true, TimeSpan? idleTimeoutFrequency = null, IBufferingStrategy[]? bufferingStrategies = null)
            : base(swallowUnkwownEvents, idleTimeoutFrequency)
        {
            BufferingStrategies = bufferingStrategies ?? new[] { new BufferSizeBufferingStrategy(1) };
            ReminderSchedulingDelay = reminderSchedulingDelay;
        }

        public IBufferingStrategy[] BufferingStrategies { get; set; }
        public TimeSpan ReminderSchedulingDelay { get; set; }

    }
}
