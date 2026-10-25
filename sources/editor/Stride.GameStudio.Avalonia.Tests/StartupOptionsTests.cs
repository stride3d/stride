// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
    public void UnknownOptionsAreReportedNotTakenForPaths()
    {
        var options = StartupOptions.Parse(["/Unknown", "--other"]);

        Assert.Null(options.InitialSessionPath);
        Assert.Equal(["/Unknown", "--other"], options.UnknownArguments);
    }
}
