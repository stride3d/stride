// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Starts another instance of the editor. The desktop host gives one to <see cref="App.ProcessLauncher"/>; a host without one
/// (the headless tests, a single view) restarts the editor in its own process.
/// </summary>
public interface IEditorProcessLauncher
{
    /// <summary>
    /// Starts a new editor process with the given command line.
    /// </summary>
    void Start(IReadOnlyList<string> arguments);
}
