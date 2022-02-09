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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;
using Anabasis.Common.Actor;
using Anabasis.EventStore.Tests.Mvc;

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

        public void Dispose()
        {
        }

        public Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"{nameof(TestWorkingBus)}"));
        }

        public Task Initialize()
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

        public void Dispose()
        {
        }

        public Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            var data = new Dictionary<string, object>()
            {
                { "ConnectivityIssue", "boom!" }
            };

            if (shouldThrowIfUnhealthy)
                throw new InvalidOperationException("not healthy");

            if (IsFailing)
                return Task.FromResult(HealthCheckResult.Unhealthy($"{nameof(TestFailingBus)}", data: data));

            return Task.FromResult(HealthCheckResult.Healthy($"{nameof(TestFailingBus)}"));
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }
    }

    public class TestStatelessActorOneHealthChecksMvc : BaseEventStoreStatelessActor
    {
        public TestStatelessActorOneHealthChecksMvc(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
        }

        public TestStatelessActorOneHealthChecksMvc(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
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
        public TestStatefulActorOneHealthChecksMvc(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestStatefulActorOneHealthChecksMvc(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
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

            services.AddWorld(TestHealthChecksMvcTestBed.TestBed.ClusterVNode, TestHealthChecksMvcTestBed.TestBed.ConnectionSettings)

                    .AddEventStoreStatefulActor<TestStatefulActorOneMvc, SomeDataAggregate>(ActorConfiguration.Default)
                    .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllStreams()
                    .CreateActor()

                   .AddEventStoreStatefulActor<TestBusRegistrationStatefullActor, SomeDataAggregate>(ActorConfiguration.Default)
                   .WithReadAllFromStartCache(
                            catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true,
                            eventTypeProvider: eventTypeProvider)
                    .WithSubscribeFromEndToAllStreams()
                    .CreateActor()

                   .AddEventStoreStatelessActor<TestStatelessActorOneMvc>(ActorConfiguration.Default)
                    .WithSubscribeFromEndToAllStreams()
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

            Assert.ThrowsAsync<BusUnhealthyException>(async () => await testStatefulActorOneMvc.ConnectTo(testFailingBus));

            testFailingBus.IsFailing = false;

            await testStatefulActorOneMvc.ConnectTo(testFailingBus);

            healthCheck = await testStatefulActorOneMvc.CheckHealthAsync(null);

            Assert.AreEqual(HealthStatus.Healthy, healthCheck.Status);

        }

    }
}
