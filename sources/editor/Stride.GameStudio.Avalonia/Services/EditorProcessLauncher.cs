// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Starts a new editor process the way this one was started: its apphost, or the <c>dotnet</c> muxer running its assembly, from
/// the same working directory and with the same environment.
/// </summary>
/// <remarks>
/// The launcher and <c>stride studio</c> start the apphost when there is one, else <c>dotnet &lt;dll&gt;</c>
/// (<c>DotNetHostSelector.NativeStartInfo</c>); the launcher sets the working directory to the editor's folder, the command line
/// keeps the shell's.
/// </remarks>
public sealed class EditorProcessLauncher : IEditorProcessLauncher
{
    private readonly string appDllPath;
    private readonly string? processPath;
    private readonly string workingDirectory;

    /// <param name="appDllPath">The editor's entry assembly.</param>
    public EditorProcessLauncher(string appDllPath)
        : this(appDllPath, Environment.ProcessPath, Environment.CurrentDirectory)
    {
    }

    /// <param name="appDllPath">The editor's entry assembly.</param>
    /// <param name="processPath">The executable of this process: the apphost, or the <c>dotnet</c> muxer.</param>
    /// <param name="workingDirectory">The working directory of this process when it started.</param>
    public EditorProcessLauncher(string appDllPath, string? processPath, string workingDirectory)
    {
        this.appDllPath = appDllPath;
        this.processPath = processPath;
        this.workingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public void Start(IReadOnlyList<string> arguments)
    {
        Process.Start(CreateStartInfo(arguments));
    }

    /// <summary>
    /// The start line of the new process.
    /// </summary>
    public ProcessStartInfo CreateStartInfo(IReadOnlyList<string> arguments)
    {
        ProcessStartInfo startInfo;
        if (processPath is not null && !IsMuxer(processPath))
        {
            startInfo = new ProcessStartInfo(processPath);
        }
        else
        {
            startInfo = new ProcessStartInfo(processPath ?? "dotnet");
            startInfo.ArgumentList.Add(Path.GetFullPath(appDllPath));
        }
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = workingDirectory;
        // dotnet.exe is a console program; started from a GUI process it would get a console window of its own (as WPF's restart)
        startInfo.CreateNoWindow = true;
        return startInfo;
    }

    private static bool IsMuxer(string path)
    {
        return string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase);
    }
}
