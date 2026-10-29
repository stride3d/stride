// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Building the current project and starting its game from the shell, as in the WPF editor: the Project menu, the toolbar, F6, F5
/// and Ctrl+F6, and the build log in the Output tab. The builder and the game launcher are fakes.
/// </summary>
public sealed class BuildAndRunTests
{
    [AvaloniaFact]
    public async Task TheProjectMenuHasTheBuildCommandsOfWpf()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var build = MenuItem(window, "BuildProjectMenuItem");
            var start = MenuItem(window, "StartProjectMenuItem");
            var cancel = MenuItem(window, "CancelBuildMenuItem");
            Assert.Equal(new KeyGesture(Key.F6), build.InputGesture);
            Assert.Equal(new KeyGesture(Key.F5), start.InputGesture);
            Assert.Equal(new KeyGesture(Key.F6, KeyModifiers.Control), cancel.InputGesture);
            Assert.Same(fixture.Main.Build.BuildProjectCommand, build.Command);
            Assert.Same(fixture.Main.Build.StartProjectCommand, start.Command);
            Assert.Same(fixture.Main.Build.CancelBuildCommand, cancel.Command);

            Assert.True(await IsEnabledInOpenMenuAsync(build));
            Assert.True(await IsEnabledInOpenMenuAsync(start));
            Assert.False(await IsEnabledInOpenMenuAsync(cancel));

            // The shortcuts are key bindings of the window, which a menu item of a closed menu would not handle
            Assert.Contains(window.KeyBindings, x => x.Gesture == new KeyGesture(Key.F6) && x.Command == fixture.Main.Build.BuildProjectCommand);
            Assert.Contains(window.KeyBindings, x => x.Gesture == new KeyGesture(Key.F5) && x.Command == fixture.Main.Build.StartProjectCommand);
            Assert.Contains(window.KeyBindings, x => x.Gesture == new KeyGesture(Key.F6, KeyModifiers.Control) && x.Command == fixture.Main.Build.CancelBuildCommand);
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
        window.Show();
        try
        {
            var toolBar = window.GetVisualDescendants().OfType<Control>().Single(x => x.Name == "MainToolBar");
            Assert.False(toolBar.IsVisible);

            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();

            Assert.True(toolBar.IsVisible);
            Assert.Same(fixture.Main.Build.BuildProjectCommand, Button(window, "BuildProjectButton").Command);
            Assert.Same(fixture.Main.Build.StartProjectCommand, Button(window, "StartProjectButton").Command);
            Assert.Same(fixture.Main.Build.CancelBuildCommand, Button(window, "CancelBuildButton").Command);
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
        window.Show();
        try
        {
            var output = window.GetLogicalDescendants().OfType<TabItem>().Single(x => x.Name == "OutputTab");
            Assert.False(output.IsSelected);

            window.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            window.KeyRelease(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            await WindowTestHelper.WaitUntilAsync(() => launcher.Games.Count == 1 && !fixture.Main.Build.BuildInProgress, "the start of the game");

            // Saved first (the progress window of the save), then built and started
            Assert.Single(dialogs.ProgressWindows);
            Assert.Single(builder.Requests);
            Assert.Equal(builder.AssemblyPath, launcher.Games[0].AssemblyPath);
            // The Output tab shows the log of the build
            Assert.True(output.IsSelected);
            Assert.Equal("Output", output.Header);
            var log = Assert.IsType<TextLogViewer>(output.Content);
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
        window.Show();
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
        window.Show();
        try
        {
            fixture.Main.Build.StartProjectCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => launcher.Games.Count == 1 && !fixture.Main.Build.BuildInProgress, "the start of the game");
            var tabs = window.GetLogicalDescendants().OfType<TabItem>().ToList();
            var output = tabs.Single(x => x.Name == "OutputTab");
            Assert.True(output.IsSelected);

            // The user goes back to the asset view; the game writes to its console
            tabs.Single(x => Equals(x.Header, "Asset view")).IsSelected = true;
            await WindowTestHelper.PumpAsync();
            Assert.False(fixture.Main.Build.IsOutputVisible);
            launcher.Games.Single().Output.Info("Hello from the game");
            await WindowTestHelper.WaitUntilAsync(() => Equals(output.Header, "Output*"), "the mark of the Output tab", 10000);

            output.IsSelected = true;
            await WindowTestHelper.PumpAsync();
            Assert.Equal("Output", output.Header);
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
        window.Show();
        try
        {
            var started = builder.NextBuild;
            fixture.Main.Build.BuildProjectCommand.Execute();
            var build = await started.WaitAsync(TimeSpan.FromSeconds(30));
            for (var i = 0; i < 200; ++i)
                build.Logger.Info($"Line {i}");
            await WindowTestHelper.PumpAsync();
            var viewer = window.GetVisualDescendants().OfType<ScrollViewer>().Single(x => x.Name == "PART_LogScrollViewer");
            await WindowTestHelper.WaitUntilAsync(() => viewer.Extent.Height > viewer.Viewport.Height && IsAtEnd(viewer), "the end of the log");

            // The user scrolls up to read the log: new lines do not move it
            viewer.Offset = new Vector(0, 0);
            await WindowTestHelper.PumpAsync();
            build.Logger.Info("One more line");
            build.Complete(true);
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Main.Build.BuildInProgress, "the end of the build");
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
