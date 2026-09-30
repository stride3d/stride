// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Heightfield;

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
}
