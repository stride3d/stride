// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Cli.Core;

/// <summary>
///   A locally installed Stride version, with the Game Studios installed for it (each one is a package of its own).
/// </summary>
public sealed record StrideVersion(PackageVersion Version, IReadOnlyList<GameStudioEditor> Editors)
{
    /// <summary>The major.minor line this version belongs to (e.g. "4.4"). Versions are pinned per line.</summary>
    public string Line => $"{Version.Version.Major}.{Version.Version.Minor}";
}
