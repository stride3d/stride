// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Assets.Presentation.ViewModels;

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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
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
        var build = await WaitForBuildStartAsync(fixture, builder, buildAndRun, started, building);
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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type, ("Textures/Wood", "wood"));
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

    [Fact]
    public async Task ABuildWhoseViewModelIsDestroyedWhileTheSessionSavesDoesNotStart()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type, ("Textures/Wood", "wood"));
        var (builder, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Edit(fixture, fixture.Session.AllAssets.Single(), "oak");

        // The session closes while the build saves it
        var building = buildAndRun.BuildAsync(true);
        Assert.False(building.IsCompleted);
        buildAndRun.Destroy();

        Assert.Equal(GameBuildOutcome.NotBuilt, await building);
        Assert.Empty(builder.Requests);
        Assert.Empty(launcher.Games);
        Assert.False(buildAndRun.BuildInProgress);
    }

    [Theory]
    [InlineData(PlatformType.Windows, "AnyCPU", "Any CPU")]
    [InlineData(PlatformType.Linux, "Linux", "Linux")]
    [InlineData(PlatformType.macOS, "macOS", "macOS")]
    public async Task TheBuildSetsThePlatformOfTheWpfEditor(PlatformType platform, string msbuildPlatform, string solutionPlatform)
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(platform);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Equal(platform, fixture.Session.CurrentProject!.Platform);

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        var request = Assert.Single(builder.Requests);
        Assert.Equal(msbuildPlatform, request.Platform);
        Assert.Equal(solutionPlatform, request.Properties["SolutionPlatform"]);
    }

    [Theory]
    [InlineData(PlatformType.Android, "Android", "Android")]
    [InlineData(PlatformType.iOS, "iPhone", "iPhone")]
    public async Task AMobileGameIsBuiltForItsPlatform(PlatformType platform, string msbuildPlatform, string solutionPlatform)
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(platform);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        var request = Assert.Single(builder.Requests);
        Assert.Equal(msbuildPlatform, request.Platform);
        Assert.Equal(solutionPlatform, request.Properties["SolutionPlatform"]);
        Assert.Equal("Build", request.Target);
    }

    [Fact]
    public async Task AnAndroidGameWithoutADeployStepIsBuiltButNotStarted()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Empty(buildAndRun.Deployers);

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Single(builder.Requests);
        Assert.Empty(launcher.Games);
        Assert.Contains($"{ProjectName} is a {PlatformType.Android} game: this editor can only start the games of its own platform ({Platform.Type}).", Messages(buildAndRun));
    }

    [Fact]
    public async Task AnIosGameIsBuiltButNotStarted()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.iOS);
        var (builder, launcher) = RegisterFakes(fixture);
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));

        Assert.Single(builder.Requests);
        Assert.Empty(launcher.Games);
        Messages(buildAndRun);
        var message = Assert.Single(buildAndRun.BuildLog.Messages, x => x.Text.Contains("doesn't deploy iOS games"));
        Assert.Equal(LogMessageType.Warning, message.Type);
        Assert.Equal($"{ProjectName} was built: the editor doesn't deploy iOS games, start it from your IDE.", message.Text);
        Assert.Equal("Build successful", status.CurrentStatus);
    }

    [Fact]
    public async Task TheDeployStepOfThePlatformPreparesTheBuildThenDeploysTheGame()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.TargetOutputs["GetAndroidPackage"] = ["com.stride.testgame"];
        var deployer = new FakeGameDeployer();
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);
        Assert.Same(deployer, buildAndRun.Deployers[PlatformType.Android]);

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));

        // Prepared from the build of the platform, for a start
        var (preparedRequest, startProject) = Assert.Single(deployer.Preparations);
        Assert.True(startProject);
        Assert.Equal("Build", preparedRequest.Target);
        Assert.Equal("Android", preparedRequest.Platform);
        // The build is the one the deploy step asked for
        var request = Assert.Single(builder.Requests);
        Assert.Equal("GetAndroidPackage;Install", request.Target);
        Assert.Equal(FakeGameDeployer.Device, request.Properties["AdbTarget"]);
        Assert.Equal("Android", request.Properties["SolutionPlatform"]);
        // The deploy step gets the outputs of the targets
        var build = Assert.Single(deployer.Deployments);
        Assert.Equal(["com.stride.testgame"], build.TargetOutputs["GetAndroidPackage"]);
        // Nothing starts on this machine
        Assert.Empty(launcher.Games);
        Assert.Contains($"Deployment of {ProjectName} successful.", Messages(buildAndRun));
    }

    [Fact]
    public async Task BuildPreparesWithTheDeployStepButDoesNotDeploy()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, _) = RegisterFakes(fixture);
        var deployer = new FakeGameDeployer();
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        var (_, startProject) = Assert.Single(deployer.Preparations);
        Assert.False(startProject);
        Assert.Equal("GetAndroidPackage;Install", Assert.Single(builder.Requests).Target);
        Assert.Empty(deployer.Deployments);
    }

    [Fact]
    public async Task ADeployStepThatCannotPrepareStopsTheBuild()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, _) = RegisterFakes(fixture);
        var deployer = new FakeGameDeployer { FailToPrepare = true };
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(true));

        Assert.Empty(builder.Requests);
        Assert.Empty(deployer.Deployments);
        Assert.Contains("No device found.", Messages(buildAndRun));
    }

    [Fact]
    public async Task AFailedDeploymentIsReported()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        RegisterFakes(fixture);
        var deployer = new FakeGameDeployer { Deploy = false };
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        var messages = Messages(buildAndRun);
        Assert.Contains("The device refused the game.", messages);
        Assert.DoesNotContain($"Deployment of {ProjectName} successful.", messages);
        // The build itself succeeded, the game did not start
        Assert.Equal("Build successful, but the game did not start", status.CurrentStatus);

        // A deploy step that throws
        deployer.Deploy = true;
        deployer.DeployException = new IOException("The connection was lost.");
        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));
        Messages(buildAndRun);
        var error = Assert.Single(buildAndRun.BuildLog.Messages, x => x.Text == "An exception occurred during the deployment.");
        Assert.Equal(LogMessageType.Error, error.Type);
        Assert.False(buildAndRun.BuildInProgress);
    }

    [Fact]
    public async Task ADeployStepOfTheEditorsPlatformReplacesTheLocalStart()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = RegisterFakes(fixture);
        var deployer = new FakeGameDeployer();
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(Platform.Type, deployer);

        var result = await buildAndRun.RunAsync(true);

        Assert.Equal(GameBuildOutcome.Succeeded, result.Outcome);
        Assert.Null(result.Game);
        Assert.Single(deployer.Deployments);
        Assert.Empty(launcher.Games);
    }

    [Fact]
    public async Task CancelStopsADeployment()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        RegisterFakes(fixture);
        var deployer = new FakeGameDeployer { WaitForCancel = true };
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);

        var building = buildAndRun.BuildAsync(true);
        await deployer.DeploymentStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(buildAndRun.BuildInProgress);
        Assert.True(buildAndRun.CancelBuildCommand.IsEnabled);
        buildAndRun.CancelBuildCommand.Execute();

        Assert.Equal(GameBuildOutcome.Canceled, await building.WaitAsync(TimeSpan.FromSeconds(30)));
        var messages = Messages(buildAndRun);
        Assert.Contains($"Build of {ProjectName} cancelled.", messages);
        Assert.DoesNotContain($"Deployment of {ProjectName} successful.", messages);
        Assert.DoesNotContain("An exception occurred during the deployment.", messages);
        Assert.Equal("Build cancelled", status.CurrentStatus);
        Assert.False(buildAndRun.BuildInProgress);
    }

    [Fact]
    public async Task AStartOfItsOwnReplacesTheDeployStep()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        var deployer = new FakeGameDeployer();
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, deployer);
        ProjectViewModel? startedProject = null;

        var result = await buildAndRun.RunAsync(true, (project, logger, _) =>
        {
            startedProject = project;
            logger.Info("Started in debug mode.");
            return Task.FromResult(true);
        });

        Assert.Equal(GameBuildOutcome.Succeeded, result.Outcome);
        Assert.Same(fixture.Session.CurrentProject, startedProject);
        // Built as the platform builds without its deploy step
        Assert.Equal("Build", Assert.Single(builder.Requests).Target);
        Assert.Empty(deployer.Preparations);
        Assert.Empty(deployer.Deployments);
        Assert.Empty(launcher.Games);
        Assert.Contains("Started in debug mode.", Messages(buildAndRun));

        // A start that fails
        Assert.Equal(GameBuildOutcome.StartFailed, (await buildAndRun.RunAsync(true, (_, _, _) => Task.FromResult(false))).Outcome);
    }

    [Fact]
    public async Task RunGivesTheGameItStarted()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        var result = await buildAndRun.RunAsync(true);

        Assert.Equal(GameBuildOutcome.Succeeded, result.Outcome);
        var game = Assert.Single(launcher.Games);
        Assert.Same(game, result.Game);
        game.Exit(0);
        await WaitForMessageAsync(buildAndRun, $"{ProjectName} exited with code 0.");

        // Built only
        Assert.Null((await buildAndRun.RunAsync(false)).Game);
    }

    [Fact]
    public async Task TheBuildOfTheGameCodeGivesItsAssemblies()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        var projects = new[] { Path.Combine(Path.GetTempPath(), "Game", "Game.csproj"), Path.Combine(Path.GetTempPath(), "Scripts", "Scripts.csproj") };

        var result = await buildAndRun.BuildCodeAssembliesAsync(projects);

        Assert.Equal(GameBuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(projects, builder.Requests.Select(x => x.ProjectPath));
        Assert.Equal(projects, result.AssemblyPaths.Keys.Order());
        Assert.All(result.AssemblyPaths.Values, x => Assert.Equal(builder.AssemblyPath, x));

        // A failure gives what was built before it
        builder.Succeed = false;
        result = await buildAndRun.BuildCodeAssembliesAsync(projects);
        Assert.Equal(GameBuildOutcome.Failed, result.Outcome);
        Assert.Empty(result.AssemblyPaths);
    }

    [Fact]
    public async Task WhileBuildsAreHeldOnlyTheBuildsOfTheGameCodeRun()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        var project = Path.Combine(Path.GetTempPath(), "Game", "Game.csproj");

        var hold = buildAndRun.HoldBuilds();
        Assert.True(buildAndRun.BuildsHeld);
        Assert.False(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.False(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.Equal(GameBuildOutcome.NotBuilt, await buildAndRun.BuildAsync(true));
        Assert.Empty(builder.Requests);

        // A reload builds its projects one after the other
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildCodeAsync([project]));
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildCodeAsync([project]));
        Assert.Equal(2, builder.Requests.Count);
        Assert.False(buildAndRun.BuildProjectCommand.IsEnabled);

        hold.Dispose();
        hold.Dispose();
        Assert.False(buildAndRun.BuildsHeld);
        Assert.True(buildAndRun.BuildProjectCommand.IsEnabled);
        Assert.True(buildAndRun.StartProjectCommand.IsEnabled);
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));
        Assert.Equal(3, builder.Requests.Count);
    }

    [Fact]
    public async Task StartBuildsThenStartsTheGameAndReportsItsExit()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
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
    public async Task TheOutputPaneKeepsOnlyTheLastMessagesOfAGameThatLogsALot()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = RegisterFakes(fixture);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));
        var game = Assert.Single(launcher.Games);

        // A game that logs every frame
        var lineCount = BuildAndRunViewModel.MaximumOutputMessages * 2;
        for (var i = 0; i < lineCount; ++i)
            game.Output.Info($"Frame {i}");
        await WaitForMessageAsync(buildAndRun, $"Frame {lineCount - 1}");

        var messages = Messages(buildAndRun);
        Assert.InRange(messages.Count, BuildAndRunViewModel.MaximumOutputMessages, BuildAndRunViewModel.MaximumOutputMessages * 11 / 10 + 1);
        Assert.StartsWith("Older messages were removed", messages[0]);
        Assert.Equal($"Frame {lineCount - 1}", messages[^1]);
        Assert.DoesNotContain("Frame 0", messages);

        game.Exit(0);
        await WaitForMessageAsync(buildAndRun, $"{ProjectName} exited with code 0.");
    }

    [Fact]
    public async Task TheOutputOfARunningGameStaysAcrossBuilds()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(OtherPlatform);
        var (builder, launcher) = RegisterFakes(fixture);
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Single(builder.Requests);
        Assert.Empty(launcher.Games);
        Assert.Contains($"{ProjectName} is a {OtherPlatform} game: this editor can only start the games of its own platform ({Platform.Type}).", Messages(buildAndRun));
        Assert.Equal("Build successful, but the game did not start", status.CurrentStatus);
    }

    [Fact]
    public async Task AGameThatCannotStartIsReported()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        launcher.FailToStart = true;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Contains($"Unable to start {builder.AssemblyPath}.", Messages(buildAndRun));
    }

    [Fact]
    public async Task AFailedBuildSaysSoAndStartsNothing()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        builder.Succeed = false;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(false));

        Assert.Contains($"Build of {ProjectName} failed.", Messages(buildAndRun));
    }

    [Fact]
    public async Task CancelStopsTheBuild()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.CompleteAtOnce = false;
        var status = new StatusViewModel(fixture.ServiceProvider);
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider, status) { Session = fixture.Session };

        var started = builder.NextBuild;
        var building = buildAndRun.BuildAsync(true);
        var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
        buildAndRun.CancelBuildCommand.Execute();

        // The cancel can come before the view model takes the started build as the current one: it cancels it then
        Assert.Equal(GameBuildOutcome.Canceled, await building);
        Assert.True(build.IsCanceled);
        Assert.Empty(launcher.Games);
        Assert.Contains($"Build of {ProjectName} cancelled.", Messages(buildAndRun));
        Assert.Equal("Build cancelled", status.CurrentStatus);
        Assert.False(buildAndRun.CancelBuildCommand.IsEnabled);
    }

    [Fact]
    public async Task ALibraryIsNotBuilt()
    {
        // The project of the package is a library
        await using var fixture = await EditorSessionFixture.OpenAsync();
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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type, ("Textures/Wood", "wood"));
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
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = RegisterFakes(fixture);
        builder.FailToLoad = true;
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(false));

        // The message names the project file as the build request does, with the separators of the OS
        Assert.Contains($"Unable to load and compile project {fixture.Session.CurrentProject!.ProjectPath.ToOSPath()}", Messages(buildAndRun));
    }

    [Fact]
    public async Task TheOutputTitleShowsMessagesThatWereNotSeen()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(Platform.Type);
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

    /// <summary>
    /// Waits for the build to reach the builder; when it doesn't, fails with where it stopped: a build that ended first gives its
    /// outcome, one still saving or preparing says so, with the state of the thread pool that its steps need.
    /// </summary>
    private static async Task<FakeGameProjectBuild> WaitForBuildStartAsync(EditorSessionFixture fixture, FakeGameProjectBuilder builder, BuildAndRunViewModel buildAndRun, Task<FakeGameProjectBuild> started, Task<GameBuildOutcome> building)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var first = await Task.WhenAny(started, building, Task.Delay(TimeSpan.FromSeconds(30)));
        if (first == started)
            return await started;

        var where = building.IsCompleted
            ? $"the build ended with {(building.IsCompletedSuccessfully ? building.Result.ToString() : building.Status.ToString())} before it reached the builder"
            : fixture.Session.IsSaving ? "the session is still saving" : "the build is still preparing (saved, not started)";
        ThreadPool.GetAvailableThreads(out var availableWorkers, out _);
        Assert.Fail($"After {watch.ElapsedMilliseconds} ms, {where}. Builds asked for: {builder.Requests.Count}. Dialogs: [{string.Join(" | ", fixture.Dialogs.Messages)}]."
            + $" Output: [{string.Join(" | ", Messages(buildAndRun))}]. Session log: [{fixture.Log}]."
            + $" Thread pool: {ThreadPool.ThreadCount} threads, {availableWorkers} available, {ThreadPool.PendingWorkItemCount} pending items.");
        return null!;
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
