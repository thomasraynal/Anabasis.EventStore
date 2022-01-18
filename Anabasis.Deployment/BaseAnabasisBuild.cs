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

    public abstract class BaseAnabasisBuild : NukeBuild
    {

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
        [Parameter("Set the build environment")]
        public string AnabasisBuildEnvironment;

        [Parameter("Solution source directory")]
        public AbsolutePath SourceDirectory = RootDirectory;

        [Parameter("Solution test directory")]
        public AbsolutePath TestsDirectory => RootDirectory;

        [Parameter]
        public readonly AbsolutePath KubeConfigPath = RootDirectory / ".kube" / "kubeconfig";

        private AbsolutePath BuildDirectory => RootDirectory / "build";

        private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

        [GitRepository]
        private readonly GitRepository GitRepository;
        private string Branch => GitRepository?.Branch ?? "NO_GIT_REPOS_DETECTED";

        private AbsolutePath OneForAllDockerFile => BuildDirectory / "docker" / "build.nuke.app.dockerfile";
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

        //https://stackoverflow.com/a/3822913
        private void CopyFilesRecursively(string sourcePath, string targetPath)
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

        public Target PreBuildChecks => _ => _
        .Executes(() =>
        {
            Assert.FileExists(KubeConfigPath);
        });

        public Target Clean => _ => _
            .DependsOn(PreBuildChecks)
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
        public Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                foreach (var project in GetAllProjects())
                {
                    DotNetTasks.DotNetRestore(dotNetRestoreSettings => dotNetRestoreSettings.SetProjectFile(project));
                }

            });
        public Target Publish => _ => _
           .DependsOn(Restore)
           .Executes(() =>
           {
               // var applications = GetAllProjects();

               //PublishApplications(applications);

           });

        public Target PostBuildChecks => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            var applications = GetApplicationProjects();

            foreach (var app in applications)
            {

            }

        });

        public Target Test => _ => _
           .DependsOn(Publish)
           .Executes(() =>
           {
               ExecuteTests(GetTestsProjects());
           });
        public Target Package => _ => _
            .DependsOn(Test)
            .Executes(() =>
            {
                var applications = GetApplicationProjects();

                //BuildContainers(applications);

                //PushContainers(applications);
            });
        public Target GenerateKubernetesYaml => _ => _
            .DependsOn(Package)
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
                    await GenerateBaseKustomize(appToBeDeployed);
                }

            });



        public AppDescriptor[] GetAppsToDeploy()
        {
            var appDescriptors = new List<AppDescriptor>();

            var applicationProjectFiles = GetApplicationProjects();

            foreach (var applicationProjectFile in applicationProjectFiles)
            {


                var appName = SanitizeForKubernetesConfig(Path.GetFileName(applicationProjectFile).Replace(".csproj", ""));
                var appSourceDirectory = new FileInfo(applicationProjectFile).Directory;


                var appDescriptor = new AppDescriptor(
                    appSourceDirectory,
                    appName,
                    BuildId);

                appDescriptors.Add(appDescriptor);
            }

            return appDescriptors.ToArray();

        }

        protected virtual string[] GetTestsProjects()
        {
            return PathConstruction.GlobFiles(TestsDirectory, $"**/*.Tests.csproj")
                .Select(path => $"{path}")
                .OrderBy(path => path)
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

            return PathConstruction.GlobFiles(directory, "**/*.App.csproj").Select(path => path.ToString()).OrderBy(path => path).ToArray();
        }

        protected virtual string[] GetNugetPackageProjects()
        {
            return PathConstruction.GlobFiles(SourceDirectory, "**/*.csproj").Select(path => path.ToString()).OrderBy(path => path).ToArray();
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
            deployment.Metadata.Name = appDescriptor.AppShortName;

            deployment.Metadata.Labels["app"] = appDescriptor.AppShortName;
            deployment.Metadata.Labels["release"] = appDescriptor.AppRelease;
            deployment.Metadata.Labels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Selector.MatchLabels["app"] = appDescriptor.AppShortName;
            deployment.Spec.Selector.MatchLabels["release"] = appDescriptor.AppRelease;
            deployment.Spec.Selector.MatchLabels["group"] = appDescriptor.AppGroup;

            deployment.Spec.Template.Metadata.Labels["app"] = appDescriptor.AppShortName;
            deployment.Spec.Template.Metadata.Labels["release"] = appDescriptor.AppRelease;
            deployment.Spec.Template.Metadata.Labels["group"] = appDescriptor.AppGroup;

            var container = deployment.Spec.Template.Spec.Containers.First();

            container.Name = appDescriptor.AppShortName;
            container.Image = $"{DockerRegistryServer}/{appDescriptor.AppShortName}/{Branch}:{appDescriptor.AppRelease}";

            var deploymentYaml = Yaml.SaveToString(deployment);
            var deploymentYamlPath = Path.Combine(appDescriptor.AppSourceKustomizeBaseDirectory.FullName, "deployment.yaml");

            WriteFile(deploymentYamlPath, deploymentYaml);

        }

        private async Task GenerateKubernetesYamlService(AppDescriptor appDescriptor)
        {
            var service = (await Yaml.LoadAllFromFileAsync(BuildProjectKustomizeTemplateDirectory / "service.yaml")).First() as k8s.Models.V1Service;

            service.Metadata.NamespaceProperty = appDescriptor.AppGroup;
            service.Metadata.Name = GetServiceName(appDescriptor.AppGroup);

            service.Metadata.Labels["app"] = appDescriptor.AppShortName;
            service.Metadata.Labels["release"] = appDescriptor.AppRelease;
            service.Metadata.Labels["group"] = appDescriptor.AppGroup;
            service.Spec.Selector["release"] = appDescriptor.AppRelease;
            service.Spec.Selector["app"] = appDescriptor.AppShortName;

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