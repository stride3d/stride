// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Assets;
using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.Desktop;

/// <summary>
/// Starts the editor on the .NET major its session needs (<see cref="DotNetHostSelector"/>), as the WPF Game Studio does: a game's
/// assemblies load into the editor's process, so a game on a newer major than the editor's build needs the editor re-executed on
/// it. Using the selector here is also what makes the launcher and <c>stride studio</c> offer <c>--framework</c> to this editor
/// (<see cref="DotNetHostSelector.SupportsHostSelection"/>).
/// </summary>
internal sealed class DotNetHostLauncher : IEditorProcessLauncher
{
    private readonly string appDllPath;
    private readonly EditorProcessLauncher sameHost;
    private readonly string? relaunchedSession;

    /// <param name="appDllPath">The editor's entry assembly.</param>
    /// <param name="args">The command line of this process.</param>
    /// <param name="initialSessionPath">The session of the command line, if any.</param>
    public DotNetHostLauncher(string appDllPath, IReadOnlyList<string> args, string? initialSessionPath)
    {
        this.appDllPath = appDllPath;
        sameHost = new EditorProcessLauncher(appDllPath);
        relaunchedSession = args.Contains(DotNetHostSelector.RelaunchedArg) ? initialSessionPath : null;
    }

    /// <summary>
    /// Re-executes this editor when the session of its command line, or its <c>--framework</c>, needs a newer .NET major than it
    /// runs on. Returns whether it did: this process then exits without opening a window.
    /// </summary>
    public bool RelaunchIfNeeded(string? sessionPath, IReadOnlyList<string> args)
        => TryRelaunch(Environment.Version.Major, sessionPath, args);

    /// <inheritdoc/>
    public void Start(IReadOnlyList<string> arguments, string? sessionPath)
    {
        // A new process runs on the editor's own major unless it is re-executed
        if (!TryRelaunch(DotNetHostSelector.ReadNativeMajor(appDllPath), sessionPath, arguments))
            Process.Start(sameHost.CreateStartInfo(arguments));
    }

    /// <inheritdoc/>
    public SessionHost CheckHost(string sessionPath, IReadOnlyList<string> arguments, out string? reason)
    {
        // This process was re-executed for that session: it isn't again, the major still missing is reported
        if (sessionPath == relaunchedSession)
            arguments = [.. arguments, DotNetHostSelector.RelaunchedArg];
        var decision = DotNetHostSelector.ResolveFor(appDllPath, Environment.Version.Major, sessionPath, arguments);
        reason = decision.Reason;
        return decision.Kind switch
        {
            DotNetHostSelector.DecisionKind.Relaunch => SessionHost.NewProcess,
            DotNetHostSelector.DecisionKind.Missing => SessionHost.Missing,
            _ => SessionHost.ThisProcess,
        };
    }

    private bool TryRelaunch(int currentMajor, string? sessionPath, IReadOnlyList<string> arguments)
    {
        var decision = DotNetHostSelector.ResolveFor(appDllPath, currentMajor, sessionPath, arguments);
        if (decision.Kind != DotNetHostSelector.DecisionKind.Relaunch)
            return false;
        Process.Start(sameHost.Prepare(DotNetHostSelector.RelaunchStartInfo(appDllPath, decision.Major, arguments, decision.Install)));
        return true;
    }
}
