// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Graphics;

namespace Stride.Heightfield.Assets;

/// <summary>
/// Data shared between the compile-time and runtime representation of the <see cref="HeightfieldTexture"/>
/// </summary>
[DataContract]
public record HeightfieldTextureSharedData
{
    /// <summary>
    /// Input texture this Heightfield is built from, reads height from the red channel
    /// </summary>
    public required UrlReference<Texture> Texture { get; init; } = null!;

    /// <summary>
    /// Mutates the resulting height; <see cref="Texture"/>[x,y] * <see cref="HeightMultiplier"/>
    /// </summary>
    public float HeightMultiplier { get; init; } = 100;

    /// <summary>
    /// Scales the texture down.
    /// A value of one stretches the texture across the whole heightfield,
    /// two would repeat the texture twice on both axis
    /// </summary>
    public float Tiling { get; init; } = 1;

    /// <summary>
    /// Improves the resulting shape when the heightfield is denser than <see cref="Texture"/>,
    /// has some physics performance overhead.
    /// </summary>
    public bool BilinearSampling { get; init; } = true;
}
