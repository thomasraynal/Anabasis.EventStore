using Anabasis.Common;
using Anabasis.Common.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class EventA : BaseEvent
    {
        public EventA(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
        }
    }

    public class EventB : BaseEvent
    {
        public EventB(Guid correlationId, string streamId) : base(correlationId, streamId)
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
            var testStatelessActor = new TestStatelessActor(new ActorConfiguration(2, 10));

            var getEventA = new Func<EventA>(() => new EventA(Guid.NewGuid(), "eventA"));
            var getEventB = new Func<EventB>(() => new EventB(Guid.NewGuid(), "eventB"));

            var cancellationTokenSource = new CancellationTokenSource();

            var producerOneTask = Task.Run(() =>
            {
                while (true)
                {
                     testStatelessActor.OnEventReceived(getEventA());
                }

            }, cancellationTokenSource.Token);

            var producerTwoTask = Task.Run( () =>
            {
                while (true)
                {
                     testStatelessActor.OnEventReceived(getEventB());

                }
            }, cancellationTokenSource.Token);


            await Task.Delay(5000);

            Assert.Greater(testStatelessActor.ProcessedEvents.Count, 0);

        }
    }
}
