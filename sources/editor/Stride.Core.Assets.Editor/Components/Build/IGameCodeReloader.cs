// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Reloads the game code of a session in the editor's process: the assemblies of the rebuilt projects replace the loaded ones,
/// and the objects of their types take the new types, as one change of the undo history.
/// </summary>
/// <remarks>
/// <see cref="GameCodeReloadViewModel"/> reloads through this service when the editor registers one, and opens the session again
/// in a fresh editor when there is none, or when the service says that a change can't be reloaded in place.
/// </remarks>
public interface IGameCodeReloader
{
    /// <summary>
    /// Reloads the assemblies that these projects built, and the loaded assemblies that depend on them.
    /// </summary>
    /// <param name="session">The session whose code changed.</param>
    /// <param name="builtAssemblies">The assembly each rebuilt project produced, by project file.</param>
    /// <param name="log">Where the reload says what it did, or why it can't reload in place.</param>
    /// <param name="undone">Called when the reload is undone: the previous code is back, so the new code waits to be reloaded again.</param>
    /// <returns>Whether the code reloaded in place, or why the session has to open again in a fresh editor.</returns>
    Task<GameCodeReloadResult> ReloadAsync(SessionViewModel session, IReadOnlyDictionary<string, string> builtAssemblies, ILogger log, Action undone);
}

/// <summary>
/// The result of <see cref="IGameCodeReloader.ReloadAsync"/>.
/// </summary>
/// <param name="Reloaded">Whether the code reloaded in the editor's process.</param>
/// <param name="RestartReason">Why the code can't reload in place, when it did not: the session has to open again in a fresh
/// editor to load it.</param>
public sealed record GameCodeReloadResult(bool Reloaded, string? RestartReason)
{
    /// <summary>
    /// The code reloaded in place.
    /// </summary>
    public static GameCodeReloadResult InPlace { get; } = new(true, null);

    /// <summary>
    /// The code can't reload in place, for this reason.
    /// </summary>
    public static GameCodeReloadResult Restart(string reason) => new(false, reason);
}
