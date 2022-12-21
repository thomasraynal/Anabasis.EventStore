using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Anabasis.ProtoActor.AspNet;
using Anabasis.ProtoActor.System;
using System;
using NUnit.Framework;

namespace Anabasis.ProtoActor.Tests.AspNet
{
    public class TestNetCoreActorPoolMvcStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestEventInterceptor>();

            services.AddProtoActorSystem()
                    .WithMessageBufferHandlerActorConfiguration(10)
                    .WithDefaultSupervisionStrategies()
                    .WithDefaultDispatchQueueConfiguration()
                    .WithMessageHandlerActorConfiguration()
                    .AddRoundRobinPool<TestActor, TestActorConfiguration>(new TestActorConfiguration(TimeSpan.FromMilliseconds(100)), poolSize: 5);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseProtoActorSystem();
        }
    }


    public class ProtoActorAspNetActorPoolTests
    {
        private TestServer? _testServer;
        private IWebHost? _host;

        [OneTimeTearDown]
        public void TearDown()
        {
            _host?.Dispose();
            _testServer?.Dispose();
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
                        .UseStartup<TestNetCoreActorPoolMvcStartup>();

            _testServer = new TestServer(builder);
            _host = _testServer.Host;

        }

        [Test, Order(0)]
        public async Task ShouldCreateARoundRobinPoolAndSubscribeToABus()
        {
            var protoActoSystem = _host.Services.GetService<IProtoActorSystem>();
            var testEventInterceptor = _host.Services.GetService<TestEventInterceptor>();

            Assert.NotNull(testEventInterceptor);
            Assert.NotNull(protoActoSystem);

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(busOne);

            protoActoSystem.SubscribeToBusOne();

            for (var i = 0; i < 5; i++)
            {
                busOne.Emit(new BusOneMessage(new BusOneEvent(i)));
            }

            await Task.Delay(1000);

            Assert.AreEqual(5, testEventInterceptor.ConcurrentDictionary.Count);

            var eventCount = 0;

            foreach (var keyValue in testEventInterceptor.ConcurrentDictionary)
            {
                eventCount += keyValue.Value;
            }

            Assert.AreEqual(5, testEventInterceptor.ConcurrentDictionary.Count);

        }

    }
}
