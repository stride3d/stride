// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// The deploy step of the games of one platform: installs a built game on a device, or copies it to another machine, and starts it
/// there, for the Build project and Start project commands (<see cref="BuildAndRunViewModel"/>).
/// </summary>
/// <remarks>
/// The host of the editor registers one per platform with <see cref="BuildAndRunViewModel.RegisterDeployer"/>. A platform with no
/// deployer is built as it is; its game starts on this machine when it is the platform of the editor.
/// </remarks>
public interface IGameDeployer
{
    /// <summary>
    /// Prepares the build of a game, before it starts: chooses where the game goes, and adds what the build needs (its targets, its
    /// properties).
    /// </summary>
    /// <param name="project">The executable project to build.</param>
    /// <param name="request">The build the editor would run without a deployer.</param>
    /// <param name="startProject">Whether the game starts once built (Start project), or is only built (Build project).</param>
    /// <param name="logger">The log of the build, in the Output pane.</param>
    /// <param name="cancellationToken">Canceled by Cancel build.</param>
    /// <returns>The build to run, or <c>null</c> to stop before it starts, once the reason is logged as an error.</returns>
    Task<GameProjectBuildRequest?> PrepareBuildAsync(ProjectViewModel project, GameProjectBuildRequest request, bool startProject, ILogger logger, CancellationToken cancellationToken);

    /// <summary>
    /// Deploys a game that was just built, and starts it.
    /// </summary>
    /// <param name="project">The executable project that was built.</param>
    /// <param name="build">The build, with its assembly and the outputs of its targets.</param>
    /// <param name="logger">The log of the build, in the Output pane.</param>
    /// <param name="cancellationToken">Canceled by Cancel build.</param>
    /// <returns>Whether the game was deployed and started; a failure logs its errors.</returns>
    Task<bool> DeployAsync(ProjectViewModel project, IGameProjectBuild build, ILogger logger, CancellationToken cancellationToken);
}
