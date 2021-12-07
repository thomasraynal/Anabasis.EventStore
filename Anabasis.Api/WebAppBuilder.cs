using Anabasis.Api.ErrorManagement;
using Anabasis.Api.Filters;
using Anabasis.Api.Middleware;
using Anabasis.Common;
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
using System.Linq;
using System.Net;
using System.Reflection;

namespace Anabasis.Api
{
    public static class WebAppBuilder
    {

        public static IWebHostBuilder Create<THost>(
            int apiPort = 80,
            int? memoryCheckTresholdInMB = 200,
            Action<MvcOptions> configureMvcBuilder = null,
            Action<IMvcBuilder> configureMvc = null,
            Action<MvcNewtonsoftJsonOptions> configureJson = null,
            Action<KestrelServerOptions> configureKestrel = null,
            Action<IApplicationBuilder> configureApplicationBuilder = null,
            Action<IServiceCollection, IConfigurationRoot> configureServiceCollection = null,
            Action<ConfigurationBuilder> configureConfigurationBuilder = null,
            Action<LoggerConfiguration> configureLogging = null)
        {

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddEnvironmentVariables();

            configurationBuilder.AddJsonFile(AnabasisAppContext.AppConfigurationFile, false, false);
            configurationBuilder.AddJsonFile(AnabasisAppContext.GroupConfigurationFile, true, false);

            configureConfigurationBuilder?.Invoke(configurationBuilder);

            var configurationRoot = configurationBuilder.Build();

            var appConfigurationOptions = new AppConfigurationOptions();
            configurationRoot.GetSection(nameof(AppConfigurationOptions)).Bind(appConfigurationOptions);

            appConfigurationOptions.Validate();

            var groupConfigurationOptions = new GroupConfigurationOptions();
            configurationRoot.GetSection(nameof(GroupConfigurationOptions)).Bind(groupConfigurationOptions);

            groupConfigurationOptions.Validate();

            var appContext = new AnabasisAppContext(
                appConfigurationOptions.ApplicationName,
                groupConfigurationOptions.GroupName,
                appConfigurationOptions.ApiVersion,
                appConfigurationOptions.SentryDsn,
                groupConfigurationOptions.Environment,
                appConfigurationOptions.DocUrl,
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
                    debug: true);

            configureLogging?.Invoke(loggerConfiguration);

            Log.Logger = loggerConfiguration.CreateLogger();

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

                    ConfigureServices<THost>(services, appContext, configureMvcBuilder, configureMvc, configureJson);

                    configureServiceCollection?.Invoke(services, configurationRoot);

                })
                .Configure((context,appBuilder) =>
                {

                    ConfigureApplication(appBuilder, context.HostingEnvironment, appContext);

                    configureApplicationBuilder?.Invoke(appBuilder);

                });

            return webHostBuilder;
        }

        private static void ConfigureServices<THost>(
            IServiceCollection services,
            AnabasisAppContext appContext,
            Action<MvcOptions> configureMvcBuilder = null,
            Action<IMvcBuilder> configureMvc = null,
            Action<MvcNewtonsoftJsonOptions> configureJson = null)
        {

            services.AddOptions();

            const long MBytes = 1024L * 1024L;

            services.AddHealthChecks()
                    .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * MBytes, "Degraded working set", HealthStatus.Degraded)
                    .AddWorkingSetHealthCheck(appContext.MemoryCheckTresholdInMB * 3L * MBytes, "Unhealthy working set", HealthStatus.Unhealthy);

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

#if DEBUG
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
#endif

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
            AnabasisAppContext appContext)
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

#if DEBUG
            appBuilder.UseCors("cors");
#endif

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
                    }
                });
            });

        }
    }
}

