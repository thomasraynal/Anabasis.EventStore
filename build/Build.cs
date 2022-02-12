using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Serilog;
using static Nuke.Common.IO.PathConstruction;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.PushNuget);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Should run integration tests?")]
    public readonly bool SkipIntegrationTests = false;

    [Solution] readonly Solution Solution;

    public AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    private static readonly string BuildVersion = AppVeyor.Instance?.BuildVersion ?? "1.0.0";

    public virtual FileInfo[] GetAllProjects()
    {
        return PathConstruction.GlobFiles(RootDirectory, "**/Anabasis.*.csproj").OrderBy(path => $"{path}")
                                                                       .Select(path => new FileInfo($"{path}"))
                                                                       .ToArray();
    }

    public virtual FileInfo[] GetAllNonTestProjects()
    {
        var allProjects = GetAllProjects();
        var testProjects = GetAllTestsProjects();

        return allProjects.Where(project => !testProjects.Contains(project)).ToArray();
    }

    public virtual FileInfo[] GetAllTestsProjects()
    {
        var testProjects = PathConstruction.GlobFiles(RootDirectory, "**/Anabasis.*.Tests*.csproj").OrderBy(path => $"{path}")
                                                                       .Select(path => new FileInfo($"{path}"))
                                                                       .ToArray();

        if (SkipIntegrationTests)
            testProjects = testProjects.Where(testProject => !testProject.Name.ToLower().Contains("integration")).ToArray();

        return testProjects;
    }

    public virtual FileInfo[] GetAllReleaseNugetPackages()
    {
        return PathConstruction.GlobFiles(RootDirectory, $"**/Anabasis.*.{BuildVersion}.nupkg")
                                                                       .OrderBy(path => $"{path}")
                                                                       .Select(path => new FileInfo($"{path}"))
                                                                       .ToArray();
    }

    Target DockerComposeDown => _ => _
        .OnlyWhenDynamic(() => !SkipIntegrationTests)
        .Executes(() =>
        {
            var process = ProcessTasks.StartProcess("docker-compose", "down", RootDirectory, logOutput: true);
            process.AssertWaitForExit();
        });

    Target DockerComposeUp => _ => _
        .OnlyWhenDynamic(() => !SkipIntegrationTests)
        .DependsOn(DockerComposeDown)
        .Executes(async () =>
            {

                var dockerComposeUpTask = Task.Run(() =>
                {
                    var process = ProcessTasks.StartProcess("docker-compose", "up", RootDirectory, logOutput: true);
                    process.AssertWaitForExit();
                });

                await Task.Delay(50_000);

            });

    Target Clean => _ => _
        .DependsOn(DockerComposeUp)
        .Executes(() =>
        {
            foreach (var directory in RootDirectory.GlobDirectories("**/bin", "**/obj"))
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

        });


    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var projectFilePath in GetAllProjects())
            {
                Log.Information($"Restoring {projectFilePath.Name}");

                DotNetTasks.DotNetRestore(dotNetRestoreSettings => dotNetRestoreSettings.SetProjectFile(projectFilePath.FullName));
            }
        });


    Target Publish => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            foreach (var projectFilePath in GetAllNonTestProjects())
            {
                Log.Information($"Publishing {projectFilePath.Name}");

                DotNetTasks.DotNetRestore(s => s.SetProjectFile(projectFilePath.FullName));

                DotNetTasks.DotNetPublish(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetVersion(BuildVersion)
                    .SetProject(projectFilePath.FullName)
                    );
            }
        });

    Target Test => _ => _
        .DependsOn(Publish)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            var exceptions = new List<Exception>();

            try
            {
                foreach (var projectFilePath in GetAllTestsProjects())
                {
                    Log.Information($"Testing {projectFilePath.Name}");

                    DotNetTasks.DotNetTest(dotNetTestSettings =>
                    {
                        dotNetTestSettings = dotNetTestSettings
                            .SetConfiguration(Configuration)
                            .SetProjectFile(projectFilePath.FullName)
                            .SetNoBuild(true);

                        return dotNetTestSettings;
                    });
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);


        });

    Target PushNuget => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            foreach (var nugetPackage in GetAllReleaseNugetPackages())
            {

                var process = ProcessTasks.StartProcess("appveyor", $"PushArtifact {nugetPackage}", RootDirectory, logOutput: true);
                process.AssertWaitForExit();
                //NuGetTasks.NuGetPush(_ => _
                // .SetTargetPath(nugetPackage.FullName)
                // .SetSource("")
                // .SetApiKey("")
            }

        });


}

