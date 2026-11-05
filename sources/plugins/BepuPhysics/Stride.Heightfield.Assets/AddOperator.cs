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
/// Concat multiple heightfields together additively 
/// </summary>
[DataContract]
[Display("Add")]
public class AddOperator : IHeightfieldLayerBuilder
{
    /// <summary>
    /// Each heightfield listed contributes additively to the final height 
    /// </summary>
    public readonly List<IHeightfieldLayerBuilder> Layers = new();

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        foreach (var heightfieldLayer in Layers)
        {
            foreach (var file in heightfieldLayer.GetInputFiles())
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer output, out float minHeight, out float maxHeight)
    {
        var runtime = new RuntimeLayer.AddRuntimeLayer();
        minHeight = 0;
        maxHeight = 0;
        foreach (var layer in Layers)
        {
            layer.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out output, out var thisMinHeight, out var thisMaxHeight);
            minHeight += thisMinHeight;
            maxHeight += thisMaxHeight;
            runtime.Layers.Add(output);
        }

        output = runtime;
    }
}
