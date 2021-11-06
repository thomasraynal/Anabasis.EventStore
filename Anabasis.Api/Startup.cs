using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq;
using System.Net;

namespace Anabasis.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, AppContext appContext)
        {
            Configuration = configuration;
            AppContext = appContext;
        }

        public IConfiguration Configuration { get; }
        public AppContext AppContext { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDataService, DataService>();

            const long MBytes = 1024L * 1024L;

            var healthChecksBuilder = services.AddHealthChecks()
                                            .AddWorkingSetHealthCheck(AppContext.MemoryCheckTresholdInMB * MBytes, "Working Set", HealthStatus.Degraded)
                                            .AddWorkingSetHealthCheck(AppContext.MemoryCheckTresholdInMB * 3L * MBytes, "Too much Working Set", HealthStatus.Unhealthy);

            services.AddControllers()
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
                        .Select(e => new UserErrorMessage("BadRequest", e.ErrorMessage, docUrl: AppContext.DocUrl))
                        .ToArray()
                        ;

                    var response = new ErrorResponseMessage(
                        messages
                        );

                    return new ErrorResponseMessageActionResult(response, (int)HttpStatusCode.BadRequest);
                };
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "dev", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "dev v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
