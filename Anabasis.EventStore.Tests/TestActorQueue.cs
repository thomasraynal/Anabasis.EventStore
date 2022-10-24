using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Standalone;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class EventMessage : IMessage
    {
        public EventMessage(IEvent @event)
        {
            Content = @event;
        }

        public Guid MessageId { get; }

        public IEvent Content { get; }

        public Guid? TraceId { get; }

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }
        public Task NotAcknowledge(string reason = null)
        {
            return Task.CompletedTask;
        }
    }

    public class EventA : BaseEvent
    {
        public EventA(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class EventB : BaseEvent
    {
        public EventB(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class TestStatelessActor : BaseStatelessActor
    {
        public List<IEvent> ProcessedEvents { get; }

        public TestStatelessActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
            ProcessedEvents = new List<IEvent>();
        }

        public TestStatelessActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public async Task Handle(EventA eventA)
        {
            ProcessedEvents.Add(eventA);

            await Task.Delay(200);
        }

        public async Task Handle(EventB eventB)
        {
            ProcessedEvents.Add(eventB);

            await Task.Delay(200);
        }
    }

    [TestFixture]
    public class TestActorQueue
    {
        [Test]
        public async Task ShouldCreateAnActor()
        {
            var testStatelessActor = new TestStatelessActor(new ActorConfiguration(2, 10), new DummyLoggerFactory());

            var getEventA = new Func<EventMessage>(() => new EventMessage(new EventA(Guid.NewGuid(), "eventA")));
            var getEventB = new Func<EventMessage>(() => new EventMessage(new EventB(Guid.NewGuid(), "eventB")));

            var cancellationTokenSource = new CancellationTokenSource();

            var producerOneTask = Task.Run(() =>
            {
                while (true)
                {
                    testStatelessActor.OnMessageReceived(getEventA());
                }

            }, cancellationTokenSource.Token);

            var producerTwoTask = Task.Run(() =>
           {
               while (true)
               {
                   testStatelessActor.OnMessageReceived(getEventB());

               }
           }, cancellationTokenSource.Token);


            await Task.Delay(5000);

            Assert.Greater(testStatelessActor.ProcessedEvents.Count, 0);

        }
    }
}
