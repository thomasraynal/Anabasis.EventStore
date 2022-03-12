using Anabasis.Api.ErrorManagement;
using Anabasis.Api.Filters;
using Anabasis.Api.Middleware;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.Common.Utilities;
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
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
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
            int? memoryCheckTresholdInMB = 200,
            bool useCors = false,
            ISerializer serializer = null,
            Action<MvcOptions> configureMvcBuilder = null,
            Action<IMvcBuilder> configureMvc = null,
            Action<MvcNewtonsoftJsonOptions> configureJson = null,
            Action<KestrelServerOptions> configureKestrel = null,
            Action<IApplicationBuilder> configureApplicationBuilder = null,
            Action<IServiceCollection, IConfigurationRoot> configureServiceCollection = null,
            Action<ConfigurationBuilder> configureConfigurationBuilder = null,
            Action<LoggerConfiguration> configureLogging = null)
        {

            var anabasisConfiguration = Configuration.GetConfigurations(configureConfigurationBuilder);

            var appContext = new AnabasisAppContext(
                anabasisConfiguration.AppConfigurationOptions.ApplicationName,
                anabasisConfiguration.GroupConfigurationOptions.GroupName,
                anabasisConfiguration.AppConfigurationOptions.ApiVersion,
                anabasisConfiguration.AppConfigurationOptions.SentryDsn,
                anabasisConfiguration.AnabasisEnvironment,
                anabasisConfiguration.AppConfigurationOptions.DocUrl,
                apiPort,
                memoryCheckTresholdInMB.Value,
                Environment.MachineName);

            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration
               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
               .Enrich.FromLogContext()
               .WriteTo.Console()
               .WriteTo.Sentry(
                    dsn: appContext.SentryDsn,
                    sampleRate: 1f,
                    debug: false);

            configureLogging?.Invoke(loggerConfiguration);

            Log.Logger = loggerConfiguration.CreateLogger();

            if (null == configureKestrel)
                configureKestrel = kestrelServerOptions => { kestrelServerOptions.AllowSynchronousIO = true; };

            var webHostBuilder = WebHost.CreateDefaultBuilder()
                                        .UseKestrel(configureKestrel);

            webHostBuilder = webHostBuilder
                .UseUrls("http://+:" + appContext.ApiPort)
                .UseEnvironment($"{appContext.Environment}")
                .UseSetting(WebHostDefaults.ApplicationKey, appContext.ApplicationName)
                .UseSetting(WebHostDefaults.StartupAssemblyKey, Assembly.GetExecutingAssembly().GetName().Name)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {

                    ConfigureServices<THost>(services, useCors, appContext, serializer, configureMvcBuilder, configureMvc, configureJson);

                    configureServiceCollection?.Invoke(services, anabasisConfiguration.ConfigurationRoot);

                })
                .Configure((context, appBuilder) =>
                {

                    ConfigureApplication(appBuilder, context.HostingEnvironment, appContext, useCors);

                    configureApplicationBuilder?.Invoke(appBuilder);

                });

            return webHostBuilder;
        }

        private static void ConfigureServices<THost>(
            IServiceCollection services,
            bool useCors,
            AnabasisAppContext appContext,
            ISerializer serializer = null,
            Action<MvcOptions> configureMvcBuilder = null,
            Action<IMvcBuilder> configureMvc = null,
            Action<MvcNewtonsoftJsonOptions> configureJson = null)
        {

            services.AddOptions();

            const long MBytes = 1024L * 1024L;

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
            bool useCors)
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

            appBuilder.UseSerilogRequestLogging();

            appBuilder.UseSwagger();


            if (webHostEnvironment.IsDevelopment())
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
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    },
                    ResponseWriter = async (httpContext, healthReport) =>
                    {
                        var combinedHealthReport = healthReport;

                        var dynamicHealthCheckProvider = httpContext.RequestServices.GetService<IDynamicHealthCheckProvider>();

                        if (null != dynamicHealthCheckProvider)
                        {
                            var dynamicHealthCheckHealthReport = await dynamicHealthCheckProvider.CheckHealth(CancellationToken.None);

                            combinedHealthReport = healthReport.Combine(dynamicHealthCheckHealthReport);
                        }

                        httpContext.Response.ContentType = "application/json; charset=utf-8";

                        await httpContext.Response.WriteAsync(combinedHealthReport.ToJson());
                    }

                });
            });

        }
    }
}

