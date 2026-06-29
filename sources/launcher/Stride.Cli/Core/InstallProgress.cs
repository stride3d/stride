// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Cli.Core;

/// <summary>Phase of a <see cref="StrideVersionManager.Install"/> / Update operation.</summary>
public enum InstallStage
{
    /// <summary>Resolving and downloading the package closure (no granular byte progress).</summary>
    Downloading,

    /// <summary>Extracting/installing a resolved package (<see cref="InstallProgress.Completed"/> of <see cref="InstallProgress.Total"/>).</summary>
    Installing,

    /// <summary>Running a package's setup step (packageinstall.exe).</summary>
    SettingUp,

    /// <summary>Deleting an installed package during uninstall.</summary>
    Removing,
}

/// <summary>A progress update for an install/update operation, suitable for a single-line console renderer.</summary>
/// <param name="Stage">The current phase.</param>
/// <param name="Version">The Stride version being installed (set on <see cref="InstallStage.Downloading"/>).</param>
/// <param name="Package">The package currently being installed or set up.</param>
/// <param name="Completed">Packages installed so far.</param>
/// <param name="Total">Total packages to install (0 until known).</param>
/// <param name="DownloadedBytes">Bytes downloaded so far (download phase).</param>
public readonly record struct InstallProgress(InstallStage Stage, string? Version = null, string? Package = null, int Completed = 0, int Total = 0, long DownloadedBytes = 0);
