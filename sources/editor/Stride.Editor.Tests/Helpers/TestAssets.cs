// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Data;

namespace Stride.Editor.Tests.Helpers;

/// <summary>
/// A game settings section some asset type depends on.
/// </summary>
[DataContract("TestSectionSettings")]
public sealed class TestSectionSettings : Configuration
{
    public int Value { get; set; }
}

/// <summary>
/// An asset whose compilation reads <see cref="TestSectionSettings"/>.
/// </summary>
[DataContract("SectionDependentAsset")]
[AssetDescription(".sdsectiontest")]
[GameSettingsDependency(typeof(TestSectionSettings))]
public sealed class SectionDependentAsset : Asset
{
    public string? Value { get; set; }
}

/// <summary>
/// An asset type whose thumbnail is the static image declared by the assembly.
/// </summary>
[DataContract("StaticThumbnailAsset")]
[AssetDescription(".sdstaticthumbtest")]
public sealed class StaticThumbnailAsset : Asset
{
    public string? Value { get; set; }
}
