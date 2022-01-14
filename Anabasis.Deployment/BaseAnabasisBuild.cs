using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Git;
using System;
using Nuke.Common.IO;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.Kubernetes;
using k8s;
using Serilog;
using System.Collections.Generic;

namespace Anabasis.Deployment
{
    public enum AnabasisBuildEnvironment
    {
        Prod
    }

    public static class BuildConst
    {
        public const string Kustomize = "kustomize";
        public const string Base = "base";
        public const string Templates = "templates";
        public const string Overlays = "overlays";
        public static readonly DirectoryInfo BuildKustomizeTemplatesDirectory = new(Templates);
        public static readonly string Production = AnabasisBuildEnvironment.Prod.ToString().ToLower();
    }

    public class AppDescriptor
    {
        public AppDescriptor(DirectoryInfo appSourceDirectory, string app, string appRelease, string appGroup, string appLongName, string appShortName)
        {
            AppSourceDirectory = appSourceDirectory;
            App = app;
            AppRelease = appRelease;
            AppGroup = appGroup;
            AppLongName = appLongName;
            AppShortName = appShortName;
        }

        public DirectoryInfo AppSourceDirectory { get;  }

        public DirectoryInfo AppSourceKustomizeDirectory => new(Path.Combine(AppSourceDirectory.FullName, BuildConst.Kustomize));
        public DirectoryInfo AppSourceKustomizeBaseDirectory => new(Path.Combine(AppSourceKustomizeDirectory.FullName, BuildConst.Base));
        public DirectoryInfo AppSourceKustomizeOverlaysDirectory => new(Path.Combine(AppSourceKustomizeDirectory.FullName, BuildConst.Overlays));

        public string App { get; }
        public string AppRelease { get;  }
        public string AppGroup { get;  }
        public string AppLongName { get;  }
        public string AppShortName { get;  }

    }

    public abstract class BaseAnabasisBuild : NukeBuild
    {

        [GitRepository]
        private readonly GitRepository GitRepository;

        private readonly string Configuration = "Release";

        public string RuntimeDockerImage { get; set; } = "microsoft/dotnet:5.0-aspnetcore-runtime";

        [Required]
        [Parameter("Docker registry")]
        public string DockerRegistryServer;

        [Required]
        [Parameter("Docker registry user name")]
        public string DockerRegistryUserName;

        [Required]
        [Parameter("Docker registry password")]
        public string DockerRegistryPassword;

        [Required]
        [Parameter("Set the build Id.")]
        public string BuildId;

        [Required]
        [Parameter("Set the domain to be deployed")]
        public string GroupToBeDeployed;

        [Parameter("Set the applications to be deployed")]
        public string[] ApplicationsToBeDeployed;

        [Parameter("Solution source directory")]
        public AbsolutePath SourceDirectory = RootDirectory / "src";

        private AbsolutePath BuildDirectory => RootDirectory / "build";
   
        private AbsolutePath TestsDirectory => RootDirectory / "tests";
        private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
        private AbsolutePath DeploymentsDirectory => RootDirectory / "deployments";
        private string Branch => GitRepository?.Branch ?? "NO_GIT_REPOS_DETECTED";
        private AbsolutePath OneForAllDockerFile => BuildDirectory / "docker" / "build.nuke.app.dockerfile";
        public AbsolutePath BuildProjectKustomizeDirectory { get; set; }
        private AbsolutePath BuildProjectKustomizeTemplateDirectory => BuildProjectKustomizeDirectory / "templates";

        private Kubernetes _client;

        protected BaseAnabasisBuild()
        {
            //unit tests
            if(null != BuildProjectDirectory)
            {
                BuildProjectKustomizeDirectory = BuildProjectDirectory / "kustomize";
            }

        }

        public Kubernetes GetKubernetesClient()
        {
            if (null == _client)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                _client = new Kubernetes(config);
            }

            return _client;
        }

        protected Target Test => _ => _
               .DependsOn(Publish)
               .Executes(() =>
               {
                   ExecuteTests(GetTestsProjects());
               });

        protected Target Clean => _ => _
            .Executes(() =>
            {

                foreach (var directory in SourceDirectory.GlobDirectories("**/bin", "**/obj")
                             .Concat(TestsDirectory.GlobDirectories("**/bin", "**/obj")))
                    try
                    {
                        if (!FileSystemTasks.DirectoryExists(directory))
                        {
                            Log.Warning($"Not existing directory : {directory}");
                            continue;
                        }

                        FileSystemTasks.DeleteDirectory(directory);
                    }
                    catch
                    {
                    }

                FileSystemTasks.EnsureCleanDirectory(ArtifactsDirectory);
            });

        protected Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                foreach (var proj in GetAllProjects())
                {
                    DotNetTasks.DotNetRestore(dotNetRestoreSettings => dotNetRestoreSettings.SetProjectFile(proj));
                }

            });

        protected Target Publish => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                var applications = GetAllProjects();

                //PublishApplications(applications);

            });

        protected Target Package => _ => _
            .DependsOn(Test)
            .Executes(() =>
            {
                var applications = GetApplicationProjects();

                //BuildContainers(applications);

                //PushContainers(applications);
            });

        protected Target Generate => _ => _
            // .DependsOn(Package)
            .Executes(async () =>
            {




            });

        public Target GenerateKubernetesYaml => _ => _
           // .DependsOn(Package)
           .Executes(async () =>
           {

           });


        public void DeleteIfExist(DirectoryInfo directoryInfo)
        {
            if (Directory.Exists(directoryInfo.FullName))
            {
                Directory.Delete(directoryInfo.FullName, true);
            }
        }

        //https://stackoverflow.com/a/3822913
        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public void SetupKustomize(AppDescriptor appDescriptor)
        {
            //clean kustomize directories structure
            DeleteIfExist(appDescriptor.AppSourceKustomizeBaseDirectory);

            //create kustomize directories structure
            Directory.CreateDirectory(appDescriptor.AppSourceKustomizeDirectory.FullName);
            Directory.CreateDirectory(appDescriptor.AppSourceKustomizeBaseDirectory.FullName);
            Directory.CreateDirectory(appDescriptor.AppSourceKustomizeOverlaysDirectory.FullName);
  
            foreach(var env in Enum.GetValues(typeof(AnabasisBuildEnvironment)).Cast<AnabasisBuildEnvironment>())
            {
                var overlaysDirectory = Directory.CreateDirectory(Path.Combine(appDescriptor.AppSourceKustomizeOverlaysDirectory.FullName, $"{env}".ToLower()));
                var baseDirectory = Directory.CreateDirectory(Path.Combine(appDescriptor.AppSourceKustomizeOverlaysDirectory.FullName, $"{env}".ToLower()));

                Directory.CreateDirectory(baseDirectory.FullName);
                Directory.CreateDirectory(overlaysDirectory.FullName);
            }
        }

        public async Task GenerateBaseKustomize(AppDescriptor appDescriptor)
        {

            foreach (var env in Enum.GetValues(typeof(AnabasisBuildEnvironment)).Cast<AnabasisBuildEnvironment>())
            {
                await GenerateKubernetesYamlNamespace(appDescriptor, env);
                await GenerateKubernetesYamlGroupConfigMap(appDescriptor, env);
                await GenerateKubernetesYamlService(appDescriptor, env);
                await GenerateKubernetesYamlDeployment(appDescriptor, env);
                await GenerateKubernetesYamlAppConfigMap(appDescriptor, env);
            }

        }

        public Target Deploy => _ => _
            // .DependsOn(Package)
            .Executes(async () =>
            {
                var appGroup = SanitizeForKubernetesConfig(GroupToBeDeployed.ToLower());

                var appsToBeDeployed = GetAppsToBeDeployed();

                //for each projects
                    //=> delete kustomize/templates/*
                    //=> create kustomize/templates/*
                    //=> create kustomize/overlay/*
                    //=> delete
                    //=> create kustomize/base/*

                //=>generate kustomize/base/* from template

                foreach (var appToBeDeployed in appsToBeDeployed)
                {
                    await GenerateBaseKustomize(appToBeDeployed);
                }

            });


        public AppDescriptor[] GetAppsToBeDeployed()
        {
            var appDescriptors = new List<AppDescriptor>();

            var lowerCaseAppGroup = SanitizeForKubernetesConfig(GroupToBeDeployed.ToLower());

            var applicationProjectFiles = GetApplicationProjects();

            foreach(var applicationProjectFile in applicationProjectFiles)
            {
                var appName = SanitizeForKubernetesConfig(Path.GetFileName(applicationProjectFile).Replace(".csproj", ""));
                var appSourceDirectory = new FileInfo(applicationProjectFile).Directory;

                var appDescriptor = new AppDescriptor(
                    appSourceDirectory,
                    appName,
                    BuildId,
                    GroupToBeDeployed,
                    appLongName: $"{lowerCaseAppGroup}-{appName.ToLower()}",
                    appShortName: $"{appName.ToLower()}");

                appDescriptors.Add(appDescriptor);
            }

            return appDescriptors.ToArray();

        }

        protected virtual string[] GetTestsProjects()
        {
            return PathConstruction.GlobFiles(TestsDirectory, $"**/*.Tests.csproj")
                                   .OrderBy(p => p)
                                   .Select(p => p.ToString())
                                   .ToArray();

        }

        protected virtual string[] GetAllProjects()
        {
            var projects = GetApplicationProjects()
                .Concat(GetTestsProjects())
                .Concat(GetNugetPackageProjects())
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            return projects;
        }

        protected virtual string[] GetApplicationProjects(AbsolutePath directory = null)
        {
            directory ??= SourceDirectory;

            return PathConstruction.GlobFiles(directory, "**/*.App.csproj").Select(p => p.ToString()).OrderBy(p => p).ToArray();
        }

        protected virtual string[] GetNugetPackageProjects()
        {
            return PathConstruction.GlobFiles(SourceDirectory, "**/*.csproj").Select(p => p.ToString()).OrderBy(p => p).ToArray();
        }

        protected void BuildContainers(params string[] projects)
        {
            var dockerFile = ArtifactsDirectory / Path.GetFileName(OneForAllDockerFile);
            FileSystemTasks.CopyFile(OneForAllDockerFile, dockerFile, FileExistsPolicy.OverwriteIfNewer);

            foreach (var proj in projects)
            {
                var projectName = Path.GetFileNameWithoutExtension(proj);
                var publishedPath = ArtifactsDirectory / projectName;

                DockerTasks.DockerBuild(s => s
                    .SetFile(OneForAllDockerFile)
                    .AddBuildArg($"RUNTIME_IMAGE={RuntimeDockerImage}")
                    .AddBuildArg($"PROJECT_NAME={projectName}")
                    .AddBuildArg($"BUILD_ID={BuildId}")
                    .SetTag($"{GetProjectDockerImageName(proj)}:{BuildId.ToLower()}")
                    .SetPath(publishedPath)
                    .EnableForceRm());

            }
        }

        protected void ExecuteTests(string[] projects, bool nobuild = false)
        {
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.ForEach(
                projects,
                proj =>
                {
                    try
                    {
                        var projectName = Path.GetFileNameWithoutExtension(proj);
                        DotNetTasks.DotNetTest(dotNetTestSettings =>
                        {
                            dotNetTestSettings = dotNetTestSettings
                                .SetConfiguration(Configuration)
                                .SetProjectFile(proj);

                            if (nobuild)
                                dotNetTestSettings = dotNetTestSettings.EnableNoBuild();

                            return dotNetTestSettings;
                        });
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });


            if (exceptions.Count != 0)
                throw new AggregateException(exceptions);

        }

        protected void PublishApplications(params string[] projects)
        {
            foreach (var proj in projects)
            {
                var projectName = Path.GetFileNameWithoutExtension(proj);
                var outputPath = ArtifactsDirectory / projectName;

                Log.Information($"Publishing {projectName} in {outputPath}");

                DotNetTasks.DotNetRestore(s => s.SetProjectFile(proj));

                DotNetTasks.DotNetPublish(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetProject(proj)
                    .SetOutput(outputPath)
                    );

            }
        }

        protected string GetProjectDockerImageName(string project)
        {
            var prefix = Path.GetFileNameWithoutExtension(project).ToLower();
            return $"{prefix}-{GitRepository.Branch.Replace("/", "")}".ToLower();
        }

        private void PushContainers(string[] projects)
        {

            DockerTasks.DockerLogin(dockerLoginSettings => dockerLoginSettings
                .SetServer(DockerRegistryServer)
                .SetUsername(DockerRegistryUserName)
                .SetPassword(DockerRegistryPassword)
            );

            foreach (var project in projects)
            {

                var imageNameAndTag = $"{GetProjectDockerImageName(project)}:{BuildId.ToLower()}";
                var imageNameAndTagOnRegistry = $"{DockerRegistryServer}/{DockerRegistryUserName}/{imageNameAndTag}";

                DockerTasks.DockerTag(settings => settings
                    .SetSourceImage(imageNameAndTag)
                    .SetTargetImage(imageNameAndTagOnRegistry)
                );
                DockerTasks.DockerPush(settings => settings
                    .SetName(imageNameAndTagOnRegistry)
                );

            }
        }

        private string SanitizeForKubernetesConfig(string str)
        {
            return str.Replace(".", "-");
        }

        private string GetServiceName(string group)
        {
            return $"svc-{group}";
        }

        private string GetConfigMapGroup(string group)
        {
            return $"config-group-{group}";
        }

        private string GetConfigMapApp(string appName)
        {
            return $"config-app-{appName}";
        }

        private async Task GenerateKubernetesYamlNamespace(AppDescriptor appDescriptor, AnabasisBuildEnvironment anabasisBuildEnvironment)
        {
            var @namespace = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / $"{anabasisBuildEnvironment}".ToLower() / "namespace" / "namespace.yaml")).First() as k8s.Models.V1Namespace;

            @namespace.Metadata.Name = appDescriptor.AppGroup;
            @namespace.Metadata.Labels["group"] = appDescriptor.AppGroup;

            var namespaceYaml = Yaml.SaveToString(@namespace);

            var namespaceYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, $"{anabasisBuildEnvironment}".ToLower(), "namespace", "namespace.yaml");

            WriteFile(namespaceYamlPath, namespaceYaml);

        }

        private async Task GenerateKubernetesYamlGroupConfigMap(AppDescriptor appDescriptor, AnabasisBuildEnvironment anabasisBuildEnvironment)
        {

            var groupConfigMap = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / $"{anabasisBuildEnvironment}".ToLower() / "group" / "config.group.yaml")).First() as k8s.Models.V1ConfigMap;

           // var configGroupYaml = File.ReadAllText($"{KustomizeDirectory}/{appDescriptor}/config.group.yaml");

            groupConfigMap.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            groupConfigMap.Metadata.Name = $"config.group.{appDescriptor.AppGroup}";

            groupConfigMap.Metadata.Labels["release"] = appDescriptor.AppRelease;
            groupConfigMap.Metadata.Labels["group"] = appDescriptor.AppGroup;

            //groupConfigMap.Data["config.group.yaml"] = configGroupYaml;

            var groupConfigMapYaml = Yaml.SaveToString(groupConfigMap);

            var groupConfigMapYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, $"{anabasisBuildEnvironment}".ToLower(), "group", "config.group.yaml");

            WriteFile(groupConfigMapYamlPath, groupConfigMapYaml);

        }

        private async Task GenerateKubernetesYamlDeployment(AppDescriptor appDescriptor, AnabasisBuildEnvironment anabasisBuildEnvironment)
        {
            var deployment = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / $"{anabasisBuildEnvironment}".ToLower() / "api" / "deployment.yaml")).First() as k8s.Models.V1Deployment;

            deployment.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            deployment.Metadata.Name = appDescriptor.AppShortName;

            deployment.Metadata.Labels["app"] = appDescriptor.AppShortName;
            deployment.Metadata.Labels["release"] = appDescriptor.AppRelease;
            deployment.Metadata.Labels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Selector.MatchLabels["app"] = appDescriptor.AppShortName;
            deployment.Spec.Selector.MatchLabels["release"] = appDescriptor.AppRelease;
            deployment.Spec.Selector.MatchLabels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Template.Metadata.Labels["app"] = appDescriptor.AppShortName;
            deployment.Spec.Template.Metadata.Labels["release"] = appDescriptor.AppRelease;

            var container = deployment.Spec.Template.Spec.Containers.First();

            container.Name = appDescriptor.AppShortName;
            container.Image = $"{DockerRegistryServer}/{appDescriptor.AppShortName}/{Branch}:{appDescriptor.AppRelease}";

            var configMapGroupVolume = deployment.Spec.Template.Spec.Volumes[0];
            configMapGroupVolume.ConfigMap.Name = $"config-group-{appDescriptor.AppGroup}";

            var configMapAppVolume = deployment.Spec.Template.Spec.Volumes[1];
            configMapAppVolume.ConfigMap.Name = $"config-app-{appDescriptor.AppShortName}";

            var deploymentYaml = Yaml.SaveToString(deployment);
            var deploymentYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, $"{anabasisBuildEnvironment}".ToLower(), "api", "deployment.yaml");

            WriteFile(deploymentYamlPath, deploymentYaml);

        }

        private async Task GenerateKubernetesYamlService(AppDescriptor appDescriptor, AnabasisBuildEnvironment anabasisBuildEnvironment)
        {
            var service = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / $"{anabasisBuildEnvironment}".ToLower() / "api" / "service.yaml")).First() as k8s.Models.V1Service;

            service.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            service.Metadata.Name = GetServiceName(appDescriptor.AppGroup);

            service.Metadata.Labels["app"] = appDescriptor.AppShortName;
            service.Metadata.Labels["release"] = appDescriptor.AppRelease;
            service.Metadata.Labels["group"] = appDescriptor.AppGroup;
            service.Spec.Selector["release"] = appDescriptor.AppRelease;
            service.Spec.Selector["app"] = appDescriptor.AppShortName;

            var serviceYaml = Yaml.SaveToString(service);
            var serviceYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, $"{anabasisBuildEnvironment}".ToLower(), "api", "service.yaml");

            WriteFile(serviceYamlPath, serviceYaml);

        }

        private async Task GenerateKubernetesYamlAppConfigMap(AppDescriptor appDescriptor, AnabasisBuildEnvironment anabasisBuildEnvironment)
        {
            var appConfigMap = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / $"{anabasisBuildEnvironment}".ToLower() / "api" / "config.app.yaml")).First() as k8s.Models.V1ConfigMap;

           // var configAppYaml = File.ReadAllText($"{BuildProjectKustomizeDirectory}/{appGroup}/{appName}/config.app.yaml");

            appConfigMap.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            appConfigMap.Metadata.Name = $"config.app";

            appConfigMap.Metadata.Labels["app"] = appDescriptor.AppShortName;
            appConfigMap.Metadata.Labels["release"] = appDescriptor.AppRelease;
            appConfigMap.Metadata.Labels["group"] = appDescriptor.AppGroup;

             appConfigMap.Data["config.app.yaml"] = "data";

            var appConfigMapYaml = Yaml.SaveToString(appConfigMap);
            var appConfigMapYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, $"{anabasisBuildEnvironment}".ToLower(), "api", "config.app.yaml");

            WriteFile(appConfigMapYamlPath, appConfigMapYaml);

        }

        private void WriteFile(string filePath, string fileContent)
        {
            (new FileInfo(filePath)).Directory.Create();

            File.WriteAllText(filePath, fileContent);
        }

        private void DeployApps(params string[] appNames)
        {

            var lowerCaseAppGroup = SanitizeForKubernetesConfig(GroupToBeDeployed.ToLower());

            //install namespace

            //install group

            //install app

            (string app, string appName, string appShortName)[] apps =
                appNames
                    .Select(appName => ($"{lowerCaseAppGroup}.{appName.ToLower()}", $"{appName.ToLower()}", $"{GroupToBeDeployed}.{appName.ToLower()}"))
                    .ToArray();

            foreach (var app in apps)
            {
                //HelmInstall(app.appName, HelmChartsDirectory / "api", DomainToBeDeployed, DomainToBeDeployed);
            }

        }

    }
}