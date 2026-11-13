// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace Stride.Heightfield.Assets;

/// <summary>
/// Heightfield as a plane set to a fixed height
/// </summary>
[DataContract]
[Display("Fixed Height")]
public record FixedHeight : IHeightfieldLayerBuilder
{
    public float Height { get; set; } = 10;

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        yield break;
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer runtimeLayer, out float minHeight, out float maxHeight)
    {
        runtimeLayer = new RuntimeLayer.FixedHeightRuntimeLayer
        {
            Height = Height,
        };
        minHeight = Height;
        maxHeight = Height;
    }
}
