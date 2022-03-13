using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anabasis.Api
{
    public static class ServiceCollectionExtensions
    {
        public static TConfiguration WithConfiguration<TConfiguration>(this IServiceCollection serviceCollection, IConfigurationRoot configurationRoot)
            where TConfiguration : class
        {

            var configuration = configurationRoot.GetSection(typeof(TConfiguration).Name).Get<TConfiguration>();

            serviceCollection.AddSingleton(configuration);

            return configuration;
        }
    }
}
