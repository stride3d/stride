// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
///   Defines a region of data organized in 3D.
/// </summary>
/// <param name="dataPointer">A pointer to the data.</param>
/// <param name="rowPitch">The number of bytes per row of the data.</param>
/// <param name="slicePitch">The number of bytes per slice of the data (for a 3D Texture, a slice is a 2D image).</param>
[StructLayout(LayoutKind.Sequential)]
public struct DataBox(IntPtr dataPointer, int rowPitch, int slicePitch) : IEquatable<DataBox>
{
    /// <summary>
    ///   An empty <see cref="DataBox"/>.
    /// </summary>
    public static readonly DataBox Empty = default;


    /// <summary>
    ///   A pointer to the data.
    /// </summary>
    public IntPtr DataPointer = dataPointer;

    /// <summary>
    ///   The number of bytes per row of the data.
    /// </summary>
    public int RowPitch = rowPitch;

    /// <summary>
    ///   The number of bytes per slice of the data (for a 3D Texture, a slice is a 2D image).
    /// </summary>
    public int SlicePitch = slicePitch;


    /// <summary>
    ///   Gets a value indicating whether this data box is empty.
    /// </summary>
    /// <value><see langword="true"/> if this instance is empty; otherwise, <see langword="false"/>.</value>
    public readonly bool IsEmpty => EqualsByRef(in Empty);


    /// <inheritdoc/>
    public readonly bool Equals(DataBox other)
    {
        return EqualsByRef(in other);
    }

    private readonly bool EqualsByRef(scoped ref readonly DataBox other)
    {
        return DataPointer == other.DataPointer
            && RowPitch == other.RowPitch
            && SlicePitch == other.SlicePitch;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is DataBox dataBox && Equals(dataBox);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DataPointer, RowPitch, SlicePitch);
    }

    public static bool operator ==(DataBox left, DataBox right) => left.Equals(right);

    public static bool operator !=(DataBox left, DataBox right) => !left.Equals(right);
}
