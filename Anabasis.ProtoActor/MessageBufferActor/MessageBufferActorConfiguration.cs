using Anabasis.ProtoActor.MessageHandlerActor;
using System;

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
