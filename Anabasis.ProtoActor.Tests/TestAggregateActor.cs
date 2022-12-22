using Anabasis.Common;
using Anabasis.ProtoActor.AggregateActor;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Anabasis.ProtoActor.Tests
{
    public class TestAggregateActorConfiguration : IAggregateMessageHandlerActorConfiguration
    {
        public TestAggregateActorConfiguration(params string[] streamIds)
        {
            if (null == streamIds || streamIds.Length == 0)
            {
                throw new ArgumentException($"{streamIds} should not be empty");
            }

            StreamIds = streamIds;
        }

        public string[] StreamIds { get; set; }

        public bool KeepAppliedEventsOnAggregate { get; set; } = false;

        public UserCredentials? UserCredentials { get; set; }

        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;

        public bool UseSnapshot { get; set; }

        public bool SwallowUnkwownEvents { get; set; } = true;

        public TimeSpan IdleTimeoutFrequency { get; set; } = TimeSpan.FromSeconds(10);
    }

    public class TestAggregate : BaseAggregate
    {
    }

    public class TestAggregateEventOne : BaseAggregateEvent<TestAggregate>
    {
        public TestAggregateEventOne(string entityId) : base(entityId)
        {
        }

        public override void Apply([NotNull] TestAggregate entity)
        {
        }
    }

    public class TestAggregateEventTwo : BaseAggregateEvent<TestAggregate>
    {
        public TestAggregateEventTwo(string entityId) : base(entityId)
        {
        }

        public override void Apply([NotNull] TestAggregate entity)
        {
        }
    }

    public class TestAggregateActor : AggregateMessageHandlerProtoActorBase<TestAggregate, TestAggregateActorConfiguration>
    {
        public TestAggregateActor(TestAggregateActorConfiguration aggregateMessageHandlerActorConfiguration, IAggregateRepository<TestAggregate> aggregateRepository, IEventTypeProvider eventTypeProvider, ILoggerFactory? loggerFactory = null) : base(aggregateMessageHandlerActorConfiguration, aggregateRepository, eventTypeProvider, loggerFactory)
        {
        }
    }
}
