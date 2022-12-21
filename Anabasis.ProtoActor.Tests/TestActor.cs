using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{
    public class TestActorConfiguration : IMessageHandlerActorConfiguration
    {
        public TestActorConfiguration(TimeSpan eventConsumptionDelay, bool swallowUnkwownEvents = true, TimeSpan? idleTimeoutFrequency = null)
        {
            EventConsumptionDelay = eventConsumptionDelay;
            SwallowUnkwownEvents = swallowUnkwownEvents;
            IdleTimeoutFrequency = idleTimeoutFrequency ?? TimeSpan.FromSeconds(10);
        }

        public TimeSpan EventConsumptionDelay { get; set; }
        public bool SwallowUnkwownEvents { get; set; }
        public TimeSpan IdleTimeoutFrequency { get; set; }
    }

    public class TestActor : MessageHandlerProtoActorBase<TestActorConfiguration>
    {
        private readonly TestActorConfiguration _messageHandlerActorConfiguration;
        private readonly TestEventInterceptor _testEventInterceptor;

        public TestActor(TestActorConfiguration messageHandlerActorConfiguration, TestEventInterceptor testEventInterceptor, ILoggerFactory? loggerFactory = null) : base(messageHandlerActorConfiguration, loggerFactory)
        {
            _messageHandlerActorConfiguration = messageHandlerActorConfiguration;
            _testEventInterceptor = testEventInterceptor;
        }

        public async Task Handle(BusOneEvent busOneEvent)
        {
            await Task.Delay(_messageHandlerActorConfiguration.EventConsumptionDelay);

            _testEventInterceptor.AddEvent(Id, busOneEvent);

            Debug.WriteLine($"{Id} handle message {busOneEvent.EventNumber}");
        }
    }
}
