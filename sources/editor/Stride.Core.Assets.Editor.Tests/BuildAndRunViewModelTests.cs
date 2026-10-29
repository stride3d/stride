// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Building and starting the current project of the session, as the WPF editor does (Build project, Start project, Cancel build),
/// with a fake builder and a fake game launcher.
/// </summary>
public sealed class BuildAndRunViewModelTests
{
    private const string ProjectName = EditorSessionFixture.PackageName;

    /// <summary>
    /// A desktop platform that is not the one of the machine running the tests.
    /// </summary>
    private static PlatformType OtherPlatform => Platform.Type == PlatformType.Windows ? PlatformType.Linux : PlatformType.Windows;

    [Fact]
    public async Task TheCommandsFollowTheSessionAndTheBuild()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        builder.CompleteAtOnce = false;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider);

        // No session
        Assert.False(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.False(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.False(buildAndRun.CancelBuildCommand.IsEnabled);

        // A session with a current project
        buildAndRun.Session = fixture.Session;
        Assert.NotNull(fixture.Session.CurrentProject);
        Assert.True(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.True(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.False(buildAndRun.CancelBuildCommand.IsEnabled);

        // While a build runs, only Cancel
        var started = builder.NextBuild;
        var building = buildAndRun.BuildAsync(false);
        var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(buildAndRun.BuildInProgress);
        Assert.False(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.False(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.True(buildAndRun.CancelBuildCommand.IsEnabled);
        // A second build does not start
        Assert.Equal(GameBuildOutcome.NotBuilt, await buildAndRun.BuildAsync(true));

        build.Complete(true);
        Assert.Equal(GameBuildOutcome.Succeeded, await building);
        Assert.False(buildAndRun.BuildInProgress);
        Assert.True(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.True(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.False(buildAndRun.CancelBuildCommand.IsEnabled);
        Assert.Single(builder.Requests);

        buildAndRun.Session = null;
        Assert.False(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.False(buildAndRun.StartProjectCommand.IsEnabled);
    }

    [Fact]
    public async Task BuildSavesTheSessionThenBuildsTheCurrentProject()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type, ("Textures/Wood", "wood"));
        var (builder, launcher) = RegisterFakes(fixture);
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };
        Edit(fixture, fixture.Session.AllAssets.Single(), "oak");

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        // Saved first, as in WPF
        Assert.Equal("oak", fixture.ReadAsset("Textures/Wood").Value);
        Assert.False(fixture.Session.HasUnsavedAssets());
        var request = Assert.Single(builder.Requests);
        Assert.Equal(fixture.Session.CurrentProject!.ProjectPath.ToOSPath(), request.ProjectPath);
        Assert.Equal("Build", request.Target);
        Assert.Equal("Debug", request.Configuration);
        Assert.Equal("true", request.Properties["StrideBuildEngineLogVerbose"]);
        // A package opened without its solution
        Assert.False(request.Properties.ContainsKey("SolutionPath"));
        // Built, not started
        Assert.Empty(launcher.Games);
        Assert.Contains($"Build of {ProjectName} succeeded.", Messages(buildAndRun));
        Assert.True(buildAndRun.IsOutputVisible);
        Assert.Equal("Build successful", status.CurrentStatus);
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Theory]
    [InlineData(PlatformType.Windows, "AnyCPU", "Any CPU")]
    [InlineData(PlatformType.Linux, "Linux", "Linux")]
    [InlineData(PlatformType.macOS, "macOS", "macOS")]
    public async Task TheBuildSetsThePlatformOfTheWpfEditor(PlatformType platform, string msbuildPlatform, string solutionPlatform)
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(platform);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Equal(platform, fixture.Session.CurrentProject!.Platform);

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        var request = Assert.Single(builder.Requests);
        Assert.Equal(msbuildPlatform, request.Platform);
        Assert.Equal(solutionPlatform, request.Properties["SolutionPlatform"]);
    }

    [Theory]
    [InlineData(PlatformType.Android)]
    [InlineData(PlatformType.iOS)]
    public async Task AMobileGameIsNotBuilt(PlatformType platform)
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(platform);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(true));

        Assert.Empty(builder.Requests);
        Assert.Contains($"Platform {platform} isn't supported for execution.", Messages(buildAndRun));
    }

    [Fact]
    public async Task StartBuildsThenStartsTheGameAndReportsItsExit()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.True(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));

        var game = Assert.Single(launcher.Games);
        Assert.Equal(builder.AssemblyPath, game.AssemblyPath);
        Assert.Contains($"{ProjectName} started (process {game.Id}).", Messages(buildAndRun));

        // The output of the game shows under its name
        game.Output.Info("Hello from the game");
        var output = await WaitForMessageAsync(buildAndRun, "Hello from the game");
        Assert.Equal(ProjectName, output.Module);

        game.Exit(3);
        var exit = await WaitForMessageAsync(buildAndRun, $"{ProjectName} exited with code 3.");
        Assert.Equal(LogMessageType.Error, exit.Type);
    }

    [Fact]
    public async Task TheOutputOfARunningGameStaysAcrossBuilds()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));
        var game = Assert.Single(launcher.Games);

        // The next build clears the log of the previous one
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));
        Assert.DoesNotContain($"{ProjectName} started (process {game.Id}).", Messages(buildAndRun));

        game.Output.Info("Still running");
        await WaitForMessageAsync(buildAndRun, "Still running");
        game.Exit(0);
        var exit = await WaitForMessageAsync(buildAndRun, $"{ProjectName} exited with code 0.");
        Assert.Equal(LogMessageType.Info, exit.Type);
    }

    [Fact]
    public async Task AGameOfAnotherPlatformIsBuiltButNotStarted()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(OtherPlatform);
        var (builder, launcher) = RegisterFakes(fixture);
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Single(builder.Requests);
        Assert.Empty(launcher.Games);
        Assert.Contains($"{ProjectName} is a {OtherPlatform} game: this editor can only start the games of its own platform ({Platform.Type}).", Messages(buildAndRun));
        Assert.Equal("Build successful", status.CurrentStatus);
    }

    [Fact]
    public async Task AGameThatCannotStartIsReported()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        launcher.FailToStart = true;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Contains($"Unable to start {builder.AssemblyPath}.", Messages(buildAndRun));
    }

    [Fact]
    public async Task AFailedBuildSaysSoAndStartsNothing()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.Succeed = false;
        builder.Errors.Add("Program.cs(3,14): CS1002: ; expected");
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(true));

        var messages = Messages(buildAndRun);
        Assert.Contains("Program.cs(3,14): CS1002: ; expected", messages);
        Assert.Contains($"Build of {ProjectName} failed with 1 error(s).", messages);
        Assert.Empty(launcher.Games);
        Assert.Equal("Build failed", status.CurrentStatus);
    }

    [Fact]
    public async Task ABuildThatFailsWithoutAnErrorSaysSo()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        builder.Succeed = false;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(false));

        Assert.Contains($"Build of {ProjectName} failed.", Messages(buildAndRun));
    }

    [Fact]
    public async Task CancelStopsTheBuild()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.CompleteAtOnce = false;
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        var started = builder.NextBuild;
        var building = buildAndRun.BuildAsync(true);
        var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
        buildAndRun.CancelBuildCommand.Execute();

        Assert.True(build.IsCanceled);
        Assert.Equal(GameBuildOutcome.Canceled, await building);
        Assert.Empty(launcher.Games);
        Assert.Contains($"Build of {ProjectName} cancelled.", Messages(buildAndRun));
        Assert.Equal("Build cancelled", status.CurrentStatus);
        Assert.False(buildAndRun.CancelBuildCommand.IsEnabled);
    }

    [Fact]
    public async Task ALibraryIsNotBuilt()
    {
        // The project of the package is a library
        using var fixture = await EditorSessionFixture.OpenAsync();
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Equal(ProjectType.Library, fixture.Session.CurrentProject!.Type);

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(false));

        Assert.Empty(builder.Requests);
        Assert.Contains($"The current project {ProjectName} is a library: set the executable project of a platform as the current project in the solution explorer.", Messages(buildAndRun));
    }

    [Fact]
    public async Task AFailedSaveStopsTheBuild()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type, ("Textures/Wood", "wood"));
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        var wood = fixture.Session.AllAssets.Single();
        Edit(fixture, wood, "oak");
        // The file of the asset cannot be written: a folder took its place
        var path = wood.AssetItem.FullPath.ToOSPath();
        File.Delete(path);
        Directory.CreateDirectory(path);

        Assert.Equal(GameBuildOutcome.NotBuilt, await buildAndRun.BuildAsync(true));

        Assert.Empty(builder.Requests);
        Assert.Equal(["To build, save the project first."], fixture.Dialogs.Messages);
        Assert.False(buildAndRun.BuildInProgress);
    }

    [Fact]
    public async Task AProjectThatCannotBeLoadedIsReported()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        builder.FailToLoad = true;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(false));

        Assert.Contains($"Unable to load and compile project {fixture.Session.CurrentProject!.ProjectPath}", Messages(buildAndRun));
    }

    [Fact]
    public async Task TheOutputTitleShowsMessagesThatWereNotSeen()
    {
        using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.False(buildAndRun.IsOutputVisible);
        Assert.Equal("Output", buildAndRun.OutputTitle);

        // A build shows the Output pane
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));
        Assert.True(buildAndRun.IsOutputVisible);
        Messages(buildAndRun);
        Assert.Equal("Output", buildAndRun.OutputTitle);

        // Another pane is shown: the next messages mark the title
        buildAndRun.IsOutputVisible = false;
        launcher.Games.Single().Output.Info("Hello");
        await WaitForMessageAsync(buildAndRun, "Hello");
        Assert.Equal("Output*", buildAndRun.OutputTitle);

        buildAndRun.IsOutputVisible = true;
        Assert.Equal("Output", buildAndRun.OutputTitle);
    }

    private static (FakeGameProjectBuilder Builder, FakeGameLauncher Launcher) RegisterFakes(EditorSessionFixture fixture)
    {
        var builder = new FakeGameProjectBuilder();
        var launcher = new FakeGameLauncher();
        fixture.ServiceProvider.RegisterService(builder);
        fixture.ServiceProvider.RegisterService(launcher);
        return (builder, launcher);
    }

    /// <summary>
    /// The text of the messages of the Output pane, debug and verbose ones included.
    /// </summary>
    private static List<string> Messages(BuildAndRunViewModel buildAndRun)
    {
        buildAndRun.BuildLog.Flush();
        return buildAndRun.BuildLog.Messages.Select(x => x.Text).ToList();
    }

    private static async Task<ILogMessage> WaitForMessageAsync(BuildAndRunViewModel buildAndRun, string text)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (true)
        {
            buildAndRun.BuildLog.Flush();
            var message = buildAndRun.BuildLog.Messages.FirstOrDefault(x => x.Text == text);
            if (message is not null)
                return message;
            if (DateTime.UtcNow > timeout)
                throw new TimeoutException($"No message \"{text}\" in the Output pane: {string.Join(" | ", buildAndRun.BuildLog.Messages.Select(x => x.Text))}");
            await Task.Delay(20);
        }
    }

    private static void Edit(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update(value);
        undoRedo.SetName(transaction, "Edit " + value);
    }
}
