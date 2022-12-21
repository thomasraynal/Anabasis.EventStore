using Anabasis.Common;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{

    public class TestMessageBufferActorConfiguration : MessageBufferActorConfiguration
    {
        public TestMessageBufferActorConfiguration(TimeSpan eventConsumptionDelay, TimeSpan reminderSchedulingDelay, bool swallowUnkwownEvents = true, TimeSpan? idleTimeoutFrequency = null, IBufferingStrategy[]? bufferingStrategies = null) : base(reminderSchedulingDelay, swallowUnkwownEvents, idleTimeoutFrequency, bufferingStrategies)
        {
            EventConsumptionDelay = eventConsumptionDelay;
        }

        public TimeSpan EventConsumptionDelay { get; set; }

    }

    public class TestMessageBufferActor : MessageBufferHandlerProtoActorBase<TestMessageBufferActorConfiguration>
    {
        public TestMessageBufferActor(TestMessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null) : base(messageBufferActorConfiguration, loggerFactory)
        {
        }

        public async Task Handle(IEvent[] events)
        {
            await Task.Delay(MessageBufferActorConfiguration.EventConsumptionDelay);

            Debug.WriteLine($"Handle {events.Length} message(s)");
        }

    }

}
