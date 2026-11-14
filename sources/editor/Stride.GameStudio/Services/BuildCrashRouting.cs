// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Diagnostics;
using Stride.CrashReport;

namespace Stride.GameStudio.Services;

/// <summary>
/// Brings the asset compiler crashes of the game builds back to Game Studio, through the crash reporter's
/// <see cref="CompilerCrashRouting"/>.
/// </summary>
internal sealed class BuildCrashRouting : IBuildCrashRouting
{
    /// <inheritdoc/>
    public string? CrashDirectory => CompilerCrashRouting.CrashDirectory;

    /// <inheritdoc/>
    public void SurfacePendingCrashes(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        CompilerCrashRouting.SurfacePendingCrashes(message => logger.Warning(message));
    }
}
