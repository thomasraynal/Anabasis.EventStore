﻿using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore2
{
    public class SomeDataAggregate : BaseAggregate
    {

        public SomeDataAggregate(string entityId)
        {
            EntityId = entityId;
        }

        public SomeDataAggregate()
        {
        }
    }

    public class SomeDataAggregatedEvent : BaseAggregateEvent<SomeDataAggregate>
    {

        public SomeDataAggregatedEvent(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
        }

        public override void Apply(SomeDataAggregate entity)
        {
        }
    }


    public class TestSomeDataSubscribeToAllAggregateActor : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestSomeDataSubscribeToAllAggregateActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration<SomeDataAggregate> catchupCacheConfiguration, IEventTypeProvider<SomeDataAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }

    }

    public class TestSomeDataSubscribeToManyAggregateActor : SubscribeToManyStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestSomeDataSubscribeToManyAggregateActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, MultipleStreamsCatchupCacheConfiguration<SomeDataAggregate> catchupCacheConfiguration, IEventTypeProvider<SomeDataAggregate> eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }

    }


    [TestFixture]
    public class TestNewVersion
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private DummyLoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;


        [OneTimeSetUp]
        public async Task Setup()
        {

            _userCredentials = new UserCredentials("admin", "changeit");
            _connectionSettings = ConnectionSettings.Create()
             .UseDebugLogger()
             .SetDefaultUserCredentials(_userCredentials)
             .KeepRetrying()
             .Build();

            _loggerFactory = new DummyLoggerFactory();

            _clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

            await _clusterVNode.StartAsync(true);

        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _clusterVNode.StopAsync();
        }

        [Test, Order(0)]
        public async Task ShouldCreateASubscribeToAllStreamsActor()
        {
            var embeddedEventStoreConnection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var actorConfiguration = new ActorConfiguration();
            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration<SomeDataAggregate>(Position.Start)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var eventStoreConnectionStatusMonitor = new EventStoreConnectionStatusMonitor(embeddedEventStoreConnection, _loggerFactory);
            var dummyLoggerFactory = new DummyLoggerFactory();

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() =>
            {
                return new[] { typeof(SomeDataAggregatedEvent) };
            });

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                embeddedEventStoreConnection,
                eventStoreConnectionStatusMonitor,
                dummyLoggerFactory);

            var testSomeDataAggregateActor = new TestSomeDataSubscribeToAllAggregateActor(
                actorConfiguration,
                eventStoreConnectionStatusMonitor,
                allStreamsCatchupCacheConfiguration,
                defaultEventTypeProvider,
                dummyLoggerFactory
                );

            await testSomeDataAggregateActor.ConnectToEventStream();

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream", Guid.NewGuid()));

            await Task.Delay(1000);

            var aggregate = testSomeDataAggregateActor.GetCurrent("stream");

            Assert.NotNull(aggregate);
            Assert.AreEqual(1, aggregate.AppliedEvents.Length);

        }

        [Test, Order(1)]
        public async Task ShouldCreateASubscribeToManyStreamsActor()
        {
            var embeddedEventStoreConnection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var actorConfiguration = new ActorConfiguration();

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<SomeDataAggregate>(
                "stream1", "stream2", "stream3"
                )
            {
                KeepAppliedEventsOnAggregate = true,
            };

            var eventStoreConnectionStatusMonitor = new EventStoreConnectionStatusMonitor(embeddedEventStoreConnection, _loggerFactory);
            var dummyLoggerFactory = new DummyLoggerFactory();

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() =>
            {
                return new[] { typeof(SomeDataAggregatedEvent) };
            });

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                embeddedEventStoreConnection,
                eventStoreConnectionStatusMonitor,
                dummyLoggerFactory);

            var testSomeDataAggregateActor = new TestSomeDataSubscribeToManyAggregateActor(
                actorConfiguration,
                eventStoreConnectionStatusMonitor,
                multipleStreamsCatchupCacheConfiguration,
                defaultEventTypeProvider,
                dummyLoggerFactory
                );

            await testSomeDataAggregateActor.ConnectToEventStream();

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream1", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream2", Guid.NewGuid()));

            await Task.Delay(1000);

            var aggregates = testSomeDataAggregateActor.GetCurrents();

            Assert.AreEqual(2,aggregates.Length);

            foreach(var aggregate in aggregates)
            {
                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
            }

           
        }

        [Test, Order(2)]
        public async Task ShouldCreateASubscribeToManyStreamsActorAndAddNewStream()
        {
            var embeddedEventStoreConnection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var actorConfiguration = new ActorConfiguration();

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<SomeDataAggregate>(
                "stream1", "stream2", "stream"
                )
            {
                KeepAppliedEventsOnAggregate = true,
            };

            var eventStoreConnectionStatusMonitor = new EventStoreConnectionStatusMonitor(embeddedEventStoreConnection, _loggerFactory);
            var dummyLoggerFactory = new DummyLoggerFactory();

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() =>
            {
                return new[] { typeof(SomeDataAggregatedEvent) };
            });

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                embeddedEventStoreConnection,
                eventStoreConnectionStatusMonitor,
                dummyLoggerFactory);

            var testSomeDataAggregateActor = new TestSomeDataSubscribeToManyAggregateActor(
                actorConfiguration,
                eventStoreConnectionStatusMonitor,
                multipleStreamsCatchupCacheConfiguration,
                defaultEventTypeProvider,
                dummyLoggerFactory
                );

            await testSomeDataAggregateActor.ConnectToEventStream();

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream1", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream2", Guid.NewGuid()));

            await Task.Delay(1000);

            await testSomeDataAggregateActor.AddEventStoreStreams("stream");

            await Task.Delay(1000);

            var aggregates = testSomeDataAggregateActor.GetCurrents();

            Assert.AreEqual(3, aggregates.Length);

            foreach (var aggregate in aggregates)
            {
                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
            }

            var multipleStreamsCatchupCacheConfiguration2 = new MultipleStreamsCatchupCacheConfiguration<SomeDataAggregate>(
                "stream1", "stream2", "stream", "stream3"
                )
            {
                KeepAppliedEventsOnAggregate = true,
            };

            var testSomeDataAggregateActor2 = new TestSomeDataSubscribeToManyAggregateActor(
                    actorConfiguration,
                    eventStoreConnectionStatusMonitor,
                    multipleStreamsCatchupCacheConfiguration2,
                    defaultEventTypeProvider,
                    dummyLoggerFactory
                    );

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream3", Guid.NewGuid()));

            await Task.Delay(1000);

            await testSomeDataAggregateActor2.ConnectToEventStream();

            await Task.Delay(1000);

            aggregates = testSomeDataAggregateActor2.GetCurrents();

            Assert.AreEqual(4, aggregates.Length);

            foreach (var aggregate in aggregates)
            {
                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
            }

        }

        [Test, Order(3)]
        public async Task ShouldCreateAndUseABus()
        {
            var embeddedEventStoreConnection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var actorConfiguration = new ActorConfiguration();
            var eventStoreConnectionStatusMonitor = new EventStoreConnectionStatusMonitor(embeddedEventStoreConnection, _loggerFactory);
            var dummyLoggerFactory = new DummyLoggerFactory();

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() =>
            {
                return new[] { typeof(SomeDataAggregatedEvent) };
            });

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                embeddedEventStoreConnection,
                eventStoreConnectionStatusMonitor,
                dummyLoggerFactory);

            var eventStoreBus = new EventStoreBus(eventStoreConnectionStatusMonitor, eventStoreRepository);

            await eventStoreBus.WaitUntilConnected();

            var eventList = new List<IMessage>();

            var subscription = eventStoreBus.SubscribeToManyStreams(new[] { "stream1", "stream2" }, (message, timeout) =>
            {
                eventList.Add(message);

            }, defaultEventTypeProvider);

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream1", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream2", Guid.NewGuid()));

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new SomeDataAggregatedEvent("stream2", Guid.NewGuid()));

            await Task.Delay(1000);

            Assert.AreEqual(3, eventList.Count);

        }

    }
}
