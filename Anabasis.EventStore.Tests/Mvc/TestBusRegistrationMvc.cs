using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.AspNet;
using Anabasis.EventStore.AspNet.Embedded;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Tests.Mvc;
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
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{

    public static class TestBusRegistrationMvcTestBed
    {
        static TestBusRegistrationMvcTestBed()
        {
            TestBed = new TestBed();
        }

        public static TestBed TestBed { get; }
    }
    public class TestBusRegistrationStatelessActorMvc : BaseStatelessActor
    {
        public TestBusRegistrationStatelessActorMvc(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestBusRegistrationStatelessActorMvc(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();

        public Task Handle(SomeMoreData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }
    }
    public class TestBusRegistrationEventStoreStatelessActorMvc : BaseStatelessActor
    {
        public TestBusRegistrationEventStoreStatelessActorMvc(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestBusRegistrationEventStoreStatelessActorMvc(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();

        public Task Handle(SomeMoreData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }

    }
    public class TestBusRegistrationEventStoreStatefullActorMvc : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestBusRegistrationEventStoreStatefullActorMvc(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public TestBusRegistrationEventStoreStatefullActorMvc(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();

        public Task Handle(SomeMoreData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }

    }
    public class TestBusRegistrationStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            var eventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeMoreData) });

            services.AddSingleton<IDummyBus, DummyBus>();
            services.AddSingleton<IEventStoreBus, EventStoreBus>();

            services.AddWorld(TestBusRegistrationMvcTestBed.TestBed.ClusterVNode, TestBusRegistrationMvcTestBed.TestBed.ConnectionSettings)

                   .AddEventStoreStatefulActor<TestBusRegistrationEventStoreStatefullActorMvc, SomeDataAggregate, AllStreamsCatchupCacheConfiguration>(eventTypeProvider)
                        .WithBus<IEventStoreBus>((actor, bus) => actor.SubscribeToAllStreams(Position.End))
                        .WithBus<IDummyBus>((actor, bus) => actor.SubscribeDummyBus("somesubject"))
                        .CreateActor()

                   .AddStatelessActor<TestBusRegistrationEventStoreStatelessActorMvc>(ActorConfiguration.Default)
                        .WithBus<IEventStoreBus>((actor, bus) => actor.SubscribeToAllStreams(Position.End))
                        .WithBus<IDummyBus>((actor, bus) =>
                        {
                            actor.SubscribeDummyBus("somesubject");
                        })
                        .CreateActor()

                   .AddStatelessActor<TestBusRegistrationStatelessActorMvc>(ActorConfiguration.Default)
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

    public static class TestActorBuilderBusRegistrationMvcTestBed
    {
        public static ClusterVNode ClusterVNode { get; }
        public static UserCredentials UserCredentials { get; }
        public static ConnectionSettings ConnectionSettings { get; }

        static TestActorBuilderBusRegistrationMvcTestBed()
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

        public static async Task Stop()
        {
            await ClusterVNode.StopAsync();
        }
    }


    [TestFixture]
    public class TestBusRegistrationMvc
    {

        private TestServer _testServer;
        private IWebHost _host;

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _host.Dispose();
            _testServer.Dispose();

            await TestBusRegistrationMvcTestBed.TestBed.Stop();
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            await TestBusRegistrationMvcTestBed.TestBed.Start();

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
        public void ShouldCreateAnEventStoreActorAndRegisterABus()
        {
            var testBusRegistrationEventStoreStatelessActorMvc = _host.Services.GetService<TestBusRegistrationEventStoreStatelessActorMvc>();
            var testBusRegistrationEventStoreStatefullActorMvc = _host.Services.GetService<TestBusRegistrationEventStoreStatefullActorMvc>();
            var testBusRegistrationStatelessActorMvc = _host.Services.GetService<TestBusRegistrationStatelessActorMvc>();

            Assert.NotNull(testBusRegistrationEventStoreStatelessActorMvc);
            Assert.NotNull(testBusRegistrationEventStoreStatefullActorMvc);
            Assert.NotNull(testBusRegistrationStatelessActorMvc);
        }

        [Test, Order(1)]
        public async Task ShouldEmitAnEventThroughtTheBus()
        {
            var testBusRegistrationEventStoreStatelessActorMvc = _host.Services.GetService<TestBusRegistrationEventStoreStatelessActorMvc>();
            var testBusRegistrationEventStoreStatefullActorMvc = _host.Services.GetService<TestBusRegistrationEventStoreStatefullActorMvc>();
            var testBusRegistrationStatelessActorMvc = _host.Services.GetService<TestBusRegistrationStatelessActorMvc>();

            Assert.NotNull(testBusRegistrationEventStoreStatelessActorMvc);
            Assert.NotNull(testBusRegistrationEventStoreStatefullActorMvc);
            Assert.NotNull(testBusRegistrationStatelessActorMvc);

            var bus = testBusRegistrationEventStoreStatelessActorMvc.GetConnectedBus<IDummyBus>();

            bus.Push(new SomeMoreData(Guid.NewGuid(), "entityId"));

            await Task.Delay(1000);

            Assert.AreEqual(1, testBusRegistrationEventStoreStatelessActorMvc.Events.Count);
            Assert.AreEqual(1, testBusRegistrationEventStoreStatefullActorMvc.Events.Count);
            Assert.AreEqual(1, testBusRegistrationStatelessActorMvc.Events.Count);
        }

    }
}
