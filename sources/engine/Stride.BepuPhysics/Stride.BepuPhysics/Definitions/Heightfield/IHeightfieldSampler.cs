// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions.Heightfield;

/// <summary>
/// The datatype that holds or computes sample's heights for an <see cref="HeightfieldShape"/>
/// </summary>
public interface IHeightfieldSampler
{
    /// <summary>
    /// Samples the heightfield at the coordinates specified in <see cref="Sample.SampleCoord"/>
    /// and writes the value to <see cref="Sample.Height"/>
    /// </summary>
    /// <remarks>
    /// A given <see cref="Sample.SampleCoord"/> is not expected to output a constant <see cref="Sample.Height"/> value,
    /// but it must be constant within a physics tick.
    /// May be called hundreds of times per physics tick, must be especially fast.
    /// </remarks>
    void FillSamples(Span<Sample> samples);
}
