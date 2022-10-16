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
                            serviceCollection.AddHostedService<HostedService>();
                        })
                        .Build()
                        .Run();
            }
        }
    }
}
