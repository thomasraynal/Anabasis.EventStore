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

namespace Anabasis.Deployment
{
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

        private AbsolutePath BuildDirectory => RootDirectory / "build";
        private AbsolutePath SourceDirectory => RootDirectory / "src";
        private AbsolutePath TestsDirectory => RootDirectory / "tests";
        private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
        private AbsolutePath DeploymentsDirectory => RootDirectory / "deployments";
        private string Branch => GitRepository?.Branch ?? "NO_GIT_REPOS_DETECTED";
        private AbsolutePath OneForAllDockerFile => BuildDirectory / "docker" / "build.nuke.app.dockerfile";
        private AbsolutePath KustomizeDirectory => BuildProjectDirectory / "kustomize";
        private AbsolutePath KustomizeTemplateDirectory => KustomizeDirectory / "templates";
        private AbsolutePath KustomizeOverlaysDirectory => KustomizeDirectory / "overlays";
        private AbsolutePath KustomizeBaseDirectory => KustomizeDirectory / "base";

        private string GeneratedFolderName = "generated";

        private Kubernetes _client;
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

        public Target Deploy => _ => _
            // .DependsOn(Package)
            .Executes(async () =>
            {
                var appGroup = SanitizeForKubernetesConfig(GroupToBeDeployed.ToLower());

                await GenerateNamespace(appGroup);
                await GenerateGroupConfigMap(appGroup, BuildId);

                var appsToBeDeployed = GetAppsToBeDeployed();

                foreach (var (app, appName, appShortName) in appsToBeDeployed)
                {
                    await GenerateService(appName, appGroup, BuildId);
                    await GenerateDeployment(appName, appGroup, BuildId);
                    await GenerateAppConfigMap(appName, appGroup, BuildId);
                }

            });


        private (string app, string appName, string appShortName)[] GetAppsToBeDeployed()
        {

            var lowerCaseAppGroup = SanitizeForKubernetesConfig(GroupToBeDeployed.ToLower());

            var apps = GetApplicationProjects().Select(project => SanitizeForKubernetesConfig(Path.GetFileName(project).Replace(".csproj", "")));

            return apps.Select(appName => ($"{lowerCaseAppGroup}.{appName.ToLower()}", $"{appName.ToLower()}", $"{GroupToBeDeployed}.{appName.ToLower()}")).ToArray();

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

        private async Task GenerateNamespace(string appGroup)
        {
            var @namespace = (await Yaml.LoadAllFromFileAsync(TemplateDirectory / "namespace" / "namespace.yaml")).First() as k8s.Models.V1Namespace;
            @namespace.Metadata.Name = appGroup;
            @namespace.Metadata.Labels["group"] = appGroup;

            var namespaceYaml = Yaml.SaveToString(@namespace);
            var namespaceYamlPath = GetGeneratedGroupYamlPath(appGroup, "namespace", "namespace.yaml");

            WriteFile(namespaceYamlPath, namespaceYaml);

        }

        private async Task GenerateGroupConfigMap(string appGroup, string release)
        {

            var groupConfigMap = (await Yaml.LoadAllFromFileAsync(TemplateDirectory / "group" / "group.yaml")).First() as k8s.Models.V1ConfigMap;

            var configGroupYaml = File.ReadAllText($"{ConfigsDirectory}/{appGroup}/config.group.yaml");

            groupConfigMap.Metadata.NamespaceProperty = appGroup;
            groupConfigMap.Metadata.Name = $"config.group.{appGroup}";

            groupConfigMap.Metadata.Labels["release"] = release;
            groupConfigMap.Metadata.Labels["group"] = appGroup;

            groupConfigMap.Data["config.group.yaml"] = configGroupYaml;

            var groupConfigMapYaml = Yaml.SaveToString(groupConfigMap);
            var groupConfigMapYamlPath = GetGeneratedGroupYamlPath(appGroup, "group", "config.group.yaml");

            WriteFile(groupConfigMapYamlPath, groupConfigMapYaml);

        }

        private async Task GenerateDeployment(string appName, string appGroup, string release)
        {
            var deployment = (await Yaml.LoadAllFromFileAsync(TemplateDirectory / "api" / "deployment.yaml")).First() as k8s.Models.V1Deployment;

            deployment.Metadata.NamespaceProperty = appGroup;
            deployment.Metadata.Name = appName;

            deployment.Metadata.Labels["app"] = appName;
            deployment.Metadata.Labels["release"] = release;
            deployment.Metadata.Labels["group"] = appGroup;

            deployment.Spec.Selector.MatchLabels["app"] = appName;
            deployment.Spec.Selector.MatchLabels["group"] = appGroup;

            deployment.Spec.Template.Metadata.Labels["app"] = appName;
            deployment.Spec.Template.Metadata.Labels["release"] = release;

            var container = deployment.Spec.Template.Spec.Containers.First();

            container.Name = appName;
            container.Image = $"{DockerRegistryServer}/{appName}/{Branch}:{release}";

            var configMapGroupVolume = deployment.Spec.Template.Spec.Volumes[0];
            configMapGroupVolume.ConfigMap.Name = $"config-group-{appGroup}";

            var configMapAppVolume = deployment.Spec.Template.Spec.Volumes[1];
            configMapAppVolume.ConfigMap.Name = $"config-app-{appName}";

            var deploymentYaml = Yaml.SaveToString(deployment);
            var deploymentYamlPath = GetGeneratedAppYamlPath(appName, appGroup, "api", "deployment.yaml");

            WriteFile(deploymentYamlPath, deploymentYaml);

        }

        private async Task GenerateService(string appName, string appGroup, string release)
        {
            var service = (await Yaml.LoadAllFromFileAsync(TemplateDirectory / "api" / "service.yaml")).First() as k8s.Models.V1Service;

            service.Metadata.NamespaceProperty = appGroup;
            service.Metadata.Name = GetServiceName(appGroup);

            service.Metadata.Labels["app"] = appName;
            service.Metadata.Labels["release"] = release;
            service.Metadata.Labels["group"] = appGroup;
            service.Spec.Selector["release"] = release;
            service.Spec.Selector["app"] = appName;

            var serviceYaml = Yaml.SaveToString(service);
            var serviceYamlPath = GetGeneratedAppYamlPath(appName, appGroup, "api", "service.yaml");

            WriteFile(serviceYamlPath, serviceYaml);

        }

        private async Task GenerateAppConfigMap(string appName, string appGroup, string release)
        {
            var appConfigMap = (await Yaml.LoadAllFromFileAsync(TemplateDirectory / "api" / "app.yaml")).First() as k8s.Models.V1ConfigMap;

            var configAppYaml = File.ReadAllText($"{ConfigsDirectory}/{appGroup}/{appName}/config.app.yaml");

            appConfigMap.Metadata.NamespaceProperty = appGroup;
            appConfigMap.Metadata.Name = $"config.app.{appName}";

            appConfigMap.Metadata.Labels["release"] = release;
            appConfigMap.Metadata.Labels["group"] = appGroup;

            appConfigMap.Data["config.app.yaml"] = configAppYaml;

            var appConfigMapYaml = Yaml.SaveToString(appConfigMap);
            var appConfigMapYamlPath = GetGeneratedAppYamlPath(appName, appGroup, "api", "config.app.yaml");

            WriteFile(appConfigMapYamlPath, appConfigMapYaml);

        }

        private string GetGeneratedGroupYamlPath(string appGroup, string ressource, string yamlName)
        {
            return DeploymentsDirectory / GeneratedFolderName / appGroup / ressource / yamlName;
        }

        private string GetGeneratedAppYamlPath(string appName, string appGroup, string ressource, string yamlName)
        {
            return DeploymentsDirectory / GeneratedFolderName / appGroup / appName / ressource / yamlName;
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