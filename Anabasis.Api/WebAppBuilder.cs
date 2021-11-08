using Anabasis.Api.Filters;
using Anabasis.Api.Middleware;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Anabasis.Api
{
    public static class WebAppBuilder
    {
        public static IWebHostBuilder Create(AppContext appContext,
            Action<MvcNewtonsoftJsonOptions> configureJson = null,
            Action<KestrelServerOptions> configureKestrel = null)
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

                    ConfigureServices(services, appContext, configureJson);

                })
                .Configure(appBuilder =>
                {
                    appBuilder.WithClientIPAddress();
                    appBuilder.WithRequestContextHeaders();
                    appBuilder.WithVersionNumber(appContext.ApiVersion.Major);

                    appBuilder.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });

                    appBuilder.UseRouting();

                    appBuilder.UseSwagger();
                    appBuilder.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/v{appContext.ApiVersion.Major}/swagger.json",
                        $"{appContext.Environment} v{appContext.ApiVersion.Major}"));

                    appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());


                });
                

            return webHostBuilder;
        }

        private static void ConfigureServices(
            IServiceCollection services, 
            AppContext appContext, 
            Action<MvcNewtonsoftJsonOptions> configureJson = null)
        {

            const long MBytes = 1024L * 1024L;

            var healthChecksBuilder = services.AddHealthChecks()
                                              .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * MBytes, "Working Set Degraded", HealthStatus.Degraded)
                                              .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * 3L * MBytes, "Working Set Unhealthy", HealthStatus.Unhealthy);
            services
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton(appContext)
                .AddControllers(options =>
                {
                    options.Filters.Add<RequiredParametersActionFilterAttribute>();
                    options.Filters.Add<ModelValidationActionFilterAttribute>();
                    options.Filters.Add<ExceptionActionFilterAttribute>();
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

                    configureJson?.Invoke(options);

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
                c.SwaggerDoc($"v{appContext.ApiVersion.Major}",
                    new OpenApiInfo { Title = appContext.Environment, Version = $"v{appContext.ApiVersion.Major}" });
            });

        }
    }
}
