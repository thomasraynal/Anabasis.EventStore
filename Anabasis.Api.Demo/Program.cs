using Anabasis.Common.Configuration;
using Anabasis.EventStore.AspNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.Api.Demo
{
    class Program
    {
        static void Main(string[] _)
        {
            {
                WebAppBuilder.Create<Program>(
                        configureServiceCollection: (anabasisContext, serviceCollection, configurationRoot) =>
                        {
                            var workerConfiguration = new WorkerConfiguration()
                            {
                                DispatcherCount = 2,
                            };

                            serviceCollection.AddWorld()
                                             .AddWorker<WorkerOne>(workerConfiguration)
                                             .WithBus<IBusOne>((worker, bus) =>
                                             {
                                                 bus.Subscribe((@event) =>
                                                 {
                                                     worker.Handle(new[] { @event });
                                                 });
                                             })
                                             .CreateWorker();

                            serviceCollection.AddHostedService<HostedService>();
                            serviceCollection.AddSingleton<IBusOne, BusOne>();

                        },
                        configureApplicationBuilder: (anabasisContext, app) =>
                        {
                            app.UseWorld();
                        })
                        .Build()
                        .Run();
            }
        }
    }
}
