// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Heightfield;

/// <summary>
/// The base definition for a Heightfield
/// </summary>
/// <example>
/// See Stride.Heightfield.Assets.Heightfield in Stride.Assets for an example implementation
/// </example>
public interface IHeightfieldSource
{
    /// <summary>
    /// How large the heightfield is in units, 10 would occupy an area of 10^2 units
    /// </summary>
    float Size { get; }

    /// <summary>
    /// How many individual height samples the heightfield is made up of on one axis,
    /// amount of samples in total would be <see cref="Subdivision"/>^2.
    /// </summary>
    /// <remarks>
    /// Note that the amount of quads along an axis would be (<see cref="Subdivision"/>-1) as each quad bridges between two samples.
    /// </remarks>
    int Subdivision { get; }

    /// <summary>
    /// The rock bottom, or lower bounds of this field. Assumed to be constant. May be negative
    /// </summary>
    float MinHeight { get; }

    /// <summary>
    /// The apex, or the highest bounds of this field. Assumed to be constant. May be negative
    /// </summary>
    float MaxHeight { get; }
}
