// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Brings the asset compiler crashes of a build back to the editor that started it, which shows them once the build ends.
/// </summary>
/// <remarks>
/// The editor's host provides it to <see cref="MSBuildGameProjectBuilder"/> (Game Studio routes through the crash reporter's
/// <c>CompilerCrashRouting</c>); without one, a crash goes to the default crash store.
/// </remarks>
public interface IBuildCrashRouting
{
    /// <summary>
    /// Gets the directory the asset compiler writes this editor's crashes to (the <c>StrideCrashDir</c> property of the build), or
    /// <c>null</c> when the crashes are not shown in the editor.
    /// </summary>
    string? CrashDirectory { get; }

    /// <summary>
    /// Shows the crashes that the builds wrote since the last call, after a build ended.
    /// </summary>
    /// <param name="logger">The logger of the build, which receives a warning when a crash can't be shown.</param>
    void SurfacePendingCrashes(ILogger logger);
}
