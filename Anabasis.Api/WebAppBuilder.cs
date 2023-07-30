using Anabasis.Api.ErrorManagement;
using Anabasis.Api.Filters;
using Anabasis.Api.Middleware;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.Common.Utilities;
using Anabasis.Insights;
using Honeycomb.OpenTelemetry;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Anabasis.Api
{
    public static class WebAppBuilder
    {

        public static IWebHostBuilder Create<THost>(
            int apiPort = 80,
            int memoryCheckTresholdInMB = 200,
            bool useCors = false,
            bool useAuthorization = false,
            bool useSwaggerUI =false,
            ISerializer? serializer = null,
            Action<MvcOptions>? configureMvcBuilder = null,
            Action<IMvcBuilder>? configureMvc = null,
            Action<SwaggerGenOptions>? configureSwaggerGen = null,
            Action<MvcNewtonsoftJsonOptions>? configureJson = null,
            Action<KestrelServerOptions>? configureKestrel = null,
            Action<AnabasisAppContext, IApplicationBuilder>? configureMiddlewares = null,
            Action<AnabasisAppContext, IApplicationBuilder>? configureApplicationBuilder = null,
            Action<AnabasisAppContext, IServiceCollection, IConfigurationRoot>? configureServiceCollection = null,
            Action<ConfigurationBuilder>? configureConfigurationBuilder = null,
            Action<LoggerConfiguration>? configureLogging = null,
            Action<TracerProviderBuilder>? configureTracerProviderBuilder = null)
        {

            var anabasisConfiguration = Configuration.GetAnabasisConfigurations(configureConfigurationBuilder);

            var honeycombConfiguration = anabasisConfiguration.ConfigurationRoot.GetSection("Honeycomb").Get<HoneycombOptions>();

            var anabasisAppContext = new AnabasisAppContext(
                anabasisConfiguration.AppConfigurationOptions.ApplicationName,
                anabasisConfiguration.GroupConfigurationOptions.GroupName,
                anabasisConfiguration.AppConfigurationOptions.ApiVersion,
                anabasisConfiguration.AppConfigurationOptions.SentryDsn,
                honeycombConfiguration?.ServiceName,
                honeycombConfiguration?.ApiKey,
                anabasisConfiguration.AnabasisEnvironment,
                anabasisConfiguration.AppConfigurationOptions.DocUrl,
                apiPort,
                memoryCheckTresholdInMB,
                Environment.MachineName);

            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration
               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
               .Enrich.FromLogContext()
               .WriteTo.Console();

            if (anabasisAppContext.UseSentry)
            {
                loggerConfiguration.WriteTo.Sentry(
                    dsn: anabasisAppContext.SentryDsn,
                    sampleRate: 1f,
                    debug: false);
            }

            configureLogging?.Invoke(loggerConfiguration);

            Log.Logger = loggerConfiguration.CreateLogger();

            if (null == configureKestrel)
                configureKestrel = kestrelServerOptions => { kestrelServerOptions.AllowSynchronousIO = true; };
            
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                              .UseKestrel(configureKestrel);

            webHostBuilder = webHostBuilder
                    .UseKestrel(configureKestrel)
                    .UseUrls("http://+:" + anabasisAppContext.ApiPort)
                    .UseEnvironment($"{anabasisAppContext.Environment}")
                    .UseSerilog()
                    .UseSetting(WebHostDefaults.ApplicationKey, anabasisAppContext.ApplicationName)
                    .UseSetting(WebHostDefaults.StartupAssemblyKey, Assembly.GetExecutingAssembly().GetName().Name)

                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices<THost>(services,
                            useCors,
                            anabasisAppContext,
                            honeycombConfiguration,
                            serializer,
                            configureMvcBuilder,
                            configureTracerProviderBuilder,
                            configureMvc,
                            configureJson,
                            configureSwaggerGen);

                        configureServiceCollection?.Invoke(anabasisAppContext, services, anabasisConfiguration.ConfigurationRoot);

                    })
                    .Configure((context, appBuilder) =>
                    {

                        configureMiddlewares?.Invoke(anabasisAppContext, appBuilder);

                        ConfigureApplication(appBuilder, context.HostingEnvironment, anabasisAppContext, useAuthorization, useCors, useSwaggerUI);

                        configureApplicationBuilder?.Invoke(anabasisAppContext, appBuilder);

                    });
         

            return webHostBuilder;
        }

        private static void ConfigureServices<THost>(
            IServiceCollection services,
            bool useCors,
            AnabasisAppContext appContext,
            HoneycombOptions? honeycombOptions = null,
            ISerializer? serializer = null,
            Action<MvcOptions>? configureMvcBuilder = null,
            Action<TracerProviderBuilder>? configureTracerProviderBuilder = null,
            Action<IMvcBuilder>? configureMvc = null,
            Action<MvcNewtonsoftJsonOptions>? configureJson = null,
            Action<SwaggerGenOptions>? configureSwaggerGen = null)
        {
            const long MBytes = 1024L * 1024L;

            services.AddOptions();

            if (appContext.UseHoneycomb && null != honeycombOptions)
            {
                services.AddOpenTracing(honeycombOptions, configureTracerProviderBuilder);
                services.AddTransient<ITracer, AnabasisTracer>();
            }

            services.AddHealthChecks()
                    .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * 3L * MBytes, "Working set", HealthStatus.Unhealthy);

            services.AddSingleton(serializer ?? new DefaultSerializer());

            services.AddHostedService<HealthCheckHostedService>();

            services.AddResponseCaching((options) =>
            {
                options.SizeLimit = 10 * MBytes;
                options.MaximumBodySize = 5 * MBytes;
            });

            services.AddApiVersioning(apiVersioningOptions =>
            {
                apiVersioningOptions.ErrorResponses = new ErrorResponseProvider();
                apiVersioningOptions.ReportApiVersions = true;
                apiVersioningOptions.AssumeDefaultVersionWhenUnspecified = true;
                apiVersioningOptions.ApiVersionReader = new HeaderApiVersionReader(HttpHeaderConstants.HTTP_HEADER_API_VERSION);
                apiVersioningOptions.DefaultApiVersion = new ApiVersion(appContext.ApiVersion.Major, appContext.ApiVersion.Minor);
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

                    Json.SetDefaultJsonSerializerSettings(jsonSerializerSettings);

                });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var error = actionContext.ModelState
                        .Where(errors => errors.Value.Errors.Count > 0)
                        .Select(_ => new ValidationProblemDetails(actionContext.ModelState))
                        .FirstOrDefault();

                    var actionName = actionContext.ActionDescriptor.GetActionName();

                    var docUrl = actionName == null ? null : DocUrlHelper.GetDocUrl(actionName, appContext.DocUrl);

                    var messages = actionContext.ModelState
                        .Where(errors => errors.Value.Errors.Count > 0)
                        .SelectMany(errors => errors.Value.Errors)
                        .Select(errors => new UserErrorMessage(HttpStatusCode.BadRequest, errors.ErrorMessage, docUrl: docUrl))
                        .ToArray();

                    var response = new ErrorResponseMessage(messages);

                    return new ErrorResponseMessageActionResult(response, HttpStatusCode.BadRequest);

                };
            });

            services.AddResponseCompression(options =>
                    {
                        options.Providers.Add<GzipCompressionProvider>();
                    });

            if (useCors)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(name: "cors",
                                      builder =>
                                      {
                                          builder.AllowAnyOrigin()
                                  .AllowAnyMethod()
                                  .AllowAnyHeader();
                                      });
                });
            }

            services.AddSwaggerGenNewtonsoftSupport();

            services.AddSwaggerGen(swaggerGenOptions =>
            {
                swaggerGenOptions.DocInclusionPredicate((version, apiDesc) => !string.IsNullOrEmpty(apiDesc.HttpMethod));
                swaggerGenOptions.MapType<Guid>(() => new OpenApiSchema { Type = "string", Format = "Guid" });
                swaggerGenOptions.CustomSchemaIds(type => type.Name);
                swaggerGenOptions.IgnoreObsoleteProperties();
                swaggerGenOptions.UseInlineDefinitionsForEnums();

                swaggerGenOptions.SwaggerDoc($"v{appContext.ApiVersion.Major}",
                    new OpenApiInfo { Title = appContext.ApplicationName, Version = $"v{appContext.ApiVersion.Major}" });


                configureSwaggerGen?.Invoke(swaggerGenOptions);
            });

            var mvcBuilder = services.AddMvc(mvcOptions =>
             {
                 configureMvcBuilder?.Invoke(mvcOptions);
             });

            mvcBuilder.AddApplicationPart(typeof(THost).Assembly);

            configureMvc?.Invoke(mvcBuilder);
        }

        private static void ConfigureApplication(
            IApplicationBuilder appBuilder,
            IWebHostEnvironment webHostEnvironment,
            AnabasisAppContext appContext,
            bool useAuthorization,
            bool useCors,
            bool useSwaggerUI)
        {
            appBuilder.WithClientIPAddress();
            appBuilder.WithRequestContextHeaders();

            appBuilder.UseResponseCompression();
            appBuilder.UseResponseCaching();
        

            appBuilder.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            appBuilder.UseRouting();

            if (useAuthorization)
            {
                appBuilder.UseAuthentication();
                appBuilder.UseAuthorization();
            }

            appBuilder.UseSerilogRequestLogging();

            appBuilder.UseSwagger();

            if (useSwaggerUI)
            {
                appBuilder.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/v{appContext.ApiVersion.Major}/swagger.json",
                     $"{appContext.Environment} v{appContext.ApiVersion.Major}"));
            }

            if (useCors)
            {
                appBuilder.UseCors("cors");
            }

            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResponseWriter = async (httpContext, healthReport) =>
                    {
                        var combinedHealthReport = healthReport;

                        var dynamicHealthCheckProvider = httpContext.RequestServices.GetService<IDynamicHealthCheckProvider>();

                        if (null != dynamicHealthCheckProvider)
                        {
                            var dynamicHealthCheckHealthReport = await dynamicHealthCheckProvider.CheckHealth(CancellationToken.None);

                            combinedHealthReport = healthReport.Combine(dynamicHealthCheckHealthReport);
                        }

                        httpContext.Response.StatusCode = (int)(combinedHealthReport.Status == HealthStatus.Unhealthy ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
                        httpContext.Response.ContentType = "application/json; charset=utf-8";

                        await httpContext.Response.WriteAsync(combinedHealthReport.ToJson());
                    }

                });
            });

        }
    }
}

