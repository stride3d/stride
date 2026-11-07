// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A launcher that records the games it starts, without starting a process.
/// </summary>
public sealed class FakeGameLauncher : IGameLauncher
{
    private readonly Lock gate = new();
    private readonly List<FakeGameProcess> games = [];

    /// <summary>
    /// Whether the next games fail to start.
    /// </summary>
    public bool FailToStart { get; set; }

    /// <summary>
    /// The games started so far.
    /// </summary>
    public IReadOnlyList<FakeGameProcess> Games
    {
        get
        {
            lock (gate)
                return [.. games];
        }
    }

    public IGameProcess Start(string assemblyPath, ILogger output)
    {
        if (FailToStart)
            throw new FileNotFoundException("The game's executable is missing.", assemblyPath);

        lock (gate)
        {
            var game = new FakeGameProcess(1000 + games.Count, assemblyPath, output);
            games.Add(game);
            return game;
        }
    }
}

/// <summary>
/// A game of <see cref="FakeGameLauncher"/>: it runs until <see cref="Exit"/>.
/// </summary>
public sealed class FakeGameProcess : IGameProcess
{
    private readonly TaskCompletionSource<int> exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FakeGameProcess(int id, string assemblyPath, ILogger output)
    {
        Id = id;
        AssemblyPath = assemblyPath;
        Output = output;
    }

    public int Id { get; }

    /// <summary>
    /// The assembly the game was started from.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// Where the game writes its console output.
    /// </summary>
    public ILogger Output { get; }

    public Task<int> Exited => exited.Task;

    /// <summary>
    /// Ends the game with the given exit code.
    /// </summary>
    public void Exit(int exitCode) => exited.TrySetResult(exitCode);
}
