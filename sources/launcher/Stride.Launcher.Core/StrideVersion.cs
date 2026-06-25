// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Launcher.Core;

/// <summary>
///   A locally installed Stride version, identified by its package id and version.
/// </summary>
public sealed record StrideVersion(string PackageId, PackageVersion Version, string InstallPath)
{
    /// <summary>The major.minor line this version belongs to (e.g. "4.4"). Versions are pinned per line.</summary>
    public string Line => $"{Version.Version.Major}.{Version.Version.Minor}";
}
