// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Threading;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Deployment;
using Xunit;

#nullable enable

namespace Stride.GameStudio.Tests;

/// <summary>
/// The deploy steps that Game Studio registers on the shared build and run: an Android game goes to a device with adb, a Linux or
/// macOS game to a remote host. adb is replaced by fakes.
/// </summary>
public class TestGameDeployers
{
    private const string PackageName = "com.stride.testgame";

    [Fact]
    public void GameStudioRegistersTheAndroidAndRemoteDeploySteps()
    {
        var serviceProvider = new ViewModelServiceProvider([new DispatcherService(Dispatcher.CurrentDispatcher)]);
        var buildAndRun = new BuildAndRunViewModel(serviceProvider);
        try
        {
            GameDeployers.Register(buildAndRun, serviceProvider);

            Assert.IsType<AndroidGameDeployer>(buildAndRun.Deployers[PlatformType.Android]);
            Assert.IsType<RemoteGameDeployer>(buildAndRun.Deployers[PlatformType.Linux]);
            Assert.Same(buildAndRun.Deployers[PlatformType.Linux], buildAndRun.Deployers[PlatformType.macOS]);
            // A Windows game starts on this machine, an iOS game is only built
            Assert.False(buildAndRun.Deployers.ContainsKey(PlatformType.Windows));
            Assert.False(buildAndRun.Deployers.ContainsKey(PlatformType.iOS));
        }
        finally
        {
            buildAndRun.Destroy();
        }
    }

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
        var runs = new List<(string Adb, string Arguments)>();
        var deployer = new AndroidGameDeployer(() => ["R58M123"], () => "/sdk/platform-tools/adb", (adb, arguments) =>
        {
            runs.Add((adb, arguments));
            return (0, []);
        });
        var logger = new LoggerResult();
        await deployer.PrepareBuildAsync(null!, new GameProjectBuildRequest("MyGame.Android.csproj", "Android", new Dictionary<string, string>()), true, logger, CancellationToken.None);

        // The package comes from the GetAndroidPackage target of the build
        Assert.True(await deployer.DeployAsync(null!, new FakeBuild(PackageName), logger, CancellationToken.None));

        var (adb, arguments) = Assert.Single(runs);
        Assert.Equal("/sdk/platform-tools/adb", adb);
        Assert.Equal($"-s R58M123 shell monkey -p {PackageName} -c android.intent.category.LAUNCHER 1", arguments);
        Assert.False(logger.HasErrors);
    }

    [Fact]
    public async Task AnAndroidGameThatAdbCannotStartIsReported()
    {
        var logger = new LoggerResult();

        // No adb
        var deployer = new AndroidGameDeployer(() => ["R58M123"], () => null, (_, _) => throw new InvalidOperationException("adb is not there"));
        Assert.False(await deployer.DeployAsync(null!, new FakeBuild(PackageName), logger, CancellationToken.None));
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.StartsWith("Android tool \"adb\" couldn't found"));

        // adb fails
        deployer = new AndroidGameDeployer(() => ["R58M123"], () => "adb", (_, _) => (1, ["error: device offline"]));
        Assert.False(await deployer.DeployAsync(null!, new FakeBuild(PackageName), logger, CancellationToken.None));
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text == "Can't run Android app with adb: error: device offline");
    }

    [Fact]
    public async Task ARemoteBuildIsBuiltAsItIs()
    {
        var serviceProvider = new ViewModelServiceProvider([new DispatcherService(Dispatcher.CurrentDispatcher)]);
        var request = new GameProjectBuildRequest("MyGame.Linux.csproj", "Linux", new Dictionary<string, string> { ["SolutionPlatform"] = "Linux" });

        var prepared = await new RemoteGameDeployer(serviceProvider).PrepareBuildAsync(null!, request, true, new LoggerResult(), CancellationToken.None);

        Assert.Same(request, prepared);
    }

    /// <summary>
    /// A build of an Android game that gave the name of its package.
    /// </summary>
    private sealed class FakeBuild(string packageName) : IGameProjectBuild
    {
        public string AssemblyPath => "MyGame.Android.dll";

        public Task<bool> Completion => Task.FromResult(true);

        public bool IsCanceled => false;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> TargetOutputs { get; } = new Dictionary<string, IReadOnlyList<string>>
        {
            ["GetAndroidPackage"] = [packageName],
        };

        public void Cancel()
        {
        }
    }
}
