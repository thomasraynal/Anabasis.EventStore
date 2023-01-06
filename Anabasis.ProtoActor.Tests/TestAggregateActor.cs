using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Bus;
using Anabasis.ProtoActor.AggregateActor;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Proto;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{
    public class TestAggregateActorConfiguration : IAggregateMessageHandlerActorConfiguration
    {
        public TestAggregateActorConfiguration(params StreamIdAndPosition[] streamIdAndPositions)
        {
            if (null == streamIdAndPositions || streamIdAndPositions.Length == 0)
            {
                throw new ArgumentException($"{nameof(StreamIdAndPosition)} should not be empty");
            }

            StreamIdAndPositions = streamIdAndPositions;
        }

        public StreamIdAndPosition[] StreamIdAndPositions { get; set; }

        public bool KeepAppliedEventsOnAggregate { get; set; } = false;

        public UserCredentials? UserCredentials { get; set; }

        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;

        public bool UseSnapshot { get; set; }

        public bool SwallowUnkwownEvents { get; set; } = true;

        public TimeSpan IdleTimeoutFrequency { get; set; } = TimeSpan.FromSeconds(10);

        public bool CrashAppIfSubscriptionFail { get; set; }
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
        public TestAggregateActor(TestAggregateActorConfiguration aggregateMessageHandlerActorConfiguration, IEventStoreBus eventStoreBus, IEventTypeProvider eventTypeProvider, ISnapshotStore<TestAggregate>? snapshotStore = null, ISnapshotStrategy? snapshotStrategy = null, ILoggerFactory? loggerFactory = null) : base(aggregateMessageHandlerActorConfiguration, eventStoreBus, eventTypeProvider, snapshotStore, snapshotStrategy, loggerFactory)
        {
            InitializeSubscriptions();
        }

        public static ConcurrentDictionary<string, IMessage[]> MessageCache { get; } = new();

 
        private void InitializeSubscriptions()
        {
            SourceCache.Connect().Subscribe(changeSet =>
            {
              //  Logger?.LogInformation($"changeSet count => {changeSet.Count}");
            });
        }

        protected override Task OnMessageConsumed(IContext context)
        {
            //MessageCache.AddOrUpdate(context.Self.Id, new[] { (IMessage) context.Message } , (key, current) =>
            //{
            //    return current.Append(current);
            //});

            return Task.CompletedTask;
        }

        protected override Task OnStarted(IContext context)
        {
            return base.OnStarted(context);
        }

        public Task Handle(BusOneEvent busOneEvent)
        {
            Debug.WriteLine($"Received {nameof(BusOneEvent)}");
            return Task.CompletedTask;
        }
    }
}
