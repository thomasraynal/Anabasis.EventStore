using Anabasis.Api.Configuration;
using Anabasis.Api.Converters;
using Anabasis.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PostSharp.Patterns.Caching;
using PostSharp.Patterns.Caching.Backends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var version = new Version(1, 0);
            var sentryDsn = "https://3abd2cfbe4fb457e86f8f17fca6e8260@o1067128.ingest.sentry.io/6060457";
            var docUrl = new Uri("https://api-docs.beezup.com/#operation");

            WebAppBuilder.Create<Program>(
                        version,
                        sentryDsn,
                        docUrl: docUrl,
                        configureServiceCollection: (services, configurationRoot) =>
                             {
                                 CachingServices.DefaultBackend = new MemoryCachingBackend();

                             
             
                                 services.ConfigureAndValidate<SomeOtherConfigurationOptions>(configurationRoot);

                             },
                        configureApplicationBuilder: (appBuilder) =>
                        {


                        },
                        configureJson: (mvcNewtonsoftJsonOptions) =>
                            {
                            
                            })
                         .Build()
                         .Run();
        }
    }

}
