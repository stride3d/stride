// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A builder that records the builds it is asked for: they succeed at once, or wait for the test (<see cref="CompleteAtOnce"/>).
/// </summary>
public sealed class FakeGameProjectBuilder : IGameProjectBuilder
{
    private readonly Lock gate = new();
    private readonly List<GameProjectBuildRequest> requests = [];
    private TaskCompletionSource<FakeGameProjectBuild> nextBuild = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The assembly the builds produce.
    /// </summary>
    public string AssemblyPath { get; set; } = Path.Combine(Path.GetTempPath(), "TestGame", "bin", "TestGame.dll");

    /// <summary>
    /// Whether the next builds complete as soon as they start, with <see cref="Succeed"/>; otherwise <see cref="FakeGameProjectBuild.Complete"/> does.
    /// </summary>
    public bool CompleteAtOnce { get; set; } = true;

    /// <summary>
    /// Whether the next builds that complete at once succeed.
    /// </summary>
    public bool Succeed { get; set; } = true;

    /// <summary>
    /// The errors the next builds log, as MSBuild logs the errors of the compiler.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Whether the next projects fail to load (no build starts).
    /// </summary>
    public bool FailToLoad { get; set; }

    /// <summary>
    /// The builds asked for so far.
    /// </summary>
    public IReadOnlyList<GameProjectBuildRequest> Requests
    {
        get
        {
            lock (gate)
                return [.. requests];
        }
    }

    /// <summary>
    /// Completes when the next build starts: read it before starting the build.
    /// </summary>
    public Task<FakeGameProjectBuild> NextBuild
    {
        get
        {
            lock (gate)
                return nextBuild.Task;
        }
    }

    public IGameProjectBuild? StartBuild(GameProjectBuildRequest request, ILogger logger)
    {
        TaskCompletionSource<FakeGameProjectBuild> started;
        lock (gate)
        {
            requests.Add(request);
            if (FailToLoad)
                return null;
            started = nextBuild;
            nextBuild = new TaskCompletionSource<FakeGameProjectBuild>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var build = new FakeGameProjectBuild(AssemblyPath, logger);
        foreach (var error in Errors)
            logger.Error(error);
        if (CompleteAtOnce)
            build.Complete(Succeed);
        started.SetResult(build);
        return build;
    }
}

/// <summary>
/// A build of <see cref="FakeGameProjectBuilder"/>.
/// </summary>
public sealed class FakeGameProjectBuild : IGameProjectBuild
{
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FakeGameProjectBuild(string assemblyPath, ILogger logger)
    {
        AssemblyPath = assemblyPath;
        Logger = logger;
    }

    public string AssemblyPath { get; }

    /// <summary>
    /// The logger the build was given.
    /// </summary>
    public ILogger Logger { get; }

    public Task<bool> Completion => completion.Task;

    public bool IsCanceled { get; private set; }

    public void Cancel()
    {
        IsCanceled = true;
        // MSBuild reports a failure when its submissions are cancelled
        completion.TrySetResult(false);
    }

    /// <summary>
    /// Ends the build.
    /// </summary>
    public void Complete(bool success) => completion.TrySetResult(success);
}
