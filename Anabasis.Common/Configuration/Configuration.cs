using Anabasis.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Anabasis.Common.Configuration
{
    public class AnabasisConfiguration
    {
        public AnabasisConfiguration(IConfigurationRoot configurationRoot, AnabasisEnvironment anabasisEnvironment, AppConfigurationOptions appConfigurationOptions, GroupConfigurationOptions groupConfigurationOptions)
        {
            ConfigurationRoot = configurationRoot;
            AnabasisEnvironment = anabasisEnvironment;
            AppConfigurationOptions = appConfigurationOptions;
            GroupConfigurationOptions = groupConfigurationOptions;
        }

        public IConfigurationRoot ConfigurationRoot { get; }
        public AnabasisEnvironment AnabasisEnvironment { get; }
        public AppConfigurationOptions AppConfigurationOptions { get; }
        public GroupConfigurationOptions GroupConfigurationOptions { get; }
    }

    public static class Configuration
    {
        private const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

        private const string BaseAppConfigurationFile = "config.app.json";
        private const string BaseGroupConfigurationFileTemplate = "config.group.json";

        private const string AppConfigurationFileTemplate = "config.app.{0}.json";
        private const string GroupConfigurationFileTemplate = "config.group.{0}.json";

        private static string GetAppConfigurationOverrideFile(AnabasisEnvironment anabasisEnvironment)
        {
            return string.Format(AppConfigurationFileTemplate, anabasisEnvironment);
        }

        private static string GetGroupConfigurationOverrideFile(AnabasisEnvironment anabasisEnvironment)
        {
            return string.Format(GroupConfigurationFileTemplate, anabasisEnvironment);
        }

        public static AnabasisConfiguration GetConfigurations(Action<ConfigurationBuilder> configureConfigurationBuilder = null, DirectoryInfo rootDirectory =null)
        {
            rootDirectory ??= new DirectoryInfo(Directory.GetCurrentDirectory());

            var environment = Environment.GetEnvironmentVariable(AspNetCoreEnvironment);

            var anabasisEnvironment = AnabasisEnvironment.Development;

            if (null != environment && Enum.TryParse<AnabasisEnvironment>(environment, out var result))
            {
                anabasisEnvironment = result;
            }

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddEnvironmentVariables();

            configurationBuilder.AddJsonFile(Path.Combine(rootDirectory.FullName,BaseAppConfigurationFile), false, false);
            configurationBuilder.AddJsonFile(Path.Combine(rootDirectory.FullName, GetAppConfigurationOverrideFile(anabasisEnvironment)), true, false);

            configurationBuilder.AddJsonFile(Path.Combine(rootDirectory.FullName, BaseGroupConfigurationFileTemplate), true, false);
            configurationBuilder.AddJsonFile(Path.Combine(rootDirectory.FullName, GetGroupConfigurationOverrideFile(anabasisEnvironment)), true, false);

            configureConfigurationBuilder?.Invoke(configurationBuilder);

            var configurationRoot = configurationBuilder.Build();

            var appConfigurationOptions = new AppConfigurationOptions();
            configurationRoot.GetSection(nameof(AppConfigurationOptions)).Bind(appConfigurationOptions);

            appConfigurationOptions.Validate();

            var groupConfigurationOptions = new GroupConfigurationOptions();
            configurationRoot.GetSection(nameof(GroupConfigurationOptions)).Bind(groupConfigurationOptions);

            groupConfigurationOptions.Validate();

            var anabasisConfiguration = new AnabasisConfiguration(
                configurationRoot,
                anabasisEnvironment,
                appConfigurationOptions,
                groupConfigurationOptions);

            return anabasisConfiguration;
        }
    }
}
