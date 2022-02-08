using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc.Factories;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{

    public class TestBusRegistrationStatelessActorMvc: BaseEventStoreStatelessActor
    {
        public TestBusRegistrationStatelessActorMvc(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
        }

        public TestBusRegistrationStatelessActorMvc(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();


        public Task Handle(SomeData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }

    }

    public class TestBusRegistrationStatefullActorMvc : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestBusRegistrationStatefullActorMvc(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestBusRegistrationStatefullActorMvc(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();


        public Task Handle(SomeData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }

    }

    public static class TestBusRegistrationTestBed
    {
        public static ClusterVNode ClusterVNode { get; }
        public static UserCredentials UserCredentials { get; }
        public static ConnectionSettings ConnectionSettings { get; }

        static TestBusRegistrationTestBed()
        {
            UserCredentials = new UserCredentials("admin", "changeit");


            ConnectionSettings = ConnectionSettings.Create()
                .UseDebugLogger()
                .SetDefaultUserCredentials(UserCredentials)
                .KeepRetrying()
                .Build();


            ClusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

        }
        public static async Task Start()
        {
            await ClusterVNode.StartAsync(true);
        }

        public static async Task Dispose()
        {
            await ClusterVNode.StopAsync();
        }
    }

    public class TestBusRegistrationStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            var eventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) });

            services.AddSingleton<IDummyBus, DummyBus>();

            services.AddWorld(TestBed.ClusterVNode, TestBed.ConnectionSettings)

                   //.AddStatefulActor<TestBusRegistrationStatefullActor, SomeDataAggregate>(ActorConfiguration.Default)
                   // .WithReadAllFromStartCache(
                   //         catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                   //         eventTypeProvider: eventTypeProvider)
                   // .WithSubscribeFromEndToAllStreams()
                   // .CreateActor()

                   .AddStatelessActor<TestBusRegistrationStatelessActorMvc>(ActorConfiguration.Default)
                   .WithSubscribeFromEndToAllStreams()
                   .WithBus<IDummyBus>((actor, bus) =>
                    {
                        actor.SubscribeDummyBus("somesubject");
                    })
                   .CreateActor();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWorld();
        }
    }

    [TestFixture]
    public class TestEventStoreActorBuilderBusRegistration
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private ILoggerFactory _loggerFactory;

        private Guid _correlationId = Guid.NewGuid();
        private TestServer _testServer;
        private IWebHost _host;

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
            _host.Dispose();

            await _clusterVNode.StopAsync();
            await TestBed.Dispose();
        }

        [OneTimeSetUp]
        public async Task SetupFixture()
        {
            await TestBed.Start();

            var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            logging.AddDebug();
                        })
                        .UseStartup<TestBusRegistrationStartup>();

            _testServer = new TestServer(builder);
            _host = _testServer.Host;

        }
        [Test, Order(0)]
        public async Task ShouldCreateAnEventStoreActorAndRegisterABus()
        {
            var testBusRegistrationStatelessActorMvc = _host.Services.GetService<TestBusRegistrationStatelessActorMvc>();
            //var testBusRegistrationStatefullActorMvc = _host.Services.GetService<TestBusRegistrationStatefullActorMvc>();

            Assert.NotNull(testBusRegistrationStatelessActorMvc);
            //Assert.NotNull(testBusRegistrationStatefullActorMvc);

        }

        [Test, Order(1)]
        public async Task ShouldEmitAnEventThroughtTheBus()
        {
            var testBusRegistrationStatelessActorMvc = _host.Services.GetService<TestBusRegistrationStatelessActorMvc>();
            Assert.NotNull(testBusRegistrationStatelessActorMvc);

            var bus = testBusRegistrationStatelessActorMvc.GetConnectedBus<IDummyBus>();

            bus.Push(new SomeData("entityId", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(1, testBusRegistrationStatelessActorMvc.Events.Count);

        }

    }
}
