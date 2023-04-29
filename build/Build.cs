using System;
using System.Linq;
using System.Security.Policy;
using JetBrains.Annotations;
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
    GitHubActionsImage.UbuntuLatest,
    On = new [] { GitHubActionsTrigger.Push },
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
        { nameof(Clean), nameof(Compile), nameof(Pack), nameof(PublishToGitHubNuget), nameof(Publish), nameof(PublishBinary) },
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

    [Parameter][Secret] readonly string NuGetApiKey;

    GitHubActions GitHubActions => GitHubActions.Instance;

    [GitRepository] readonly GitRepository Repository;

    [LatestNuGetVersion(
        packageId: "dotnet-nperf",
        IncludePrerelease = false)]
    [CanBeNull]
    readonly NuGetVersion DotnetNperfVersion;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

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
        .OnlyWhenStatic(() => GitHubActions.Instance != null)
        .Produces(ArtifactsDirectory / "nperf")
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(Solution.src.nperf)
                .SetConfiguration(Configuration)
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .SetOutput(ArtifactsDirectory)
                .SetRuntime("linux-x64")
                .SetProcessArgumentConfigurator(a => a
                    .Add("-p:PublishAot=true")
                    .Add("-p:StripSymbols=true")
                )
            );
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .DependsOn(Clean)
        .Produces(ArtifactsDirectory / "*.nupkg")
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
                .SetOutputDirectory(ArtifactsDirectory)
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
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .SetSource("https://nuget.pkg.github.com/psimsa/index.json")
                .SetApiKey(GitHubActions.Token)
            );
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetApiKey)
            );
        });
}
