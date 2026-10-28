// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The command line of the desktop host, as the WPF Game Studio reads it.
/// </summary>
public sealed class StartupOptionsTests
{
    [Fact]
    public void NoArgumentsMeansAnEmptyEditorWithEverythingEnabled()
    {
        var options = StartupOptions.Parse([]);

        Assert.Null(options.InitialSessionPath);
        Assert.False(options.NewProject);
        Assert.True(options.EnableThumbnails);
        Assert.False(options.DisablePreview);
        Assert.False(options.DebugEditorGraphics);
        Assert.False(options.ViewportReadback);
        Assert.Empty(options.UnknownArguments);
    }

    [Fact]
    public void TheViewportReadbackOptionTurnsTheGpuInteropOff()
    {
        var options = StartupOptions.Parse(["--viewport-readback", "/home/user/MyGame/MyGame.sln"]);

        Assert.True(options.ViewportReadback);
        Assert.Equal("/home/user/MyGame/MyGame.sln", options.InitialSessionPath?.FullPath);
        Assert.Empty(options.UnknownArguments);
    }

    [Fact]
    public void ThePathArgumentIsTheSessionToOpen()
    {
        var options = StartupOptions.Parse(["/DisableThumbnails", "/DisablePreview", "/DebugEditorGraphics", "C:/Games/MyGame/MyGame.sln"]);

        Assert.Equal("C:/Games/MyGame/MyGame.sln", options.InitialSessionPath?.FullPath);
        Assert.False(options.EnableThumbnails);
        Assert.True(options.DisablePreview);
        Assert.True(options.DebugEditorGraphics);
    }

    [Fact]
    public void TheLauncherAndGraphicsArgumentsAreNotSessionPaths()
    {
        var options = StartupOptions.Parse(["/LauncherWindowHandle", "1234", "/LauncherPipe", "stride-launcher", "--graphics-api", "Vulkan", "/RecordEffects", "effects.log", "/NewProject"]);

        Assert.Null(options.InitialSessionPath);
        Assert.True(options.NewProject);
        Assert.Empty(options.UnknownArguments);
    }

    [Fact]
    public void AnAbsoluteUnixPathIsASessionNotAnOption()
    {
        var options = StartupOptions.Parse(["/home/user/MyGame/MyGame.sln"]);

        Assert.Equal("/home/user/MyGame/MyGame.sln", options.InitialSessionPath?.FullPath);
        Assert.Empty(options.UnknownArguments);

        var package = StartupOptions.Parse(["/NewProject", "/tmp/MyGame/MyGame.sdpkg"]);
        Assert.Equal("/tmp/MyGame/MyGame.sdpkg", package.InitialSessionPath?.FullPath);
        Assert.False(package.NewProject);
    }

    [Fact]
    public void TheProjectSwitchNamesTheSession()
    {
        var options = StartupOptions.Parse(["/NewProject", "--project", "/home/user/MyGame/MyGame.sln", "/DisablePreview"]);

        Assert.Equal("/home/user/MyGame/MyGame.sln", options.InitialSessionPath?.FullPath);
        Assert.False(options.NewProject);
        Assert.True(options.DisablePreview);
        Assert.Empty(options.UnknownArguments);

        var windowsStyle = StartupOptions.Parse(["/project", "C:/Games/MyGame/MyGame.sdpkg"]);
        Assert.Equal("C:/Games/MyGame/MyGame.sdpkg", windowsStyle.InitialSessionPath?.FullPath);
    }

    [Fact]
    public void AProjectSwitchWithoutPathIsReported()
    {
        var options = StartupOptions.Parse(["--project"]);

        Assert.Null(options.InitialSessionPath);
        Assert.Equal(["--project"], options.UnknownArguments);
    }

    [Fact]
    public void UnknownOptionsAreReportedNotTakenForPaths()
    {
        var options = StartupOptions.Parse(["/Unknown", "--other"]);

        Assert.Null(options.InitialSessionPath);
        Assert.Equal(["/Unknown", "--other"], options.UnknownArguments);
    }

    [Fact]
    public void ARestartKeepsTheOptionsAndOpensTheNextSession()
    {
        var options = StartupOptions.Parse(["/DisableThumbnails", "--graphics-api", "Vulkan", "/home/user/MyGame/MyGame.sln", "/DebugEditorGraphics", "/RecordEffects", "effects.log"]);

        UFile next = "/home/user/Other/Other.sln";
        Assert.Equal(["/DisableThumbnails", "--graphics-api", "Vulkan", "/DebugEditorGraphics", "/RecordEffects", "effects.log", next.ToOSPath()],
            options.GetRestartArguments(next, launcherNotified: true));
    }

    [Fact]
    public void ARestartWithoutSessionShowsTheProjectSelection()
    {
        var options = StartupOptions.Parse(["/DisablePreview", "/home/user/MyGame/MyGame.sln"]);

        Assert.Equal(["/DisablePreview", "/NewProject"], options.GetRestartArguments(null, launcherNotified: true));
    }

    [Fact]
    public void ARestartTellsTheLauncherOnlyIfThisEditorDidNot()
    {
        // As WPF's restart: once the launcher heard from this editor, the next one does not tell it again
        var options = StartupOptions.Parse(["/LauncherPipe", "stride-launcher-42", "/LauncherWindowHandle", "1234", "/NewProject", "/Unknown"]);

        UFile session = "/home/user/MyGame/MyGame.sln";
        Assert.Equal([session.ToOSPath()], options.GetRestartArguments(session, launcherNotified: true));
        Assert.Equal(["/LauncherPipe", "stride-launcher-42", "/LauncherWindowHandle", "1234", session.ToOSPath()],
            options.GetRestartArguments(session, launcherNotified: false));
    }
}
