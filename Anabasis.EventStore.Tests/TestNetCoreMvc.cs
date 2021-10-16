using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;
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
using System.Threading;

namespace Anabasis.EventStore.Tests
{

    public class TestStatelessActorOneMvc : BaseStatelessActor
    {
        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestStatelessActorOneMvc(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
        }

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

    public class TestStatefulActorOneMvc : TestStatefulActorTwoMvc
    {
        public TestStatefulActorOneMvc(IEventStoreAggregateRepository<Guid> eventStoreRepository, IEventStoreCache<Guid, SomeDataAggregate<Guid>> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestStatefulActorOneMvc(IEventStoreAggregateRepository<Guid> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }

    public class TestStatefulActorTwoMvc : BaseStatefulActor<Guid, SomeDataAggregate<Guid>>
    {
        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestStatefulActorTwoMvc(IEventStoreAggregateRepository<Guid> eventStoreRepository, IEventStoreCache<Guid, SomeDataAggregate<Guid>> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestStatefulActorTwoMvc(IEventStoreAggregateRepository<Guid> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }

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

            services.AddLogging();

            var eventTypeProvider = new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>), typeof(SomeMoreData), typeof(AgainSomeMoreData) });

            services.AddWorld(TestBed.ClusterVNode, TestBed.ConnectionSettings)

                    .AddStatefulActor<TestStatefulActorOneMvc, Guid, SomeDataAggregate<Guid>>()
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllQueue()
                    .CreateActor()

                   .AddStatefulActor<TestStatefulActorTwoMvc, Guid, SomeDataAggregate<Guid>>()
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllQueue()
                    .CreateActor()

                   .AddStatelessActor<TestStatelessActorOneMvc>()
                    .WithSubscribeFromEndToAllQueue()
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

            var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            logging.AddDebug();
                        })
                        .UseStartup<TestStartup>();

            _testServer = new TestServer(builder);
            _host = _testServer.Host;

            await TestBed.Start();

        }

        [Test, Order(0)]
        public void ShouldCheckThatAllActorsAreCreated()
        {
            var testStatefulActorOneMvc = _host.Services.GetService<TestStatefulActorOneMvc>();
            var testStatefulActorTwoMvc = _host.Services.GetService<TestStatefulActorTwoMvc>();
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
            var testStatefulActorTwoMvc = _host.Services.GetService<TestStatefulActorTwoMvc>();
            var testStatelessActorOneMvc = _host.Services.GetService<TestStatelessActorOneMvc>();

            await Task.Delay(500);

            await testStatefulActorOneMvc.Emit(new SomeMoreData(Guid.NewGuid(), streamOne));
            await testStatefulActorOneMvc.Emit(new AgainSomeMoreData(Guid.NewGuid(), streamOne));
            await testStatefulActorOneMvc.Emit(new AgainSomeMoreData(Guid.NewGuid(), streamTwo));

            await Task.Delay(1000);

            Assert.Equals(3, testStatefulActorOneMvc.Events.Count);
            Assert.Equals(3, testStatefulActorTwoMvc.Events.Count);
            Assert.Equals(3, testStatelessActorOneMvc.Events.Count);

            Assert.Equals(2, testStatefulActorOneMvc.State.GetCurrents().Length);
            Assert.Equals(2, testStatefulActorTwoMvc.State.GetCurrents().Length);
        }

    }
}
