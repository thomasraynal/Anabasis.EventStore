using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class TestBaseStatelessActor : BaseStatelessActor
    {
        public TestBaseStatelessActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestBaseStatelessActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();

        public Task Handle(SomeDataAggregatedEvent someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }

    }

    public class TestNetCoreMvcStatelessStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDummyBus, DummyBus>();

            services.AddWorld()
                    .AddStatelessActor<TestBaseStatelessActor>(ActorConfiguration.Default)
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

    public class TestNetCoreMvcStateless
    {
        private TestServer _testServer;
        private IWebHost _host;

        [OneTimeTearDown]
        public void TearDown()
        {
            _testServer.Dispose();
            _host.Dispose();
        }

        [OneTimeSetUp]
        public void Setup()
        {

            var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            logging.AddDebug();
                        })
                        .UseStartup<TestNetCoreMvcStatelessStartup>();

            _testServer = new TestServer(builder);
            _host = _testServer.Host;

        }

        [Test, Order(0)]
        public void ShouldCheckThatAllActorsAreCreated()
        {

            var testBaseStatelessActor = _host.Services.GetService<TestBaseStatelessActor>();

            Assert.NotNull(testBaseStatelessActor);
            Assert.True(testBaseStatelessActor.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldGenerateAnEventAndUpdateAll()
        {

            var testBaseStatelessActor = _host.Services.GetService<TestBaseStatelessActor>();

            await Task.Delay(100);

            var bus = testBaseStatelessActor.GetConnectedBus<IDummyBus>();

            bus.Push(new SomeDataAggregatedEvent("entityId", Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(1, testBaseStatelessActor.Events.Count);

            testBaseStatelessActor.Dispose();
        }

    }
}
