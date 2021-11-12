using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

         //  var appContext = new AppContext(appName, environment, version, sentryDsn,new Uri(docUrl));

            WebAppBuilder.Create(
                        version,
                        sentryDsn,
                        docUrl: docUrl,
                        configureServiceCollection:(services)=>
                            {
                                services.AddSingleton<IDataService, RessourceService>();
                            })
                         .Build()
                         .Run();
        }
    }

    //    public static IHostBuilder CreateHostBuilder(string[] args) =>
    //        Host.CreateDefaultBuilder(args)
    //            .ConfigureWebHostDefaults(webBuilder =>
    //            {
    //                webBuilder.UseStartup<Startup>();
    //            });
    //}


}
