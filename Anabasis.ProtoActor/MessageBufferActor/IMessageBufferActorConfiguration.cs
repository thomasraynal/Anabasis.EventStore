using System;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public interface IMessageBufferActorConfiguration : IMessageHandlerActorConfiguration
    {
        IBufferingStrategy[] BufferingStrategies { get; set; }
        TimeSpan ReminderSchedulingDelay { get; set; }
    }
}