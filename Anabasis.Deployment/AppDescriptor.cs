using Anabasis.Common;
using Anabasis.Common.Configuration;
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
        public AppDescriptor(FileInfo projectFilePath, DirectoryInfo projectBuildDirectory, string appName, string appRelease)
        {
            ProjectFilePath = projectFilePath;
            AppBuildDirectory = projectBuildDirectory;
            AppSourceDirectory = projectFilePath.Directory;
            AppName = appName;
            AppRelease = appRelease;

            var anabasisConfiguration = Configuration.GetConfigurations(rootDirectory: projectBuildDirectory);

            AppConfiguration = anabasisConfiguration.AppConfigurationOptions;
            GroupConfiguration = anabasisConfiguration.GroupConfigurationOptions;

            AppGroup = SanitizeForKubernetesConfig(GroupConfiguration.GroupName);

            AppLongName = SanitizeForKubernetesConfig($"{AppGroup}-{AppName}");
            AppShortName = SanitizeForKubernetesConfig(appName);
        }

        public FileInfo ProjectFilePath { get; }
        public DirectoryInfo AppBuildDirectory { get; }
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
        public string ServiceName => $"svc-{AppLongName}";
        public string IngressPath => $"/{AppShortName}/v{AppConfiguration.ApiVersion.Major}";

        private string SanitizeForKubernetesConfig(string str)
        {
            return str.Replace(".", "-").ToLower();
        }

    }
}
