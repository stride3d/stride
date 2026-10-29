// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Builds the project of a game for the Build project and Start project commands (<see cref="BuildAndRunViewModel"/>).
/// </summary>
/// <remarks>
/// The editor builds with MSBuild (<see cref="MSBuildGameProjectBuilder"/>); the tests replace it.
/// </remarks>
public interface IGameProjectBuilder
{
    /// <summary>
    /// Starts building a project.
    /// </summary>
    /// <param name="request">The project and the properties of the build.</param>
    /// <param name="logger">The logger that receives the output of the build: its messages, warnings and errors.</param>
    /// <returns>The running build, or <c>null</c> when the project could not be loaded.</returns>
    IGameProjectBuild? StartBuild(GameProjectBuildRequest request, ILogger logger);
}

/// <summary>
/// A build started by an <see cref="IGameProjectBuilder"/>.
/// </summary>
public interface IGameProjectBuild
{
    /// <summary>
    /// The assembly the build produces (the <c>TargetPath</c> of the project).
    /// </summary>
    string AssemblyPath { get; }

    /// <summary>
    /// Completes when the build is over: <c>true</c> when it succeeded. A failure also logs its errors.
    /// </summary>
    Task<bool> Completion { get; }

    /// <summary>
    /// Whether <see cref="Cancel"/> was called.
    /// </summary>
    bool IsCanceled { get; }

    /// <summary>
    /// Stops the build; <see cref="Completion"/> then completes.
    /// </summary>
    void Cancel();
}

/// <summary>
/// What to build: a project, its MSBuild target, configuration and platform, and extra global properties.
/// </summary>
/// <param name="ProjectPath">The full path of the project file.</param>
/// <param name="Platform">The MSBuild <c>Platform</c> of the build.</param>
/// <param name="Properties">The other global properties of the build (the solution and its platform...).</param>
public sealed record GameProjectBuildRequest(string ProjectPath, string Platform, IReadOnlyDictionary<string, string> Properties)
{
    /// <summary>
    /// The MSBuild targets to run, separated by <c>;</c>.
    /// </summary>
    public string Target { get; init; } = "Build";

    /// <summary>
    /// The MSBuild <c>Configuration</c> of the build.
    /// </summary>
    public string Configuration { get; init; } = "Debug";
}
