// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// A host-loadable asset assembly declared in a packed <c>sdpkg</c>: the package-relative path to the
/// dll plus the <see cref="TargetFramework"/> it was built for. A multi-targeted package declares one
/// entry per host-compatible TFM so a consumer loads the build matching its asset compiler's runtime.
/// <see cref="TargetFramework"/> is null for legacy single-TFM packages (load unconditionally).
/// </summary>
[DataContract("AssetAssembly")]
public sealed class AssetAssembly
{
    public AssetAssembly() { }

    public AssetAssembly(string? targetFramework, UFile path)
    {
        TargetFramework = targetFramework;
        Path = path;
    }

    /// <summary>The TFM this assembly was built for (e.g. <c>net10.0</c>, <c>net10.0-windows7.0</c>), or null if untagged.</summary>
    public string? TargetFramework { get; set; }

    /// <summary>Package-relative path to the assembly (e.g. <c>../lib/net10.0/MyPlugin.dll</c>).</summary>
    public UFile? Path { get; set; }
}
