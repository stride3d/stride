// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Starts a built game on this machine for the Start project command (<see cref="BuildAndRunViewModel"/>).
/// </summary>
/// <remarks>
/// The editor starts a process (<see cref="GameProcessLauncher"/>); the tests replace it.
/// </remarks>
public interface IGameLauncher
{
    /// <summary>
    /// Starts the game built at the given path.
    /// </summary>
    /// <param name="assemblyPath">The assembly of the game's executable project, as the build produced it.</param>
    /// <param name="output">The logger that receives what the game writes to its standard output (info) and error (errors).</param>
    /// <returns>The running game.</returns>
    IGameProcess Start(string assemblyPath, ILogger output);
}

/// <summary>
/// A game started by an <see cref="IGameLauncher"/>.
/// </summary>
public interface IGameProcess
{
    /// <summary>
    /// The identifier of the game's process.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Completes with the exit code of the game when it exits.
    /// </summary>
    Task<int> Exited { get; }
}
