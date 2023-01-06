using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Bus;
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
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(1, true);

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

            var rand = new Random();

            //for (var i = 0; i < 10; i++)
            //{
            //    await eventStoreRepository.Emit(new TestAggregateEventOne(_streamIds[0]));
            //}



            protoActoSystem.CreateActors<TestAggregateActor>(1);


            await Task.Delay(1000);

            //Task.Run(() =>
            //{
                for (var i = 0; i < 10; i++)
                {
                    busOne.Emit(Enumerable.Range(1, 10).Select(_ => new BusOneMessage(new BusOneEvent(i))).ToArray());
                }
            //});


            //await Task.Delay(1000);




         //   protoActoSystem.CreateActors<TestAggregateActor>(1);


            //Task.Run(() =>
            //{
            //    for (var i = 0; i < 50; i++)
            //    {
            //        busOne.Emit(Enumerable.Range(0, rand.Next(1, 10)).Select(_ => new BusOneMessage(new BusOneEvent(i))).ToArray());
            //    }
            //});


            await Task.Delay(8000);

        }
    }
}
