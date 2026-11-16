// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A launcher of new editor processes that records their command lines instead of starting them.
/// </summary>
internal sealed class RecordingProcessLauncher : IEditorProcessLauncher
{
    public List<IReadOnlyList<string>> Starts { get; } = [];

    public EditorProcessService Service { get; set; } = null!;

    public Action? Started { get; set; }

    /// <summary>
    /// Where each session can load, and why not when its .NET major is missing; this process by default.
    /// </summary>
    public Func<string, (SessionHost Host, string? Reason)> Host { get; set; } = _ => (SessionHost.ThisProcess, null);

    public void Start(IReadOnlyList<string> arguments, string? sessionPath)
    {
        Starts.Add([.. arguments]);
        Started?.Invoke();
    }

    public SessionHost CheckHost(string sessionPath, IReadOnlyList<string> arguments, out string? reason)
    {
        (var host, reason) = Host(sessionPath);
        return host;
    }
}
