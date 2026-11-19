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
/// The deploy steps that Game Studio registers on the shared build and run: an Android game goes to a device with adb (the shared
/// deploy step, tested in Stride.Core.Assets.Editor.Tests), a Linux or macOS game to a remote host.
/// </summary>
public class TestGameDeployers
{
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
    public async Task ARemoteBuildIsBuiltAsItIs()
    {
        var serviceProvider = new ViewModelServiceProvider([new DispatcherService(Dispatcher.CurrentDispatcher)]);
        var request = new GameProjectBuildRequest("MyGame.Linux.csproj", "Linux", new Dictionary<string, string> { ["SolutionPlatform"] = "Linux" });

        var prepared = await new RemoteGameDeployer(serviceProvider).PrepareBuildAsync(null!, request, true, new LoggerResult(), CancellationToken.None);

        Assert.Same(request, prepared);
    }
}
