// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Starting a built game: its apphost, else the <c>dotnet</c> muxer, from the folder of the game; its console output and its exit code.
/// </summary>
public sealed class GameProcessLauncherTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));

    public GameProcessLauncherTests()
    {
        Directory.CreateDirectory(directory);
    }

    private string AssemblyPath => Path.Combine(directory, "MyGame.Linux.dll");

    private string AppHostPath => Path.ChangeExtension(AssemblyPath, OperatingSystem.IsWindows() ? ".exe" : null);

    [Fact]
    public void TheGameStartsFromItsAppHost()
    {
        File.WriteAllText(AssemblyPath, string.Empty);
        File.WriteAllText(AppHostPath, string.Empty);

        var startInfo = GameProcessLauncher.CreateStartInfo(AssemblyPath);

        Assert.Equal(AppHostPath, startInfo.FileName);
        Assert.Empty(startInfo.ArgumentList);
        Assert.Equal(directory, startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void WithoutAnAppHostTheMuxerRunsTheGame()
    {
        File.WriteAllText(AssemblyPath, string.Empty);

        var startInfo = GameProcessLauncher.CreateStartInfo(AssemblyPath);

        Assert.Equal("dotnet", startInfo.FileName);
        Assert.Equal([AssemblyPath], startInfo.ArgumentList);
        Assert.Equal(directory, startInfo.WorkingDirectory);
    }

    [SkippableFact]
    public async Task TheOutputAndTheExitCodeOfTheGameAreReported()
    {
        // The apphost is a script here, which Windows cannot run as an .exe
        Skip.If(OperatingSystem.IsWindows(), "The apphost of the test is a shell script.");
        File.WriteAllText(AssemblyPath, string.Empty);
        File.WriteAllText(AppHostPath, "#!/bin/sh\necho \"started in $(pwd)\"\necho \"something went wrong\" >&2\nexit 3\n");
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(AppHostPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var output = new LoggerResult();

        var game = new GameProcessLauncher().Start(AssemblyPath, output);

        Assert.Equal(3, await game.Exited.WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.True(game.Id > 0);
        var info = Assert.Single(output.Messages, x => x.Type == LogMessageType.Info);
        Assert.Equal($"started in {directory}", info.Text);
        var error = Assert.Single(output.Messages, x => x.Type == LogMessageType.Error);
        Assert.Equal("something went wrong", error.Text);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }
}
