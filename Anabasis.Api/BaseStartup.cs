using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Anabasis.Api
{
    public abstract class BaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDataService, DataService>();

            const long MBytes = 1024L * 1024L;
            var healthChecksBuilder = services.AddHealthChecks()
                                            .AddWorkingSetHealthCheck(memoryCheckTresholdInMB * MBytes, "Working Set", HealthStatus.Degraded)
                                            .AddWorkingSetHealthCheck(memoryCheckTresholdInMB * 3L * MBytes, "Too much Working Set", HealthStatus.Unhealthy);

            services.AddControllers()
                          .AddNewtonsoftJson(options =>
                          {
                              var defaultSettings = JsonSerializationHelper.GetDefaultJsonSerializerSettings(false, Formatting.Indented, false);

                              var settings = options.SerializerSettings;
                              settings.ReferenceLoopHandling = defaultSettings.ReferenceLoopHandling;
                              settings.NullValueHandling = defaultSettings.NullValueHandling;
                              settings.DateTimeZoneHandling = defaultSettings.DateTimeZoneHandling;
                              settings.Formatting = defaultSettings.Formatting;
                              settings.DateFormatHandling = defaultSettings.DateFormatHandling;
                              settings.Converters = defaultSettings.Converters;
                              settings.ContractResolver = defaultSettings.ContractResolver;
                              settings.StringEscapeHandling = defaultSettings.StringEscapeHandling;

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
