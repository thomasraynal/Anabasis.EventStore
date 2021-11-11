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
using Serilog;
using Serilog.Events;
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
            Action<KestrelServerOptions> configureKestrel = null,
            Action<IApplicationBuilder> configureApplicationBuilder = null,
            Action<IServiceCollection> configureServiceCollection = null)
        {

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
               .Enrich.FromLogContext()
               .WriteTo.Console()
               .WriteTo.Sentry(
                    dsn: "https://e538c4939ce44d75988960f7a082a5bd@o1067128.ingest.sentry.io/6060457",
                    sampleRate: 1f,
                    debug: true)
               .CreateLogger();


            if (null == configureKestrel)
                configureKestrel = kestrelServerOptions => { kestrelServerOptions.AllowSynchronousIO = true; };

            var webHostBuilder = WebHost.CreateDefaultBuilder()
                                        .UseKestrel(configureKestrel);

            webHostBuilder = webHostBuilder
                .UseUrls("http://+:" + appContext.ApiPort)
                .UseEnvironment(appContext.Environment.ToString())
                .UseSetting(WebHostDefaults.ApplicationKey, appContext.ApplicationName)
                .UseSetting(WebHostDefaults.StartupAssemblyKey, Assembly.GetExecutingAssembly().GetName().Name)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {

                    ConfigureServices(services, appContext, configureJson);

                    configureServiceCollection?.Invoke(services);

                })
                .Configure(appBuilder =>
                {
                    appBuilder.WithClientIPAddress();
                    appBuilder.WithRequestContextHeaders();
                    appBuilder.WithApiVersion(appContext.ApiVersion.Major);
                    appBuilder.UseResponseCompression();
                    appBuilder.UseResponseCaching();

                    appBuilder.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });

                    appBuilder.UseRouting();

                    appBuilder.UseSerilogRequestLogging();

                    appBuilder.UseSwagger();

                    appBuilder.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/v{appContext.ApiVersion.Major}/swagger.json",
                        $"{appContext.Environment} v{appContext.ApiVersion.Major}"));

                    appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());

                    configureApplicationBuilder?.Invoke(appBuilder);


                });

            return webHostBuilder;
        }

        private static void ConfigureServices(
            IServiceCollection services, 
            AppContext appContext, 
            Action<MvcNewtonsoftJsonOptions> configureJson = null)
        {

            const long MBytes = 1024L * 1024L;

            services.AddHealthChecks()
                    .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * MBytes, "Working Set Degraded", HealthStatus.Degraded)
                    .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * 3L * MBytes, "Working Set Unhealthy", HealthStatus.Unhealthy);

            services.AddResponseCaching((options)=>
            {
                options.SizeLimit = 10 * MBytes;
                options.MaximumBodySize = 5 * MBytes;
            });

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

                    var jsonSerializerSettings = options.SerializerSettings;
                    var defaultJsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();

                    jsonSerializerSettings.ReferenceLoopHandling = defaultJsonSerializerSettings.ReferenceLoopHandling;
                    jsonSerializerSettings.NullValueHandling = defaultJsonSerializerSettings.NullValueHandling;
                    jsonSerializerSettings.DateTimeZoneHandling = defaultJsonSerializerSettings.DateTimeZoneHandling;
                    jsonSerializerSettings.Formatting = defaultJsonSerializerSettings.Formatting;
                    jsonSerializerSettings.DateFormatHandling = defaultJsonSerializerSettings.DateFormatHandling;

                    jsonSerializerSettings.Converters = defaultJsonSerializerSettings.Converters;

                    jsonSerializerSettings.StringEscapeHandling = defaultJsonSerializerSettings.StringEscapeHandling;

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

            services.AddSwaggerGen(swaggerGenOptions =>
            {
                swaggerGenOptions.DocInclusionPredicate((version, apiDesc) => !string.IsNullOrEmpty(apiDesc.HttpMethod));

                swaggerGenOptions.MapType<Guid>(() => new OpenApiSchema { Type = "string", Format = "Guid" });
                swaggerGenOptions.CustomSchemaIds(type => type.Name);
                swaggerGenOptions.IgnoreObsoleteProperties();
                swaggerGenOptions.UseInlineDefinitionsForEnums();

                swaggerGenOptions.SwaggerDoc($"v{appContext.ApiVersion.Major}",
                    new OpenApiInfo { Title = appContext.ApplicationName, Version = $"v{appContext.ApiVersion.Major}" });
            });

        }
    }
}
