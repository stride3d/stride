// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Stride.Assets;
using Stride.Core;
using Stride.Core.Assets;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

[DataContract("HeightfieldAsset")]
[AssetDescription(FileExtension, AllowArchetype = false)]
[AssetContentType(typeof(Heightfield))]
[AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "1.0.0.0")]
public sealed class HeightfieldAsset : Asset
{
    private const string CurrentVersion = "1.0.0.0";
    public const string FileExtension = ".sdhf";

    /// <inheritdoc cref="Heightfield.Size" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public float Size
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    } = 1024;

    /// <inheritdoc cref="Heightfield.Subdivision" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public int Subdivision
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    } = 2048;

    /// <inheritdoc cref="HeightfieldShape.CoarseBlockInterval" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public int CoarseBlockInterval
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    } = 64;

    /// <summary> The composition of the heightfield </summary>
    public required IHeightfieldLayerBuilder? Layer { get; init; }
}
