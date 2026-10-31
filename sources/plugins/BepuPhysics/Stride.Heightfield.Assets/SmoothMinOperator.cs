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
/// Compute the smooth minimum between two heightfields 
/// </summary>
[DataContract]
[Display("Smooth Min")]
public class SmoothMinOperator : IHeightfieldLayerBuilder
{
    public required IHeightfieldLayerBuilder? A, B;

    public float Smoothness = 4;

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        foreach (var heightfieldLayer in new[] { A, B })
        {
            foreach (var file in heightfieldLayer?.GetInputFiles() ?? [])
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer output, out float minHeight, out float maxHeight)
    {
        IHeightfieldRuntimeLayer? aOutput = null, bOutput = null;
        float aMinHeight = 0, bMinHeight = 0;
        float aMaxHeight = 0, bMaxHeight = 0;
        A?.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out aOutput, out aMinHeight, out aMaxHeight);
        B?.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out bOutput, out bMinHeight, out bMaxHeight);

        minHeight = float.Min(aMinHeight, bMinHeight);
        maxHeight = float.Max(aMaxHeight, bMaxHeight);

        output = new RuntimeLayer.SmoothMinRuntimeLayer
        {
            A = aOutput,
            B = bOutput,
            Smoothness = Smoothness
        };
    }
}
