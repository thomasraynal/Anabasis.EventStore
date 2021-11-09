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
            var appName = "testApp";
            var environment = "test";

            var appContext = new AppContext(appName, environment, version);

            WebAppBuilder.Create(appContext, configureServiceCollection:(services)=>
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
