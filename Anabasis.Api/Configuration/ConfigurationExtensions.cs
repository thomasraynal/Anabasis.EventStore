using Anabasis.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.Api
{
 
    public static class ConfigurationExtensions
    {
     
        public static IServiceCollection ConfigureAndValidate<TConfiguration>(this IServiceCollection services, IConfigurationRoot configurationRoot)
            where TConfiguration : class, ICanValidate
        {
            return services.Configure<TConfiguration>(options =>
            {
                configurationRoot.GetSection(typeof(TConfiguration).Name).Bind(options);

                options.Validate();

            });
        }

    }

}
