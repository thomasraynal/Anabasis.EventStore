using Anabasis.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Anabasis.Deployment
{
    public class AppDescriptor
    {
        public AppDescriptor(DirectoryInfo appSourceDirectory, string appName, string appRelease)
        {
            AppSourceDirectory = appSourceDirectory;
            AppName = appName;
            AppRelease = appRelease;

            var (appConfigurationOptions, groupConfigurationOptions) = GetConfigurations(appSourceDirectory);

            AppConfiguration = appConfigurationOptions;
            GroupConfiguration = groupConfigurationOptions;

            AppGroup = GroupConfiguration.GroupName;

            AppLongName = SanitizeForKubernetesConfig($"{AppGroup}-{AppName}");
            AppShortName = SanitizeForKubernetesConfig(appName);
        }

        public DirectoryInfo AppSourceDirectory { get; }
        public DirectoryInfo AppSourceKustomizeDirectory => new(Path.Combine(AppSourceDirectory.FullName, BuildConst.KustomizeFolderName));
        public DirectoryInfo AppSourceKustomizeBaseDirectory => new(Path.Combine(AppSourceKustomizeDirectory.FullName, BuildConst.Base));

        public AppConfigurationOptions AppConfiguration { get; private set; }
        public GroupConfigurationOptions GroupConfiguration { get; private set; }

        public string AppName { get; }
        public string AppRelease { get; }
        public string AppGroup { get; }
        public string AppLongName { get; }
        public string AppShortName { get; }

        private string SanitizeForKubernetesConfig(string str)
        {
            return str.Replace(".", "-").ToLower();
        }

        private (AppConfigurationOptions appConfigurationOptions, GroupConfigurationOptions groupConfigurationOptions) GetConfigurations(DirectoryInfo appSourceDirectory)
        {
            var configGroupFile = appSourceDirectory.EnumerateFiles(BuildConst.GroupConfigurationFileName, SearchOption.AllDirectories).FirstOrDefault();
            var configAppFile = appSourceDirectory.EnumerateFiles(BuildConst.AppConfigurationFileName, SearchOption.AllDirectories).FirstOrDefault();

            if (null == configGroupFile)
                throw new InvalidOperationException($"Unable to find a {BuildConst.GroupConfigurationFileName} file in the project {appSourceDirectory.FullName} and its subdirectories");

            if (null == configAppFile)
                throw new InvalidOperationException($"Unable to find a {BuildConst.AppConfigurationFileName} file in the project {appSourceDirectory.FullName} and its subdirectories");

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddJsonFile(configGroupFile.FullName, false, false);
            configurationBuilder.AddJsonFile(configAppFile.FullName, false, false);

            var configurationRoot = configurationBuilder.Build();

            var appConfigurationOptions = new AppConfigurationOptions();
            configurationRoot.GetSection(nameof(AppConfigurationOptions)).Bind(appConfigurationOptions);

            var groupConfigurationOptions = new GroupConfigurationOptions();
            configurationRoot.GetSection(nameof(GroupConfigurationOptions)).Bind(groupConfigurationOptions);

            appConfigurationOptions.Validate();
            groupConfigurationOptions.Validate();

            return (appConfigurationOptions, groupConfigurationOptions);

        }

    }
}
