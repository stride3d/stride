// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The new editor process starts the way this one did: its apphost, or <c>dotnet</c> running its assembly.
/// </summary>
public sealed class EditorProcessLauncherTests
{
    private static readonly string AppDll = Path.GetFullPath(Path.Combine("editor", "Stride.GameStudio.Avalonia.Desktop.dll"));
    private static readonly string WorkingDirectory = Path.GetFullPath("games");

    [Fact]
    public void AnApphostStartsItselfAgain()
    {
        var apphost = Path.ChangeExtension(AppDll, OperatingSystem.IsWindows() ? ".exe" : null);
        var launcher = new EditorProcessLauncher(AppDll, apphost, WorkingDirectory);

        var startInfo = launcher.CreateStartInfo(["/DisablePreview", "/NewProject"]);

        Assert.Equal(apphost, startInfo.FileName);
        Assert.Equal(["/DisablePreview", "/NewProject"], startInfo.ArgumentList);
        Assert.Equal(WorkingDirectory, startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.CreateNoWindow);
    }

    [Theory]
    [InlineData("dotnet")]
    [InlineData("dotnet.exe")]
    public void TheMuxerRunsTheAssemblyAgain(string muxerName)
    {
        var muxer = Path.GetFullPath(Path.Combine("dotnet-root", muxerName));
        var launcher = new EditorProcessLauncher(AppDll, muxer, WorkingDirectory);

        var startInfo = launcher.CreateStartInfo(["MyGame.sln"]);

        Assert.Equal(muxer, startInfo.FileName);
        Assert.Equal([AppDll, "MyGame.sln"], startInfo.ArgumentList);
        Assert.Equal(WorkingDirectory, startInfo.WorkingDirectory);
    }

    [Fact]
    public void WithoutAProcessPathTheMuxerOfThePathRunsTheAssembly()
    {
        var launcher = new EditorProcessLauncher(AppDll, null, WorkingDirectory);

        var startInfo = launcher.CreateStartInfo([]);

        Assert.Equal("dotnet", startInfo.FileName);
        Assert.Equal([AppDll], startInfo.ArgumentList);
    }
}
