using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using JetBrains.Annotations;
using Microsoft.Build.Evaluation;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static System.Net.Mime.MediaTypeNames;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "Continuous integration",
    GitHubActionsImage.UbuntuLatest, GitHubActionsImage.WindowsLatest,
    OnPushBranchesIgnore = new[] { "main" },
    InvokedTargets = new[]
    {
        nameof(Clean), nameof(Compile), nameof(PublishBinary), nameof(Pack)
    },
    EnableGitHubToken = true)]
[GitHubActions(
    "Build main and publish to nuget",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[]
    {
        nameof(Clean), nameof(Compile), nameof(Pack), nameof(PublishToGitHubNuget), nameof(Publish),
        nameof(PublishBinary)
    },
    ImportSecrets = new[] { nameof(NuGetApiKey) },
    EnableGitHubToken = true)]
[GitHubActions(
    "Manual publish to Github Nuget",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    InvokedTargets = new[] { nameof(Pack), nameof(PublishToGitHubNuget) },
    EnableGitHubToken = true
)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter] [Secret] readonly string NuGetApiKey;

    GitHubActions GitHubActions => GitHubActions.Instance;

    [GitRepository] readonly GitRepository Repository;

    [LatestNuGetVersion(
        packageId: "dotnet-nperf",
        IncludePrerelease = false)]
    [CanBeNull]
    readonly NuGetVersion DotnetNperfVersion;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    bool IsOnLinux => Environment.OSVersion.Platform == PlatformID.Unix;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(InvokedTargets.Contains(Restore))
            );
        });

    Target PublishBinary => _ => _
        .DependsOn(Compile)
        .DependsOn(Clean)
        // .OnlyWhenStatic(() => GitHubActions.Instance != null)
        .Produces(ArtifactsDirectory / "app")
        .Executes(() =>
        {
            var platform = Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => "linux-x64",
                // PlatformID.MacOSX => "osx-x64",
                PlatformID.Win32NT => "win-x64",
                _ => throw new NotSupportedException()
            };
            var project = Solution.src.nperf;
            var tmpProjectPath = project.Directory / "tmp.csproj";

            var msbuildProject = project.GetMSBuildProject();
            var property = msbuildProject.GetProperty("TargetFrameworks");
            if (property != null)
                msbuildProject.RemoveProperty(property);

            msbuildProject.SetProperty("TargetFramework", "net7.0");

            msbuildProject.Save(tmpProjectPath);

            DotNetPublish(s => s
                .SetProject(tmpProjectPath)
                .SetConfiguration(Configuration)
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .SetOutput(ArtifactsDirectory / "app" / platform)
                .SetRuntime(platform)
                .SetFramework("net7.0")
                .SetProcessArgumentConfigurator(a => a
                    .Add("-p:PublishAot=true")
                    .Add("-p:StripSymbols=true")
                    .Add("-p:InvariantGlobalization=true")
                    .Add("-p:DebuggerSupport=false")
                    .Add("-p:EventSourceSupport=false")
                )
            );

            File.Delete(tmpProjectPath);
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .DependsOn(Clean)
        .Before(PublishBinary)
        .Produces(ArtifactsDirectory / "nuget")
        .Executes(() =>
        {
            var currentVersion = DotnetNperfVersion ?? new NuGetVersion(0, 0, 0);

            var newMajor = 1;
            var newMinor = 0;
            var newPatch = currentVersion.Patch + 1;

            if (newMajor > currentVersion.Major)
            {
                newMinor = 0;
                newPatch = 0;
            }
            else if (newMinor > currentVersion.Minor)
            {
                newPatch = 0;
            }

            var newVersion = new NuGetVersion(newMajor, newMinor, newPatch,
                Repository.IsOnMainOrMasterBranch() ? null : $"preview{GitHubActions?.RunNumber ?? 0}");

            DotNetPack(_ => _
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory / "nuget")
                .SetNoBuild(true)
                .SetNoRestore(true)
                .SetVersion(newVersion.ToString())
                .SetVerbosity(DotNetVerbosity.Normal)
                .SetProject(Solution.src.nperf)
            );
        });

    Target PublishToGitHubNuget => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .OnlyWhenStatic(() => IsOnLinux)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "nuget" / "*.nupkg")
                .SetSource("https://nuget.pkg.github.com/psimsa/index.json")
                .SetApiKey(GitHubActions.Token)
            );
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .OnlyWhenStatic(() => IsOnLinux)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "nuget" / "*.nupkg")
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetApiKey)
            );
        });
}