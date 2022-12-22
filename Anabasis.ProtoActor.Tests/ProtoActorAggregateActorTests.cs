using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.ProtoActor.AggregateActor;
using Anabasis.ProtoActor.MessageHandlerActor;
using Anabasis.ProtoActor.Queue;
using Anabasis.ProtoActor.System;
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
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{

    public static class BusOneExtension
    {
        public static IDisposable SubscribeToEventStore(this IProtoActorSystem protoActorSystem, IEventTypeProvider eventTypeProvider)
        {

            var eventStoreBus = protoActorSystem.GetConnectedBus<IEventStoreBus>();

            return eventStoreBus.SubscribeToAllStreams(Position.Start, async (messages, timeout) =>
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
                    typeof(TestAggregateEventTwo)
                };
            });

            var testAggregateActorConfiguration = new TestAggregateActorConfiguration(_streamIds);

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
                serviceRegistry.For<IAggregateRepository<TestAggregate>>().Use<AggregateRepository<TestAggregate>>();
                serviceRegistry.For<IEventStoreRepositoryConfiguration>().Use(eventStoreRepositoryConfiguration);
                serviceRegistry.For<IEventStoreRepository>().Use(eventStoreRepository);
                serviceRegistry.For<IEventStoreBus>().Use<EventStoreBus>();
                serviceRegistry.For<TestAggregateActorConfiguration>().Use(testAggregateActorConfiguration);
                
            });

            return container;
        }

        [Test]
        public async Task ShouldCreateAggregatedActor()
        {
            var container = await GetContainer();

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy,
              protoActorPoolDispatchQueueConfiguration,
              container.ServiceProvider,
              new DummyLoggerFactory());

            var eventStoreRepository = container.GetService<IEventStoreRepository>();
            var eventStoreBus = container.GetService<IEventStoreBus>();
            var eventTypeProvider = container.GetService<IEventTypeProvider>();

            await protoActoSystem.ConnectTo(eventStoreBus);

            await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[1]));

            var subscribeToEventStore = protoActoSystem.SubscribeToEventStore(eventTypeProvider);

            var pid = protoActoSystem.CreateActors<TestAggregateActor>(1);

            await Task.Delay(1000);

            await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[1]));

            await Task.Delay(1000);



        }
    }
}
