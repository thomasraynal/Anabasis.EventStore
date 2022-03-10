using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using Anabasis.EventStore.Actor;
using Microsoft.Extensions.Hosting;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Anabasis.EventStore.Tests.Mvc;
using EventStore.ClientAPI;
using System.Threading;
using Anabasis.EventStore.AspNet.Embedded;
using Anabasis.EventStore.AspNet;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;

namespace Anabasis.EventStore.Tests
{

    public static class TestHealthChecksMvcTestBed
    {
        static TestHealthChecksMvcTestBed()
        {
            TestBed = new TestBed();
        }

        public static TestBed TestBed { get; }
    }

    public class TestWorkingBus : IBus
    {
        public bool IsConnected => true;

        public string BusId => nameof(TestWorkingBus);

        public bool IsInitialized => true;

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"{nameof(TestWorkingBus)}"));
        }

        public void Dispose()
        {
        }


        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }
    }

    public class TestFailingBus : IBus
    {
        public bool IsConnected => false;

        public bool IsFailing { get; set; } = true;

        public string BusId => nameof(TestFailingBus);

        public bool IsInitialized => false;

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>()
            {
                { "ConnectivityIssue", "boom!" }
            };

            //if (shouldThrowIfUnhealthy)
            //    throw new InvalidOperationException("not healthy");

            if (IsFailing)
                return Task.FromResult(HealthCheckResult.Unhealthy($"{nameof(TestFailingBus)}", data: data));

            return Task.FromResult(HealthCheckResult.Healthy($"{nameof(TestFailingBus)}"));
        }

        public void Dispose()
        {
        }


        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }
    }

    public class TestStatelessActorOneHealthChecksMvc : BaseStatelessActor
    {
        public TestStatelessActorOneHealthChecksMvc(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestStatelessActorOneHealthChecksMvc(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
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

    public class TestStatefulActorOneHealthChecksMvc : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestStatefulActorOneHealthChecksMvc(IActorConfiguration actorConfiguration, IAggregateCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
        }

        public TestStatefulActorOneHealthChecksMvc(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, snapshotStore, snapshotStrategy, loggerFactory)
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

    public class TestStartupHealthChecks
    {
        public void ConfigureServices(IServiceCollection services)
        {

            var eventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) });

            services.AddSingleton<IEventStoreBus, EventStoreBus>();

            services.AddWorld(TestHealthChecksMvcTestBed.TestBed.ClusterVNode, TestHealthChecksMvcTestBed.TestBed.ConnectionSettings)

                   .AddEventStoreStatefulActor<TestStatefulActorOneMvc, SomeDataAggregate>(ActorConfiguration.Default)
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithBus<IEventStoreBus>((actor, bus) => actor.SubscribeFromEndToAllStreams())
                    .CreateActor()

                   .AddEventStoreStatefulActor<TestBusRegistrationStatefullActor, SomeDataAggregate>(ActorConfiguration.Default)
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithBus<IEventStoreBus>((actor, bus) => actor.SubscribeFromEndToAllStreams())
                    .CreateActor()

                   .AddStatelessActor<TestStatelessActorOneMvc>(ActorConfiguration.Default)
                    .WithBus<IEventStoreBus>((actor, bus) => actor.SubscribeFromEndToAllStreams())
                    .CreateActor();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWorld();
        }
    }

    public class TestHealthChecksMvc
    {
        private TestServer _testServer;
        private IWebHost _host;

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _host.Dispose();
            await TestHealthChecksMvcTestBed.TestBed.Stop();
        }

        [OneTimeSetUp]
        public async Task SetupFixture()
        {
            await TestHealthChecksMvcTestBed.TestBed.Start();

            var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            logging.AddDebug();
                        })
                        .UseStartup<TestStartupHealthChecks>();

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
        public async Task ShouldAddBusAndEvaluateHealthChecks()
        {

            var testWorkingBus = new TestWorkingBus();
            var testFailingBus = new TestFailingBus();

            var testStatefulActorOneMvc = _host.Services.GetService<TestStatefulActorOneMvc>();

            await Task.Delay(200);

            await testStatefulActorOneMvc.ConnectTo(testWorkingBus);

            var healthCheck = await testStatefulActorOneMvc.CheckHealthAsync(null);

            Assert.AreEqual(HealthStatus.Healthy, healthCheck.Status);

            //Assert.ThrowsAsync<BusUnhealthyException>(async () => await testStatefulActorOneMvc.ConnectTo(testFailingBus));

            testFailingBus.IsFailing = false;

            await testStatefulActorOneMvc.ConnectTo(testFailingBus);

            healthCheck = await testStatefulActorOneMvc.CheckHealthAsync(null);

            Assert.AreEqual(HealthStatus.Healthy, healthCheck.Status);

        }

    }
}
