// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Heightfield;

/// <summary>
/// The definition for an <see cref="HeightfieldShape"/>
/// </summary>
/// <example>
/// See Stride.Heightfield.Assets.Heightfield in Stride.Assets for an example implementation
/// </example>
public interface IHeightfieldSource
{
    /// <inheritdoc cref="HeightfieldShape.Subdivision"/>
    float Size { get; }

    /// <inheritdoc cref="HeightfieldShape.Subdivision"/>
    int Subdivision { get; }

    /// <inheritdoc cref="HeightfieldShape.MinHeight"/>
    float MinHeight { get; }

    /// <inheritdoc cref="HeightfieldShape.MaxHeight"/>
    float MaxHeight { get; }
}
