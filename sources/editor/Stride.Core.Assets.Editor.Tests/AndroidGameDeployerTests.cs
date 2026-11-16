// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The deploy step of the Android games: the build installs the game on the first device that adb lists, then adb starts it on
/// that device. adb is a fake.
/// </summary>
public sealed class AndroidGameDeployerTests
{
    private const string ProjectName = EditorSessionFixture.PackageName;
    private const string PackageName = "com.stride.testgame";

    [Fact]
    public async Task AnAndroidBuildInstallsTheGameOnTheFirstDevice()
    {
        var deployer = new AndroidGameDeployer(() => ["emulator-5554", "R58M123"], () => "adb", (_, _) => (0, []));
        var request = new GameProjectBuildRequest("MyGame.Android.csproj", "Android", new Dictionary<string, string> { ["SolutionPlatform"] = "Android" });
        var logger = new LoggerResult();

        var prepared = await deployer.PrepareBuildAsync(null!, request, false, logger, CancellationToken.None);

        Assert.NotNull(prepared);
        Assert.Equal("GetAndroidPackage;Install", prepared.Target);
        Assert.Equal("-s emulator-5554", prepared.Properties["AdbTarget"]);
        Assert.Equal("Android", prepared.Properties["SolutionPlatform"]);
        Assert.Equal("Android", prepared.Platform);
        Assert.False(logger.HasErrors);
    }

    [Fact]
    public async Task AnAndroidBuildWithoutADeviceDoesNotStart()
    {
        var deployer = new AndroidGameDeployer(() => [], () => "adb", (_, _) => (0, []));
        var request = new GameProjectBuildRequest("MyGame.Android.csproj", "Android", new Dictionary<string, string>());
        var logger = new LoggerResult();

        Assert.Null(await deployer.PrepareBuildAsync(null!, request, true, logger, CancellationToken.None));

        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text == "No Android device found for execution.");
    }

    [Fact]
    public async Task AnAndroidGameStartsWithAdbOnTheDeviceItWasInstalledOn()
    {
        var adb = new FakeAdb("R58M123");
        var deployer = (AndroidGameDeployer)adb.CreateDeployer();
        var logger = new LoggerResult();
        await deployer.PrepareBuildAsync(null!, new GameProjectBuildRequest("MyGame.Android.csproj", "Android", new Dictionary<string, string>()), true, logger, CancellationToken.None);

        // The package comes from the GetAndroidPackage target of the build
        Assert.True(await deployer.DeployAsync(null!, new FakeAndroidBuild(PackageName), logger, CancellationToken.None));

        var (path, arguments) = Assert.Single(adb.Runs);
        Assert.Equal(FakeAdb.Path, path);
        Assert.Equal($"-s R58M123 shell monkey -p {PackageName} -c android.intent.category.LAUNCHER 1", arguments);
        Assert.False(logger.HasErrors);
    }

    [Fact]
    public async Task AnAndroidGameThatAdbCannotStartIsReported()
    {
        var logger = new LoggerResult();

        // No adb
        var deployer = new AndroidGameDeployer(() => ["R58M123"], () => null, (_, _) => throw new InvalidOperationException("adb is not there"));
        Assert.False(await deployer.DeployAsync(null!, new FakeAndroidBuild(PackageName), logger, CancellationToken.None));
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.StartsWith("Android tool \"adb\" couldn't found"));

        // adb fails
        deployer = new AndroidGameDeployer(() => ["R58M123"], () => "adb", (_, _) => (1, ["error: device offline"]));
        Assert.False(await deployer.DeployAsync(null!, new FakeAndroidBuild(PackageName), logger, CancellationToken.None));
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text == "Can't run Android app with adb: error: device offline");
    }

    [Fact]
    public async Task AnAndroidBuildThatGaveNoPackageIsNotStarted()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        RegisterFakes(fixture);
        var adb = new FakeAdb("R58M123");
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());

        Assert.Equal(GameBuildOutcome.StartFailed, await buildAndRun.BuildAsync(true));

        Assert.Empty(adb.Runs);
        Assert.Contains($"Couldn't find Android package name for {ProjectName}.", Messages(buildAndRun));
    }

    [Fact]
    public async Task StartProjectWithoutAnAndroidDeviceDoesNotBuild()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        var adb = new FakeAdb();
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());

        Assert.Equal(GameBuildOutcome.Failed, await buildAndRun.BuildAsync(true));

        Assert.Empty(builder.Requests);
        Assert.Empty(adb.Runs);
        Assert.Empty(launcher.Games);
        Assert.Contains("No Android device found for execution.", Messages(buildAndRun));
    }

    [Fact]
    public async Task StartProjectInstallsTheGameAndStartsItOnTheSameDevice()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.TargetOutputs[AndroidGameDeployer.PackageTarget] = [PackageName];
        var adb = new FakeAdb("emulator-5554", "R58M123");
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(true));

        // The build installs the game on the first device
        var request = Assert.Single(builder.Requests);
        Assert.Equal("GetAndroidPackage;Install", request.Target);
        Assert.Equal("-s emulator-5554", request.Properties["AdbTarget"]);
        // adb starts it there; nothing starts on this machine
        var (_, arguments) = Assert.Single(adb.Runs);
        Assert.Equal($"-s emulator-5554 shell monkey -p {PackageName} -c android.intent.category.LAUNCHER 1", arguments);
        Assert.Empty(launcher.Games);
        Assert.Contains($"Deployment of {ProjectName} successful.", Messages(buildAndRun));
    }

    [Fact]
    public async Task BuildProjectInstallsTheGameWithoutStartingIt()
    {
        await using var fixture = await EditorSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = RegisterFakes(fixture);
        builder.TargetOutputs[AndroidGameDeployer.PackageTarget] = [PackageName];
        var adb = new FakeAdb("R58M123");
        var buildAndRun = new BuildAndRunViewModel(fixture.ServiceProvider) { Session = fixture.Session };
        buildAndRun.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());

        Assert.Equal(GameBuildOutcome.Succeeded, await buildAndRun.BuildAsync(false));

        var request = Assert.Single(builder.Requests);
        Assert.Equal("GetAndroidPackage;Install", request.Target);
        Assert.Equal("-s R58M123", request.Properties["AdbTarget"]);
        Assert.Empty(adb.Runs);
        Assert.Empty(launcher.Games);
    }

    private static (FakeGameProjectBuilder Builder, FakeGameLauncher Launcher) RegisterFakes(EditorSessionFixture fixture)
    {
        var builder = new FakeGameProjectBuilder();
        var launcher = new FakeGameLauncher();
        fixture.ServiceProvider.RegisterService(builder);
        fixture.ServiceProvider.RegisterService(launcher);
        return (builder, launcher);
    }

    private static List<string> Messages(BuildAndRunViewModel buildAndRun)
    {
        buildAndRun.BuildLog.Flush();
        return buildAndRun.BuildLog.Messages.Select(x => x.Text).ToList();
    }

    /// <summary>
    /// A build of an Android game that gave the name of its package.
    /// </summary>
    private sealed class FakeAndroidBuild(string packageName) : IGameProjectBuild
    {
        public string AssemblyPath => "MyGame.Android.dll";

        public Task<bool> Completion => Task.FromResult(true);

        public bool IsCanceled => false;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> TargetOutputs { get; } = new Dictionary<string, IReadOnlyList<string>>
        {
            [AndroidGameDeployer.PackageTarget] = [packageName],
        };

        public void Cancel()
        {
        }
    }
}
