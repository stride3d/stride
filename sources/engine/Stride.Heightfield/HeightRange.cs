// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Heightfield;

/// <summary>
/// Vertical bounds of a block of heightfield samples
/// </summary>
[DataContract]
public record struct HeightRange(float MinHeight, float MaxHeight)
{
    /// <inheritdoc cref="HeightfieldShape.MinHeight"/>
    public float MinHeight { get; set; } = MinHeight;

    /// <inheritdoc cref="HeightfieldShape.MaxHeight"/>
    public float MaxHeight { get; set; } = MaxHeight;

    /// <summary>
    /// Extract the range of a given block by collecting the samples it covers
    /// </summary>
    public static HeightRange ExtractRange(int blockIndex, int coarseBlocksSubdivision, int coarseBlockInterval, int subdivision, IHeightfieldSampler heightfieldFunction)
    {
        var range = new HeightRange(float.PositiveInfinity, float.NegativeInfinity);
        Span<Sample> samples = stackalloc Sample[1];
            
        Int2 block2D = new Int2(blockIndex % coarseBlocksSubdivision, blockIndex / coarseBlocksSubdivision);

        Int2 sampleCorner = block2D * coarseBlockInterval;
        Int2 sampleEnd = sampleCorner + new Int2(coarseBlockInterval);

        // We're defining block ranges as operating on N amount of samples, although the physics shape uses
        // them when evaluating triangles, those lay between samples.
        // If we specify three samples of coverage, e.g.: block0{s0, s1, s2}, block1{s3, s4, s5}
        // The triangle laying between s2 and s3 would not be covered by either blocks.
        // s2 would be the coordinate used when a position between s2 and s3 is tested,
        // meaning that the block to extend would be the one where s2 lives; b0.
        // Based on this fact, we know we have to extend the end of the blocks to the next sample over
        Int2 sampleCornerEndInclusive = Int2.Min(sampleEnd, new Int2(subdivision - 1));
        for (int sampleY = sampleCorner.Y; sampleY <= sampleCornerEndInclusive.Y; sampleY++)
        {
            for (int sampleX = sampleCorner.X; sampleX <= sampleCornerEndInclusive.X; sampleX++)
            {
                samples[0].SampleCoord = new Int2(sampleX, sampleY);
                heightfieldFunction.FillSamples(samples);
                range.MinHeight = Math.Min(range.MinHeight, samples[0].Height);
                range.MaxHeight = Math.Max(range.MaxHeight, samples[0].Height);
            }
        }

        return range;
    }
}
