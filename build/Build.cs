using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.NugetPush);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    public virtual FileInfo[] GetAllProjects()
    {
        return PathConstruction.GlobFiles(RootDirectory, "**/Anabasis.*.csproj").OrderBy(path => $"{path}")
                                                                       .Select(path => new FileInfo($"{path}"))
                                                                       .ToArray();
    }

    public virtual FileInfo[] GetAllTestsProjects()
    {
        return PathConstruction.GlobFiles(RootDirectory, "**/Anabasis.*.Test.*.csproj").OrderBy(path => $"{path}")
                                                                       .Select(path => new FileInfo($"{path}"))
                                                                       .ToArray();
    }

    Target DockerComposeDown => _ => _
        .Executes(() =>
        {
            var process = ProcessTasks.StartProcess("docker-compose", "down", RootDirectory, logOutput: true);
            process.AssertWaitForExit();
        });

    Target DockerComposeUp => _ => _
        .DependsOn(DockerComposeDown)
        .Executes(async() =>
        {

            var dockerComposeUpTask = Task.Run(() =>
            {
                var process = ProcessTasks.StartProcess("docker-compose", "up", RootDirectory, logOutput: true);
                process.AssertWaitForExit();
            });


            await Task.Delay(30_000);
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
                DotNetTasks.DotNetRestore(dotNetRestoreSettings => dotNetRestoreSettings.SetProjectFile(projectFilePath.FullName));
            }
        });


    Target Publish => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            foreach (var projectFilePath in GetAllProjects())
            {
                Log.Information($"Publishing {projectFilePath.Name}");

                DotNetTasks.DotNetRestore(s => s.SetProjectFile(projectFilePath.FullName));

                DotNetTasks.DotNetPublish(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetProject(projectFilePath.FullName)
                    );
            }
        });

    Target Test => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            foreach (var projectFilePath in GetAllProjects())
            {

                DotNetTasks.DotNetTest(dotNetTestSettings =>
                {
                    dotNetTestSettings = dotNetTestSettings
                        .SetConfiguration(Configuration)
                        .SetProjectFile(projectFilePath.FullName);
                       // .EnableNoBuild();

                    return dotNetTestSettings;
                });
            }
        });

    Target NugetPush => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            //NuGetTasks.NuGetPush();

        });



}
