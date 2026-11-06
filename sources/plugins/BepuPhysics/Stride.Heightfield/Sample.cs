// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Heightfield;

/// <summary>
/// A query passed to <see cref="IHeightfieldSampler"/> who will write the <see cref="Height"/> of the <see cref="SampleCoord"/> provided
/// </summary>
public record struct Sample
{
    /// <summary>
    /// A coordinate whose components lay in the range [0, <see cref="Heightfield.Subdivision"/>-1],
    /// implementors of <see cref="IHeightfieldSampler.FillSamples"/> will read this coordinate to evaluate and write the result to <see cref="Height"/>.
    /// </summary>
    public Int2 SampleCoord;

    /// <summary>
    /// The height of the sample at coordinate <see cref="SampleCoord"/>,
    /// will be assigned by implementors of <see cref="IHeightfieldSampler.FillSamples"/>.
    /// </summary>
    public float Height;

    /// <inheritdoc cref="Sample"/>
    public Sample(Int2 SampleCoord, float Height)
    {
        this.SampleCoord = SampleCoord;
        this.Height = Height;
    }
}
