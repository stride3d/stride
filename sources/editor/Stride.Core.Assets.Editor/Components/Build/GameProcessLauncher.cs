// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Starts a built game in a process of its own, from the folder of its assembly, as the WPF editor starts a Windows game.
/// </summary>
/// <remarks>
/// The game keeps running when the editor exits. Its console output goes to the editor's Output pane while the editor runs.
/// </remarks>
public sealed class GameProcessLauncher : IGameLauncher
{
    /// <inheritdoc/>
    public IGameProcess Start(string assemblyPath, ILogger output)
    {
        ArgumentNullException.ThrowIfNull(assemblyPath);
        ArgumentNullException.ThrowIfNull(output);

        var startInfo = CreateStartInfo(assemblyPath);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        // The two streams are read on different threads
        var outputLock = new object();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            lock (outputLock)
                output.Info(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            lock (outputLock)
                output.Error(e.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return new GameProcess(process);
    }

    /// <summary>
    /// The start line of a game: the apphost the SDK writes next to the assembly (<c>MyGame.exe</c> on Windows, <c>MyGame</c>
    /// elsewhere), else the <c>dotnet</c> muxer running the assembly. The game starts from its folder.
    /// </summary>
    /// <param name="assemblyPath">The assembly of the game's executable project.</param>
    public static ProcessStartInfo CreateStartInfo(string assemblyPath)
    {
        ArgumentNullException.ThrowIfNull(assemblyPath);

        ProcessStartInfo startInfo;
        var appHost = Path.ChangeExtension(assemblyPath, OperatingSystem.IsWindows() ? ".exe" : null);
        if (string.Equals(Path.GetExtension(assemblyPath), ".exe", StringComparison.OrdinalIgnoreCase) || File.Exists(appHost))
        {
            startInfo = new ProcessStartInfo(File.Exists(appHost) ? appHost : assemblyPath);
        }
        else
        {
            startInfo = new ProcessStartInfo("dotnet");
            startInfo.ArgumentList.Add(assemblyPath);
        }
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        // A console game started by the muxer would get a console window of its own on Windows
        startInfo.CreateNoWindow = true;
        return startInfo;
    }

    private sealed class GameProcess : IGameProcess
    {
        public GameProcess(Process process)
        {
            Id = process.Id;
            Exited = WaitForExitAsync(process);
        }

        public int Id { get; }

        public Task<int> Exited { get; }

        private static async Task<int> WaitForExitAsync(Process process)
        {
            using (process)
            {
                // Also waits for the end of the redirected output
                await process.WaitForExitAsync();
                return process.ExitCode;
            }
        }
    }
}
