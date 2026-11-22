// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Assets.Editor.Panels;
using Stride.GameStudio.Avalonia.Views.Panels;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Building the current project and starting its game from the shell: the Project menu, the toolbar, F6, F5
/// and Ctrl+F6, and the build log in the Output tab. The builder and the game launcher are fakes.
/// </summary>
public sealed class BuildAndRunTests
{
    [AvaloniaFact]
    public async Task TheProjectMenuHasTheBuildCommands()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var build = MenuItem(window, "BuildProjectMenuItem");
            var forceBuild = MenuItem(window, "ForceBuildProjectMenuItem");
            var start = MenuItem(window, "StartProjectMenuItem");
            var cancel = MenuItem(window, "CancelBuildMenuItem");
            Assert.Equal(new KeyGesture(Key.F6), build.InputGesture);
            Assert.Equal(new KeyGesture(Key.F5), start.InputGesture);
            Assert.Equal(new KeyGesture(Key.F6, KeyModifiers.Control), cancel.InputGesture);
            Assert.Equal(GameStudioCommands.BuildProjectId, Assert.IsType<CommandReference>(build.Command).Id);
            Assert.Equal(GameStudioCommands.ForceBuildProjectId, Assert.IsType<CommandReference>(forceBuild.Command).Id);
            Assert.Equal(GameStudioCommands.StartProjectId, Assert.IsType<CommandReference>(start.Command).Id);
            Assert.Equal(GameStudioCommands.CancelBuildId, Assert.IsType<CommandReference>(cancel.Command).Id);

            Assert.True(await IsEnabledInOpenMenuAsync(build));
            Assert.True(await IsEnabledInOpenMenuAsync(forceBuild));
            Assert.True(await IsEnabledInOpenMenuAsync(start));
            Assert.False(await IsEnabledInOpenMenuAsync(cancel));

            // The shortcuts are the commands' shortcuts, which the window runs (a menu item of a closed menu would not)
            var shortcuts = window.View.CommandSlots!.Shortcuts;
            Assert.Contains(shortcuts, x => x.Gesture == new KeyGesture(Key.F6) && x.Descriptor.Id == GameStudioCommands.BuildProjectId);
            Assert.Contains(shortcuts, x => x.Gesture == new KeyGesture(Key.F5) && x.Descriptor.Id == GameStudioCommands.StartProjectId);
            Assert.Contains(shortcuts, x => x.Gesture == new KeyGesture(Key.F6, KeyModifiers.Control) && x.Descriptor.Id == GameStudioCommands.CancelBuildId);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task OnlyTheAndroidGamesHaveADeployStep()
    {
        // The games of the editor's platform start on this machine; the iOS, Linux and macOS ones of another editor are only built
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (platform, deployer) = Assert.Single(fixture.Main.Build.Deployers);
        Assert.Equal(PlatformType.Android, platform);
        Assert.IsType<AndroidGameDeployer>(deployer);
    }

    [AvaloniaFact]
    public async Task F5OnAnAndroidGameWithoutADeviceDoesNotBuild()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = fixture.ReplaceGameBuilder();
        fixture.ReplaceDialogService();
        var adb = new FakeAdb();
        fixture.Main.Build.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            window.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            window.KeyRelease(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            var log = fixture.Main.Build.BuildLog;
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Main.Build.BuildInProgress && log.Messages.Any(x => x.Text == "No Android device found for execution."), "the end of the start");

            Assert.Equal(LogMessageType.Error, log.Messages.Single(x => x.Text == "No Android device found for execution.").Type);
            Assert.Empty(builder.Requests);
            Assert.Empty(adb.Runs);
            Assert.Empty(launcher.Games);
            Assert.Equal("Build failed", fixture.Main.Status.CurrentStatus);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task F5InstallsAnAndroidGameAndStartsItOnTheDeviceAndF6OnlyInstallsIt()
    {
        const string packageName = "com.stride.testgame";
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(PlatformType.Android);
        var (builder, launcher) = fixture.ReplaceGameBuilder();
        builder.TargetOutputs[AndroidGameDeployer.PackageTarget] = [packageName];
        fixture.ReplaceDialogService();
        var adb = new FakeAdb("emulator-5554", "R58M123");
        fixture.Main.Build.RegisterDeployer(PlatformType.Android, adb.CreateDeployer());
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            window.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            window.KeyRelease(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            await WindowTestHelper.WaitUntilAsync(() => adb.Runs.Count == 1 && !fixture.Main.Build.BuildInProgress, "the start of the game on the device");

            // Installed on the first device by the build, and started there
            var request = Assert.Single(builder.Requests);
            Assert.Equal("GetAndroidPackage;Install", request.Target);
            Assert.Equal("-s emulator-5554", request.Properties["AdbTarget"]);
            Assert.Equal($"-s emulator-5554 shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1", adb.Runs[0].Arguments);
            Assert.Empty(launcher.Games);
            Assert.Equal("Build successful", fixture.Main.Status.CurrentStatus);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Main.Build.BuildLog.Messages.Any(x => x.Text == $"Deployment of {ShellSessionFixture.PackageName} successful."), "the log of the deployment");

            // F6 installs the game without starting it
            window.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            window.KeyRelease(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            await WindowTestHelper.WaitUntilAsync(() => builder.Requests.Count == 2 && !fixture.Main.Build.BuildInProgress, "the second build");
            Assert.Equal("GetAndroidPackage;Install", builder.Requests[1].Target);
            Assert.Single(adb.Runs);
            Assert.Empty(launcher.Games);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheToolbarShowsTheBuildButtonsWhileASessionIsOpen()
    {
        using var fixture = ShellSessionFixture.Create();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var toolBar = window.GetVisualDescendants().OfType<Control>().Single(x => x.Name == "MainToolBar");
            Assert.False(toolBar.IsVisible);

            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();

            Assert.True(toolBar.IsVisible);
            Assert.Equal(GameStudioCommands.BuildProjectId, Assert.IsType<CommandReference>(Button(window, "BuildProjectButton").Command).Id);
            Assert.Equal(GameStudioCommands.StartProjectId, Assert.IsType<CommandReference>(Button(window, "StartProjectButton").Command).Id);
            Assert.Equal(GameStudioCommands.CancelBuildId, Assert.IsType<CommandReference>(Button(window, "CancelBuildButton").Command).Id);
            // With their shortcuts
            Assert.Equal("Build the project (F6)", ToolTip.GetTip(Button(window, "BuildProjectButton")));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task F5BuildsTheProjectStartsTheGameAndShowsTheOutput()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = fixture.ReplaceGameBuilder();
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var output = fixture.Main.Panels.FindDockable(BuiltInPanels.BuildLog)!;
            Assert.False(output.ViewModel.IsShown);

            window.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            window.KeyRelease(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            await WindowTestHelper.WaitUntilAsync(() => launcher.Games.Count == 1 && !fixture.Main.Build.BuildInProgress, "the start of the game");

            // Saved first (the progress window of the save), then built and started
            Assert.Single(dialogs.ProgressWindows);
            Assert.Single(builder.Requests);
            Assert.Equal(builder.AssemblyPath, launcher.Games[0].AssemblyPath);
            // The Output comes forward and shows the log of the build
            await WindowTestHelper.WaitUntilAsync(() => output.ViewModel.IsShown, "the Output in front");
            Assert.Equal("Output", output.Title);
            var log = window.GetVisualDescendants().OfType<OutputView>().Single().GetVisualDescendants().OfType<TextLogViewer>().Single();
            await WindowTestHelper.WaitUntilAsync(() => log.LogMessages?.Any(x => x.Text == $"{ShellSessionFixture.PackageName} started (process 1000).") == true, "the log of the start");
            Assert.Contains(log.LogMessages!, x => x.Text == $"Build of {ShellSessionFixture.PackageName} succeeded.");
            Assert.Equal("Build successful", fixture.Main.Status.CurrentStatus);

            // F6 builds without starting
            window.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            window.KeyRelease(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            await WindowTestHelper.WaitUntilAsync(() => builder.Requests.Count == 2 && !fixture.Main.Build.BuildInProgress, "the second build");
            Assert.Single(launcher.Games);

            launcher.Games[0].Exit(0);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CtrlF6CancelsTheBuild()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, launcher) = fixture.ReplaceGameBuilder();
        fixture.ReplaceDialogService();
        builder.CompleteAtOnce = false;
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var started = builder.NextBuild;
            await window.ClickAsync(Button(window, "StartProjectButton"));
            var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
            await WindowTestHelper.PumpAsync();
            Assert.True(Button(window, "CancelBuildButton").IsEffectivelyEnabled);
            Assert.False(Button(window, "BuildProjectButton").IsEffectivelyEnabled);

            window.KeyPress(Key.F6, RawInputModifiers.Control, PhysicalKey.F6, null);
            window.KeyRelease(Key.F6, RawInputModifiers.Control, PhysicalKey.F6, null);
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Main.Build.BuildInProgress, "the end of the build");

            Assert.True(build.IsCanceled);
            Assert.Empty(launcher.Games);
            Assert.Equal("Build cancelled", fixture.Main.Status.CurrentStatus);
            Assert.False(Button(window, "CancelBuildButton").IsEffectivelyEnabled);
            Assert.True(Button(window, "BuildProjectButton").IsEffectivelyEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheOutputTitleMarksMessagesThatWereNotSeen()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (_, launcher) = fixture.ReplaceGameBuilder();
        fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            fixture.Main.Build.StartProjectCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => launcher.Games.Count == 1 && !fixture.Main.Build.BuildInProgress, "the start of the game");
            var output = fixture.Main.Panels.FindDockable(BuiltInPanels.BuildLog)!;
            await WindowTestHelper.WaitUntilAsync(() => output.ViewModel.IsShown, "the Output in front");

            // The user selects another tab of the Output's group; the game writes to its console
            fixture.Main.Panels.Show(BuiltInPanels.References);
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Main.Build.IsOutputVisible, "the Output behind");
            launcher.Games.Single().Output.Info("Hello from the game");
            await WindowTestHelper.WaitUntilAsync(() => output.Title == "Output*", "the mark of the Output tab", 10000);

            fixture.Main.Panels.Show(BuiltInPanels.BuildLog);
            await WindowTestHelper.WaitUntilAsync(() => output.Title == "Output", "the mark gone");
            launcher.Games.Single().Exit(0);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheOutputFollowsTheEndOfTheLogUnlessTheUserScrolledUp()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = fixture.ReplaceGameBuilder();
        fixture.ReplaceDialogService();
        builder.CompleteAtOnce = false;
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var started = builder.NextBuild;
            fixture.Main.Build.BuildProjectCommand.Execute();
            var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
            var log = fixture.Main.Build.BuildLog.Messages;
            for (var i = 0; i < 200; ++i)
                build.Logger.Info($"Line {i}");
            // The lines reach the log after its update interval: until then the empty viewer is "at its end" too
            await WindowTestHelper.WaitUntilAsync(() => log.Any(x => x.Text == "Line 199"), "the lines in the log");
            var viewer = window.GetVisualDescendants().OfType<ScrollViewer>().Single(x => x.Name == "PART_LogScrollViewer");
            await WindowTestHelper.WaitUntilAsync(() => viewer.Viewport.Height > 0 && viewer.Extent.Height > 2 * viewer.Viewport.Height && IsAtEnd(viewer), "the end of the log");

            // The user scrolls up to read the log: new lines do not move it
            viewer.Offset = new Vector(0, 0);
            await WindowTestHelper.PumpAsync();
            build.Logger.Info("One more line");
            build.Complete(true);
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Main.Build.BuildInProgress && log.Any(x => x.Text == "One more line"), "the end of the build");
            await Task.Delay(500);
            await WindowTestHelper.PumpAsync();
            Assert.Equal(0, viewer.Offset.Y);
        }
        finally
        {
            await window.CloseAsync();
        }

        static bool IsAtEnd(ScrollViewer viewer) => viewer.Extent.Height - viewer.Viewport.Height - viewer.Offset.Y < 1.0;
    }

    private static MenuItem MenuItem(Window window, string name) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == name);

    private static Button Button(Window window, string name) => window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == name);

    /// <summary>
    /// Whether the item is enabled when the user opens its menu: Avalonia evaluates the command of an item of a closed menu
    /// only when the menu opens.
    /// </summary>
    private static async Task<bool> IsEnabledInOpenMenuAsync(MenuItem item)
    {
        var menu = Assert.IsType<MenuItem>(item.Parent);
        menu.Open();
        await WindowTestHelper.PumpAsync();
        try
        {
            return item.IsEffectivelyEnabled;
        }
        finally
        {
            menu.Close();
            await WindowTestHelper.PumpAsync();
        }
    }
}
