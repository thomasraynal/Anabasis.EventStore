using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anabasis.Api
{
    public static class ServiceCollectionExtensions
    {
        public static TConfiguration WithConfiguration<TConfiguration>(this IServiceCollection serviceCollection, IConfigurationRoot configurationRoot, string sectionName = null)
            where TConfiguration : class
        {
            var camelCaseConfigurationName = sectionName ?? typeof(TConfiguration).Name;

            var configuration = configurationRoot.GetSection(camelCaseConfigurationName).Get<TConfiguration>();

            serviceCollection.AddSingleton(configuration);

            return configuration;
        }
    }
}
