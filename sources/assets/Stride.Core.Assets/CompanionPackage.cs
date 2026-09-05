// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// What a package carries: the asset compiler loads <see cref="Assets"/> packages, the editor loads
/// <see cref="Assets"/> and <see cref="Editor"/> ones.
/// </summary>
[DataContract("PackageKind")]
public enum PackageKind
{
    /// <summary>Game code and engine runtime; the default.</summary>
    Runtime,

    /// <summary>Asset types, compilers, importers and templates.</summary>
    Assets,

    /// <summary>Editor extensions: gizmos, previews, thumbnails, property grid behaviour.</summary>
    Editor,
}

/// <summary>
/// A package a host loads on behalf of the package declaring it (<see cref="Package.CompanionPackages"/>); the game never references it.
/// </summary>
[DataContract("CompanionPackage")]
public sealed class CompanionPackage
{
    /// <summary>Package id of the companion.</summary>
    [DataMember(0)]
    public string? Name { get; set; }

    /// <summary>Version the declaring package was packed with; the declaring package's own version when unset.</summary>
    [DataMember(1)]
    public PackageVersion? Version { get; set; }

    /// <summary>What the companion carries, copied from its own declaration.</summary>
    [DataMember(2)]
    public PackageKind Kind { get; set; }

    /// <summary>Companion packages declared elsewhere in the session that this one replaces; they are not loaded.</summary>
    [DataMember(3)]
    public List<string> Replaces { get; } = [];

    /// <summary>The companion's project, known when the declaring package comes from a project or its build manifest.</summary>
    [DataMemberIgnore]
    public UFile? Project { get; set; }

    private bool ShouldSerializeReplaces() => Replaces.Count > 0;

    public override string ToString() => $"{Kind} package [{Name}] version [{Version}]";
}
