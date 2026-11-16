// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Starts another instance of the editor, on the .NET major its session needs. The desktop host gives one to
/// <see cref="App.ProcessLauncher"/>; a host without one (the headless tests, a single view) restarts the editor in its own process.
/// </summary>
public interface IEditorProcessLauncher
{
    /// <summary>
    /// Starts a new editor process with the given command line.
    /// </summary>
    /// <param name="arguments">The command line of the new editor.</param>
    /// <param name="sessionPath">The session it opens, which decides its .NET major; <c>null</c> for the project selection.</param>
    void Start(IReadOnlyList<string> arguments, string? sessionPath);

    /// <summary>
    /// Whether this process can load a session, by the .NET major its game needs.
    /// </summary>
    /// <param name="sessionPath">The session to load.</param>
    /// <param name="arguments">The command line a new editor for that session would get (its <c>--framework</c> choice).</param>
    /// <param name="reason">Why the session cannot open, for <see cref="SessionHost.Missing"/>.</param>
    SessionHost CheckHost(string sessionPath, IReadOnlyList<string> arguments, out string? reason);
}
