using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Cache;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Common.Options;
using EventStore.Core;
using EventStore.ClientAPI.SystemData;
using Anabasis.EventStore.EventProvider;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Hosting;
using Anabasis.Common;
using Anabasis.EventStore.Mvc.Factories;
using Anabasis.Common.Configuration;

namespace Anabasis.EventStore.Tests
{

    public class TestStatelessActorOneMvc : BaseEventStoreStatelessActor
    {
        public TestStatelessActorOneMvc(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
        }

        public TestStatelessActorOneMvc(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();


        public Task Handle(AgainSomeMoreData againSomeMoreData)
        {
            Events.Add(againSomeMoreData);

            return Task.CompletedTask;
        }

        public Task Handle(SomeMoreData someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

    }

    public class TestStatefulActorOneMvc : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestStatefulActorOneMvc(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestStatefulActorOneMvc(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();

        public Task Handle(AgainSomeMoreData againSomeMoreData)
        {
            Events.Add(againSomeMoreData);

            return Task.CompletedTask;
        }

        public Task Handle(SomeMoreData someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

    }

    public class TestBusRegistrationStatefullActor : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestBusRegistrationStatefullActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestBusRegistrationStatefullActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();



        public Task Handle(AgainSomeMoreData againSomeMoreData)
        {
            Events.Add(againSomeMoreData);

            return Task.CompletedTask;
        }

        public Task Handle(SomeMoreData someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

    }

    public static class TestBed
    {
        public static ClusterVNode ClusterVNode { get; }
        public static UserCredentials UserCredentials { get; }
        public static ConnectionSettings ConnectionSettings { get; }

        static TestBed()
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

    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            var eventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) });

            services.AddWorld(TestBed.ClusterVNode, TestBed.ConnectionSettings)

                    .AddStatefulActor<TestStatefulActorOneMvc, SomeDataAggregate>(ActorConfiguration.Default)
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllStreams()
                    .CreateActor()

                   .AddStatefulActor<TestBusRegistrationStatefullActor, SomeDataAggregate>(ActorConfiguration.Default)
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllStreams()
                    .CreateActor()

                   .AddStatelessActor<TestStatelessActorOneMvc>(ActorConfiguration.Default)
                    .WithSubscribeFromEndToAllStreams()
                    .CreateActor();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWorld();
        }
    }

    public class TestNetCoreMvc
    {
        private TestServer _testServer;
        private IWebHost _host;

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _host.Dispose();
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
                        .UseStartup<TestStartup>();

            _testServer = new TestServer(builder);
            _host = _testServer.Host;

        }

        [Test, Order(0)]
        public void ShouldCheckThatAllActorsAreCreated()
        {

            var testStatefulActorOneMvc = _host.Services.GetService<TestStatefulActorOneMvc>();
            var testStatefulActorTwoMvc = _host.Services.GetService<TestBusRegistrationStatefullActor>();
            var testStatelessActorOneMvc = _host.Services.GetService<TestStatelessActorOneMvc>();

            Assert.NotNull(testStatefulActorOneMvc);
            Assert.NotNull(testStatefulActorTwoMvc);
            Assert.NotNull(testStatelessActorOneMvc);

            Assert.True(testStatefulActorOneMvc.IsConnected);
            Assert.True(testStatefulActorTwoMvc.IsConnected);
            Assert.True(testStatelessActorOneMvc.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldGenerateAnEventAndUpdateAll()
        {
            var streamOne = "stream-one";
            var streamTwo = "stream-two";

            var testStatefulActorOneMvc = _host.Services.GetService<TestStatefulActorOneMvc>();
            var testStatefulActorTwoMvc = _host.Services.GetService<TestBusRegistrationStatefullActor>();
            var testStatelessActorOneMvc = _host.Services.GetService<TestStatelessActorOneMvc>();

            await Task.Delay(200);

            await testStatelessActorOneMvc.EmitEventStore(new SomeMoreData(Guid.NewGuid(), streamOne));
            await testStatelessActorOneMvc.EmitEventStore(new AgainSomeMoreData(Guid.NewGuid(), streamOne));
            await testStatelessActorOneMvc.EmitEventStore(new AgainSomeMoreData(Guid.NewGuid(), streamTwo));

            await Task.Delay(200);

            Assert.AreEqual(3, testStatefulActorOneMvc.Events.Count);
            Assert.AreEqual(3, testStatefulActorTwoMvc.Events.Count);
            Assert.AreEqual(3, testStatelessActorOneMvc.Events.Count);

            Assert.AreEqual(0, testStatefulActorOneMvc.State.GetCurrents().Length);
            Assert.AreEqual(0, testStatefulActorTwoMvc.State.GetCurrents().Length);

            var aggregateIdOne = Guid.NewGuid();

            await testStatelessActorOneMvc.EmitEventStore(new SomeData($"{aggregateIdOne}", Guid.NewGuid()));
            await testStatelessActorOneMvc.EmitEventStore(new SomeData($"{aggregateIdOne}", Guid.NewGuid()));
            await testStatelessActorOneMvc.EmitEventStore(new SomeData($"{Guid.NewGuid()}", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(2, testStatefulActorOneMvc.State.GetCurrents().Length);
            Assert.AreEqual(2, testStatefulActorTwoMvc.State.GetCurrents().Length);
        }

    }
}
