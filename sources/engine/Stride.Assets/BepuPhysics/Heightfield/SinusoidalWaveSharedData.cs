// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// Data shared between the compile-time and runtime representation of the <see cref="SinusoidalWave"/>
/// </summary>
[DataContract]
public record SinusoidalWaveSharedData
{
    /// <summary>
    /// Multiplier over the height of the wave,
    /// the wave's height will span a [-<see cref="HeightMultiplier"/>, <see cref="HeightMultiplier"/>] range
    /// </summary>
    public float HeightMultiplier { get; set; } = 10;

    /// <summary>
    /// The amount of waves on each axis stretched over the whole <see cref="Heightfield"/>
    /// </summary>
    public Vector2 Tiling { get; set; } = new(100);
}
