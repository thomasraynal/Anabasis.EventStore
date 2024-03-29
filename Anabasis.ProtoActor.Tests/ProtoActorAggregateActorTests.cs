﻿using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Bus;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.ProtoActor.AggregateActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Anabasis.ProtoActor.Queue;
using Anabasis.ProtoActor.System;
using DynamicData;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Proto;
using Proto.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{

    public static class TestEventStoreExtensions
    {
        public static IDisposable SubscribeToEventStore(this IProtoActorSystem protoActorSystem, string streamId, int position, IEventTypeProvider eventTypeProvider)
        {

            var eventStoreBus = protoActorSystem.GetConnectedBus<IEventStoreBus>();

            return eventStoreBus.SubscribeToManyStreams(new[] { new StreamIdAndPosition(streamId, position) }, async (messages, timeout) =>
             {
                 await protoActorSystem.Send(messages);

             }, eventTypeProvider);

        }
    }

    [TestFixture]
    public class ProtoActorAggregateActorTests
    {
        private readonly string[] _streamIds = new[] { "streamIdOne", "streamIdTwo" };

        private async Task<Container> GetContainer()
        {
            var eventTypeProvider = new DefaultEventTypeProvider(() =>
            {
                return new[]
                {
                    typeof(TestAggregateEventOne),
                    typeof(TestAggregateEventTwo),
                    typeof(BusOneEvent)
                };
            });

            var testAggregateActorConfiguration = new TestAggregateActorConfiguration(_streamIds.Select(streamId =>
            {
                return new StreamIdAndPosition(streamId, 0);

            }).ToArray());

            var userCredentials = new UserCredentials("admin", "changeit");

            var connectionSettings = ConnectionSettings.Create()
            .UseDebugLogger()
               .SetDefaultUserCredentials(userCredentials)
               .KeepRetrying()
               .Build();

            var loggerFactory = new DummyLoggerFactory();

            var clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();


            await clusterVNode.StartAsync(true);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              loggerFactory);

            var container = new Container(serviceRegistry =>
            {
                serviceRegistry.For<TestActor>().Use<TestActor>();
                serviceRegistry.For<TestMessageBufferActor>().Use<TestMessageBufferActor>();
                serviceRegistry.For<ILoggerFactory>().Use<DummyLoggerFactory>();
                serviceRegistry.For<IMessageHandlerActorConfiguration>().Use<MessageHandlerActorConfiguration>();
                serviceRegistry.For<IEventTypeProvider>().Use(eventTypeProvider);
                serviceRegistry.For<IEventStoreAggregateRepository>().Use(eventStoreRepository);
                serviceRegistry.For<IConnectionStatusMonitor<IEventStoreConnection>>().Use(connectionMonitor);
                serviceRegistry.For<IEventStoreRepositoryConfiguration>().Use(eventStoreRepositoryConfiguration);
                serviceRegistry.For<IEventStoreRepository>().Use(eventStoreRepository);
                serviceRegistry.For<IEventStoreBus>().Use<EventStoreBus>();
                serviceRegistry.For<TestAggregateActorConfiguration>().Use(testAggregateActorConfiguration);

            });

            return container;
        }

        private static class Decider
        {
            public static SupervisorDirective Decide(PID pid, Exception reason)
            {
                return SupervisorDirective.Restart;
            }
        }

        [Test]
        public async Task ShouldTestActorRestart()
        {
            var container = await GetContainer();
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(10, true);
            var protoActoSystem = new ProtoActorSystem(new OneForOneStrategy(Decider.Decide, 1, null),
                  protoActorPoolDispatchQueueConfiguration,
                  container.ServiceProvider,
                  new DummyLoggerFactory());

            var eventStoreRepository = container.GetService<IEventStoreRepository>();
            var eventStoreBus = container.GetService<IEventStoreBus>();
            var eventTypeProvider = container.GetService<IEventTypeProvider>();

            var actorPid = protoActoSystem.CreateActors<TestAggregateActor>(1);

            var actor = protoActoSystem.GetSystemSpawnActor<TestAggregateActor>(actorPid[0]);

            var firstActorInstanceId = actor.Id;

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(eventStoreBus);
            await protoActoSystem.ConnectTo(busOne);
            protoActoSystem.SubscribeToBusOne();

            await Task.Delay(1000);

            for (var i = 0; i < 2; i++)
            {
                await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            }

            await Task.Delay(500);

            busOne.Emit(new FaultyBusOneMessage());

            await Task.Delay(500);

            actor = protoActoSystem.GetSystemSpawnActor<TestAggregateActor>(actorPid[0]);

            var secondActorInstanceId = actor.Id;

            Assert.AreNotEqual(firstActorInstanceId, secondActorInstanceId);

        }


        [Test]
        public async Task ShouldCreateAnAggregatedActorAndCrashIt()
        {
            var container = await GetContainer();

            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(10, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy,
                  protoActorPoolDispatchQueueConfiguration,
                  container.ServiceProvider,
                  new DummyLoggerFactory());

            var eventStoreRepository = container.GetService<IEventStoreRepository>();
            var eventStoreBus = container.GetService<IEventStoreBus>();
            var eventTypeProvider = container.GetService<IEventTypeProvider>();

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(eventStoreBus);
            await protoActoSystem.ConnectTo(busOne);
            protoActoSystem.SubscribeToBusOne();

            var allmesssages = new List<IMessage>();

            var actorPid = protoActoSystem.CreateActors<TestAggregateActor>(1);

            var actor = protoActoSystem.GetSystemSpawnActor<TestAggregateActor>(actorPid[0]);

            var firstActorInstanceId = actor.Id;

            for (var i = 0; i < 10; i++)
            {
                await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            }

            busOne.Emit(new FaultyBusOneMessage());

            await Task.Delay(1000);

            killSwitch.Received(1).KillProcess(Arg.Any<string>(), Arg.Any<Exception>());

        }

        [Test]
        public async Task ShouldCreateAnAggregatedActorAndConsumeEvents()
        {
            var container = await GetContainer();

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(10, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy,
              protoActorPoolDispatchQueueConfiguration,
              container.ServiceProvider,
              new DummyLoggerFactory());


            var eventStoreRepository = container.GetService<IEventStoreRepository>();
            var eventStoreBus = container.GetService<IEventStoreBus>();
            var eventTypeProvider = container.GetService<IEventTypeProvider>();

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(eventStoreBus);
            await protoActoSystem.ConnectTo(busOne);
            protoActoSystem.SubscribeToBusOne();

            for (var i = 0; i < 10; i++)
            {
                await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            }

            var actorPid  =protoActoSystem.CreateActors<TestAggregateActor>(1);


            var actor = protoActoSystem.GetSystemSpawnActor<TestAggregateActor>(actorPid[0]);

            var aggregateMessages = new List<Change<TestAggregate, string>>();

            actor.SourceCache.Connect().Subscribe((changes) =>
            {
                aggregateMessages.AddRange(changes.ToArray());
            });

            await Task.Delay(1000);

            var allmesssages = new List<IMessage>();

            Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    var messsages = Enumerable.Range(0, 5).Select(_ => new BusOneMessage(new BusOneEvent(i))).ToArray();
                    allmesssages.AddRange(messsages);
                    busOne.Emit(messsages);
                }
            });

            for (var i = 0; i < 10; i++)
            {
                await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            }


            Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                {
                    var messsages = Enumerable.Range(0, 5).Select(_ => new BusOneMessage(new BusOneEvent(i))).ToArray();
                    allmesssages.AddRange(messsages);
                    busOne.Emit(messsages);
                }
            });


            await Task.Delay(6000);

            Assert.IsTrue(allmesssages.All(m => m.IsAcknowledged));

            Assert.AreEqual(allmesssages.Count, protoActoSystem.ProcessedMessagesCount);
            Assert.AreEqual(allmesssages.Count, protoActoSystem.ReceivedMessagesCount);
            Assert.AreEqual(allmesssages.Count, protoActoSystem.AcknowledgeMessagesCount);
            Assert.AreEqual(allmesssages.Count, protoActoSystem.EnqueuedMessagesCount);

            //Assert.AreEqual(allmesssages.Count, protoActoSystem2.ProcessedMessagesCount);
            //Assert.AreEqual(allmesssages.Count, protoActoSystem2.ReceivedMessagesCount);
            //Assert.AreEqual(allmesssages.Count, protoActoSystem2.AcknowledgeMessagesCount);
            //Assert.AreEqual(allmesssages.Count, protoActoSystem2.EnqueuedMessagesCount);


        }
    }
}
