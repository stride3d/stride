// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A deploy step that records what it is asked: it installs the game with an <c>Install</c> target on a device it names, and deploys
/// at once, or waits for Cancel build (<see cref="WaitForCancel"/>).
/// </summary>
public sealed class FakeGameDeployer : IGameDeployer
{
    private readonly Lock gate = new();
    private readonly List<(GameProjectBuildRequest Request, bool StartProject)> preparations = [];
    private readonly List<IGameProjectBuild> deployments = [];

    /// <summary>
    /// The device the builds are prepared for.
    /// </summary>
    public const string Device = "-s emulator-5554";

    /// <summary>
    /// Whether the preparation stops the build, with an error.
    /// </summary>
    public bool FailToPrepare { get; set; }

    /// <summary>
    /// Whether the deployments succeed.
    /// </summary>
    public bool Deploy { get; set; } = true;

    /// <summary>
    /// The exception the deployments throw, if any.
    /// </summary>
    public Exception? DeployException { get; set; }

    /// <summary>
    /// Whether the deployments wait until Cancel build.
    /// </summary>
    public bool WaitForCancel { get; set; }

    /// <summary>
    /// Completes when a deployment starts.
    /// </summary>
    public TaskCompletionSource DeploymentStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The builds prepared so far, as the editor asked for them, and whether their game was to start.
    /// </summary>
    public IReadOnlyList<(GameProjectBuildRequest Request, bool StartProject)> Preparations
    {
        get
        {
            lock (gate)
                return [.. preparations];
        }
    }

    /// <summary>
    /// The builds deployed so far.
    /// </summary>
    public IReadOnlyList<IGameProjectBuild> Deployments
    {
        get
        {
            lock (gate)
                return [.. deployments];
        }
    }

    public Task<GameProjectBuildRequest?> PrepareBuildAsync(ProjectViewModel project, GameProjectBuildRequest request, bool startProject, ILogger logger, CancellationToken cancellationToken)
    {
        lock (gate)
            preparations.Add((request, startProject));
        if (FailToPrepare)
        {
            logger.Error("No device found.");
            return Task.FromResult<GameProjectBuildRequest?>(null);
        }

        var properties = new Dictionary<string, string>(request.Properties) { ["AdbTarget"] = Device };
        return Task.FromResult<GameProjectBuildRequest?>(request with { Target = "GetAndroidPackage;Install", Properties = properties });
    }

    public async Task<bool> DeployAsync(ProjectViewModel project, IGameProjectBuild build, ILogger logger, CancellationToken cancellationToken)
    {
        lock (gate)
            deployments.Add(build);
        DeploymentStarted.TrySetResult();
        if (WaitForCancel)
            await Task.Delay(Timeout.Infinite, cancellationToken);
        if (DeployException is not null)
            throw DeployException;
        if (!Deploy)
            logger.Error("The device refused the game.");
        return Deploy;
    }
}
