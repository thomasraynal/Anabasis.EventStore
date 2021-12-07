using Anabasis.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection ConfigureAndValidate<TConfiguration>(this IServiceCollection services, IConfigurationRoot configurationRoot)
            where TConfiguration: class, ICanValidate
        {
            return services.Configure<TConfiguration>(options =>
            {
                configurationRoot.GetSection(typeof(TConfiguration).Name).Bind(options);

                options.Validate();

            });
        }
    }
}
