using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class WebAppBuilder
    {
        public static IWebHostBuilder Create(AppContext appContext,
            Action<JsonOptions> configureJson = null,
            Action<KestrelServerOptions> configureKestrel = null,
            Action<IApplicationBuilder> configureApplicationBuilder = null)
        {

            if (null == configureKestrel)
                configureKestrel = kestrelServerOptions => { kestrelServerOptions.AllowSynchronousIO = true; };

            var webHostBuilder = WebHost.CreateDefaultBuilder()
                                        .UseKestrel(configureKestrel);

            webHostBuilder = webHostBuilder
                .UseUrls("http://+:" + appContext.ApiPort)
                .UseEnvironment(appContext.Environment.ToString())
                .UseSetting(WebHostDefaults.ApplicationKey, appContext.ApplicationName)
                .UseSetting(WebHostDefaults.StartupAssemblyKey, Assembly.GetExecutingAssembly().GetName().Name)
                .ConfigureServices((context, services) =>
                {
                    foreach (var service in appContext.ServiceCollection)
                    {
                        services.Add(service);
                    }

                    ConfigureServices(context, services, appContext);
                    configureServices?.Invoke(services, appContext, exceptionsMapper);

                    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                })
                .Configure(appBuilder =>
                {

                    configureApplicationBuilder?.Invoke(appBuilder);
                    Configure(appBuilder, app, executingAssembly, withSwagger, disableSharedSecretKeyAuthentication);
                })
                ;


            return webHostBuilder;
        }

        static void ConfigureServices(WebHostBuilderContext webContext, IServiceCollection services, AppContext appContext)
        {
            services.AddSingleton<IDataService, DataService>();

            const long MBytes = 1024L * 1024L;

            var healthChecksBuilder = services.AddHealthChecks()
                                              .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * MBytes, "Working Set", HealthStatus.Degraded)
                                              .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * 3L * MBytes, "Too much Working Set", HealthStatus.Unhealthy);
            services
                .AddMvc(options =>
                {

                    //options.Filters.Add<BeezUPResponseSuccessfulActionFilterAttribute>();
                    //options.Filters.Add<RequiredParametersFilterAttribute>();
                    //options.Filters.Add<ValidateModelAttribute>();
                    //options.Filters.Add<BeezUPExceptionActionFilterAttribute>();

                    options.RespectBrowserAcceptHeader = true;
                })
                .AddNewtonsoftJson(options =>
                {

                    var settings = options.SerializerSettings;

                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    settings.Formatting = Formatting.None;
                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;

                    settings.Converters.Add(new UriJsonConverter());
                    settings.Converters.Add(new StringEnumConverter());
                    settings.Converters.Add(new ExpandoObjectConverter());

                    settings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;

                });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var error = actionContext.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .Select(e => new ValidationProblemDetails(actionContext.ModelState))
                        .FirstOrDefault();

                    var actionName = actionContext.ActionDescriptor.GetActionName();

                    var messages = actionContext.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(e => e.Value.Errors)
                        .Select(e => new UserErrorMessage("BadRequest", e.ErrorMessage, docUrl: appContext.DocUrl))
                        .ToArray();

                    var response = new ErrorResponseMessage(messages);

                    return new ErrorResponseMessageActionResult(response, (int)HttpStatusCode.BadRequest);

                };
            });

            services.AddResponseCompression(options =>
                    {
                        options.Providers.Add<GzipCompressionProvider>();
                    });

            services.AddRouting();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc($"v{appContext.ApiVersion.Major}", new OpenApiInfo { Title = "dev", Version = $"v{appContext.ApiVersion.Major}" });
            });

        }

        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware?tabs=aspnetcore2x
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        internal static void Configure(IApplicationBuilder app, BeezUPApp beezUPApp, Assembly executingAssembly, bool withSwagger, bool disableSharedSecretKeyAuthentication)
        {
            if (withSwagger)
            {
                app.UseBeezUPSwagger(executingAssembly, BeezUPAppWebApiDefaults.API_VERSION, beezUPApp.Context.ApplicationName.FullName);
            }

            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            beezUPApp.AddLogger(new BeezUPLoggerToMSLoggerAdapter(beezUPApp.Context, loggerFactory));

            app

                .WithBeezUPMiddleware()
                .WithVersionNumber(BeezUPAppWebApiDefaults.API_VERSION)
                .UseMvc(routeBuilder =>
                {
                    routeBuilder.Routes.Add(new CustomDirectRouter(routeBuilder.DefaultHandler));
                });
        }

    }
}
