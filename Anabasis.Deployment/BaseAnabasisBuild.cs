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
    //todo: fetch the config in the build folder (artefact)
    //todo: inject build directory in app constructor
    //todo: parallelisation option for tests
    //todo: kube service by app

    public abstract partial class BaseAnabasisBuild : NukeBuild
    {

        private readonly string Configuration = "Release";
        private readonly string RuntimeDockerImage = "mcr.microsoft.com/dotnet/aspnet:5.0";

        public abstract bool DeployOnKubernetes { get; }

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
        [Parameter("Set the build environment")]
        public AnabasisBuildEnvironment AnabasisBuildEnvironment;

        [Parameter("Solution source directory")]
        public AbsolutePath SourceDirectory = RootDirectory / "src";

        [Parameter("Solution test directory")]
        public AbsolutePath TestsDirectory = RootDirectory / "tests";

        [Parameter("Kubernetes cluster configuration file")]
        public readonly AbsolutePath KubeConfigPath = RootDirectory / ".kube" / "kubeconfig";

        private AbsolutePath BuildDirectory => RootDirectory / "build";

        private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

        [Required]
        [GitRepository]
        public readonly GitRepository GitRepository;

        private string Branch => GitRepository?.Branch ?? "NO_GIT_REPOS_DETECTED";

        private AbsolutePath DockerFile => BuildDirectory / "docker" / "build.dockerfile";
        public AbsolutePath BuildProjectKustomizeDirectory { get; set; }
        private AbsolutePath BuildProjectKustomizeTemplateDirectory => BuildProjectKustomizeDirectory / "templates";
        private AbsolutePath DefaultKustomizationFile => BuildProjectKustomizeDirectory / "kustomization.yaml";

        protected BaseAnabasisBuild()
        {

            //for unit tests
            if (null != BuildProjectDirectory)
            {
                BuildProjectKustomizeDirectory = BuildProjectDirectory / "kustomize";
            }
        }

        public void DeleteIfExist(DirectoryInfo directoryInfo)
        {
            if (Directory.Exists(directoryInfo.FullName))
            {
                Directory.Delete(directoryInfo.FullName, true);
            }
        }

        public void SetupKustomize(AppDescriptor appDescriptor)
        {
            //clean kustomize directories structure
            DeleteIfExist(appDescriptor.AppSourceKustomizeBaseDirectory);

            //create kustomize directories structure
            Directory.CreateDirectory(appDescriptor.AppSourceKustomizeDirectory.FullName);
            Directory.CreateDirectory(appDescriptor.AppSourceKustomizeBaseDirectory.FullName);

            foreach (var env in Enum.GetValues(typeof(AnabasisBuildEnvironment)).Cast<AnabasisBuildEnvironment>())
            {
                var envDirectory = Path.Combine(appDescriptor.AppSourceKustomizeDirectory.FullName, $"{env}".ToLower());

                if (!Directory.Exists(envDirectory))
                {
                    Directory.CreateDirectory(envDirectory);
                    File.Copy(DefaultKustomizationFile, Path.Combine(envDirectory, "kustomization.yaml"));
                }

            }
        }

        public async Task GenerateBaseKustomize(AppDescriptor appDescriptor)
        {
            await GenerateKubernetesYamlNamespace(appDescriptor);
            await GenerateKubernetesYamlService(appDescriptor);
            await GenerateKubernetesYamlDeployment(appDescriptor);
        }

        public virtual Target PreBuildChecks => _ => _
        .Executes(() =>
        {
            if(DeployOnKubernetes) 
                Assert.FileExists(KubeConfigPath);
        });

        public virtual Target Clean => _ => _
            .DependsOn(PreBuildChecks)
            .Executes(() =>
            {

                foreach (var directory in SourceDirectory.GlobDirectories("**/bin", "**/obj")
                                   .Concat(TestsDirectory.GlobDirectories("**/bin", "**/obj")))
                    try
                    {
                        if ($"{directory}".Contains(new FileInfo(BuildProjectFile).Directory.FullName))
                            continue;

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

        public virtual Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                foreach (var projectFilePath in GetAllProjects())
                {
                    DotNetTasks.DotNetRestore(dotNetRestoreSettings => dotNetRestoreSettings.SetProjectFile(projectFilePath.FullName));
                }

            });

        public virtual Target Publish => _ => _
           .DependsOn(Restore)
           .Executes(() =>
           {
               foreach(var projectFilePath in GetAllProjects())
               {
                   PublishApplication(projectFilePath.FullName);
               }
           });

        public virtual Target Test => _ => _
           .DependsOn(Publish)
           .Executes(() =>
           {
               foreach (var testProjectPath in GetTestsProjects())
               {
                   ExecuteTests(testProjectPath.FullName);
               }
           });

        public virtual Target DockerPackage => _ => _
           // .DependsOn(Test)
            .Executes(() =>
            {
                foreach (var appDescriptor in GetAppsToDeploy())
                {
                   CreateDockerImage(appDescriptor);
                }

            });

        public virtual Target DockerPush => _ => _
            .DependsOn(DockerPackage)
            .Executes(() =>
            {
                foreach (var appDescriptor in GetAppsToDeploy())
                {
                    PushDockerImage(appDescriptor);
                }
            });

        public Target GenerateKubernetesYaml => _ => _
           // .DependsOn(DockerPackage)
            .Executes(async () =>
            {
                foreach (var app in GetAppsToDeploy())
                {
                    SetupKustomize(app);
                    await GenerateBaseKustomize(app);
                }

            });
        public Target Deploy => _ => _
            .DependsOn(GenerateKubernetesYaml)
            .Executes(async () =>
            {

                //$"--kubeconfig={ValidateKubeConfigPath()}" : "";

                // KubernetesTasks.Kubernetes($"apply -k  {KubeConfigArgument}");

                var appsToBeDeployed = GetAppsToDeploy();

                foreach (var appToBeDeployed in appsToBeDeployed)
                {
                    //  await GenerateBaseKustomize(appToBeDeployed);
                }

            });



        public AppDescriptor[] GetAppsToDeploy()
        {
            var appDescriptors = new List<AppDescriptor>();

            var applicationProjectFiles = GetApplicationProjects();

            foreach (var applicationProjectFile in applicationProjectFiles)
            {
                var appName = Path.GetFileNameWithoutExtension(applicationProjectFile.FullName);

                var projectBuildDirectory = new DirectoryInfo(ArtifactsDirectory / appName);


                var appDescriptor = new AppDescriptor(
                    applicationProjectFile,
                    projectBuildDirectory,
                    appName,
                    BuildId);

                appDescriptors.Add(appDescriptor);
            }

            return appDescriptors.ToArray();

        }

        protected virtual FileInfo[] GetTestsProjects()
        {
            return PathConstruction.GlobFiles(TestsDirectory, $"**/*.Tests.csproj")
                .OrderBy(path => $"{path}")
                .Select(path => new FileInfo($"{path}"))
                .ToArray();
        }

        protected virtual FileInfo[] GetAllProjects()
        {
            var projects = GetApplicationProjects()
                .Concat(GetTestsProjects())
                .Concat(GetNugetPackageProjects())
                .Distinct()
                .OrderBy(s => s.FullName)
                .Where(s=> s.FullName != BuildProjectFile)
                .ToArray();

            return projects;
        }

        protected virtual FileInfo[] GetApplicationProjects(AbsolutePath directory = null)
        {
            directory ??= SourceDirectory;

            return PathConstruction.GlobFiles(directory, "**/*.App.csproj").OrderBy(path => $"{path}")
                                                                           .Select(path => new FileInfo($"{path}"))
                                                                           .ToArray();
        }

        protected virtual FileInfo[] GetNugetPackageProjects()
        {
            return PathConstruction.GlobFiles(SourceDirectory, "**/*.csproj")
                                                                             .OrderBy(path => $"{path}")
                                                                             .Select(path => new FileInfo($"{path}"))
                                                                             .ToArray();
        }

        protected void CreateDockerImage(AppDescriptor appDescriptor)
        {
            var dockerFile = ArtifactsDirectory / Path.GetFileName(DockerFile);
            FileSystemTasks.CopyFile(DockerFile, dockerFile, FileExistsPolicy.OverwriteIfNewer);

            var projectName = Path.GetFileNameWithoutExtension(appDescriptor.ProjectFilePath.FullName);
            var publishedPath = ArtifactsDirectory / projectName;
            var dockerImageName = GetProjectDockerImageName(appDescriptor.ProjectFilePath.FullName);

            DockerTasks.DockerBuild(s => s
                .SetFile(DockerFile)
                .AddBuildArg($"RUNTIME_IMAGE={RuntimeDockerImage}")
                .AddBuildArg($"PROJECT_NAME={projectName}")
                .AddBuildArg($"BUILD_ID={BuildId}")
                .SetTag($"{GetProjectDockerImageName(appDescriptor.ProjectFilePath.FullName)}:{BuildId.ToLower()}")
                .SetPath(publishedPath)
                .EnableQuiet()
                .EnableForceRm());
        }

        protected void ExecuteTests(string testProjectPath, bool nobuild = false)
        {

            DotNetTasks.DotNetTest(dotNetTestSettings =>
            {
                dotNetTestSettings = dotNetTestSettings
                    .SetConfiguration(Configuration)
                    .SetProjectFile(testProjectPath);

                if (nobuild)
                    dotNetTestSettings = dotNetTestSettings.EnableNoBuild();

                return dotNetTestSettings;
            });

        }

        protected void PublishApplication(string projectPath)
        {

            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var outputPath = ArtifactsDirectory / projectName;

            Log.Information($"Publishing {projectName} in {outputPath}");

            DotNetTasks.DotNetRestore(s => s.SetProjectFile(projectPath));

            DotNetTasks.DotNetPublish(s => s
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProject(projectPath)
                .SetOutput(outputPath)
                );
        }

        protected string GetProjectDockerImageName(string project)
        {
            var prefix = Path.GetFileNameWithoutExtension(project).ToLower();
            return $"{prefix}-{GitRepository.Branch.Replace("/", "")}".ToLower();
        }

        private void PushDockerImage(AppDescriptor appDescriptor)
        {

            DockerTasks.DockerLogin(dockerLoginSettings => dockerLoginSettings
                .SetServer(DockerRegistryServer)
                .SetUsername(DockerRegistryUserName)
                .SetPassword(DockerRegistryPassword)
            );

            var imageNameAndTag = $"{GetProjectDockerImageName(appDescriptor.ProjectFilePath.FullName)}:{BuildId.ToLower()}";
            var imageNameAndTagOnRegistry = $"{DockerRegistryServer}/{DockerRegistryUserName}/{imageNameAndTag}";

            DockerTasks.DockerTag(settings => settings
                .SetSourceImage(imageNameAndTag)
                .SetTargetImage(imageNameAndTagOnRegistry)
            );

            DockerTasks.DockerPush(settings =>
                settings.SetName(imageNameAndTagOnRegistry)
           );

        }

        private async Task GenerateKubernetesYamlNamespace(AppDescriptor appDescriptor)
        {
            var @namespace = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / "namespace.yaml")).First() as k8s.Models.V1Namespace;

            @namespace.Metadata.Name = appDescriptor.AppGroup;
            @namespace.Metadata.Labels["group"] = appDescriptor.AppGroup;

            var namespaceYaml = Yaml.SaveToString(@namespace);

            var namespaceYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, "namespace.yaml");

            WriteFile(namespaceYamlPath, namespaceYaml);

        }

        private async Task GenerateKubernetesYamlDeployment(AppDescriptor appDescriptor)
        {
            var deployment = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / "deployment.yaml")).First() as k8s.Models.V1Deployment;

            deployment.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            deployment.Metadata.Name = appDescriptor.AppLongName;

            deployment.Metadata.Labels["app"] = appDescriptor.AppLongName;
            deployment.Metadata.Labels["release"] = appDescriptor.AppRelease;
            deployment.Metadata.Labels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Selector.MatchLabels["app"] = appDescriptor.AppLongName;
            deployment.Spec.Selector.MatchLabels["release"] = appDescriptor.AppRelease;
            deployment.Spec.Selector.MatchLabels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Template.Metadata.Labels["app"] = appDescriptor.AppLongName;
            deployment.Spec.Template.Metadata.Labels["release"] = appDescriptor.AppRelease;
            deployment.Spec.Template.Metadata.Labels["group"] = appDescriptor.AppGroup;

            var container = deployment.Spec.Template.Spec.Containers.First();

            container.Name = appDescriptor.AppLongName;
            container.Image = $"{DockerRegistryServer}/{appDescriptor.AppLongName}/{Branch}:{appDescriptor.AppRelease}";

            var deploymentYaml = Yaml.SaveToString(deployment);
            var deploymentYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, "deployment.yaml");

            WriteFile(deploymentYamlPath, deploymentYaml);

        }

        private async Task GenerateKubernetesYamlService(AppDescriptor appDescriptor)
        {
            var service = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / "service.yaml")).First() as k8s.Models.V1Service;

            service.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            service.Metadata.Name = $"svc-{appDescriptor.AppLongName}";

            service.Metadata.Labels["app"] = appDescriptor.AppLongName;
            service.Metadata.Labels["release"] = appDescriptor.AppRelease;
            service.Metadata.Labels["group"] = appDescriptor.AppGroup;
            service.Spec.Selector["release"] = appDescriptor.AppRelease;
            service.Spec.Selector["app"] = appDescriptor.AppLongName;

            var serviceYaml = Yaml.SaveToString(service);
            var serviceYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, "service.yaml");

            WriteFile(serviceYamlPath, serviceYaml);

        }

        private void WriteFile(string filePath, string fileContent)
        {
            (new FileInfo(filePath)).Directory.Create();

            File.WriteAllText(filePath, fileContent);
        }

    }
}