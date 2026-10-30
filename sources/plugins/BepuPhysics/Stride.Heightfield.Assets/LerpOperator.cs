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
/// Interpolates between two heightfields 
/// </summary>
[DataContract]
[Display("Lerp")]
public class LerpOperator : IHeightfieldLayerBuilder
{
    public required IHeightfieldLayerBuilder? Black, White, BlendFactor;

    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        foreach (var heightfieldLayer in new[] { Black, White, BlendFactor })
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
        IHeightfieldRuntimeLayer? blackOutput = null, whiteOutput = null, blendFactorOutput = null;
        float blackMinHeight = 0, whiteMinHeight = 0, blendFactorMin = 0;
        float blackMaxHeight = 0, whiteMaxHeight = 0, blendFactorMax = 0;
        Black?.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out blackOutput, out blackMinHeight, out blackMaxHeight);
        White?.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out whiteOutput, out whiteMinHeight, out whiteMaxHeight);
        BlendFactor?.BuildRuntimeRepresentation(size, subdivision, commandContext, assetFinder, out blendFactorOutput, out blendFactorMin, out blendFactorMax);

        // Those are blending factors not heights, we'll treat them as such
        blendFactorMin = float.ClampNative(blendFactorMin, 0, 1);
        blendFactorMax = float.ClampNative(blendFactorMax, 0, 1);

        // Lerp always results in a value within the range of all blacks and white values, minimum possible then is min(blacks, whites).
        // - If all blend factors are above 0 (blendFactorMin), then the minimum of blacks can't ever be reached entirely
        // - If all blend factors are below 1 (blendFactorMax), then the minimum of whites can't ever be reached entirely
        minHeight = float.Lerp(blackMinHeight, whiteMinHeight, blendFactorMin);
        minHeight = float.Min(minHeight, float.Lerp(blackMinHeight, whiteMinHeight, blendFactorMax));

        // Same idea for the max
        maxHeight = float.Lerp(blackMaxHeight, whiteMaxHeight, blendFactorMin);
        maxHeight = float.Max(maxHeight, float.Lerp(blackMaxHeight, whiteMaxHeight, blendFactorMax));

        output = new RuntimeLayer.LerpRuntimeLayer
        {
            Black = blackOutput,
            White = whiteOutput,
            BlendFactor = blendFactorOutput
        };
    }
}
