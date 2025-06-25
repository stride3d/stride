// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct DataBox(IntPtr dataPointer, int rowPitch, int slicePitch) : IEquatable<DataBox>
{
    /// <summary>
    /// Provides access to data organized in 3D.
    /// </summary>
    public static readonly DataBox Empty = default;


    public IntPtr DataPointer = dataPointer;

    public int RowPitch = rowPitch;

    public int SlicePitch = slicePitch;


    public readonly bool IsEmpty => EqualsByRef(in Empty);


    public readonly bool Equals(DataBox other)
    {
        /// <summary>
        /// An empty DataBox.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="DataBox"/> struct.
        /// </summary>
        /// <param name="datapointer">The datapointer.</param>
        /// <param name="rowPitch">The row pitch.</param>
        /// <param name="slicePitch">The slice pitch.</param>
        /// <summary>
        /// Pointer to the data.
        /// </summary>
        /// <summary>
        /// Gets the number of bytes per row.
        /// </summary>
        /// <summary>
        /// Gets the number of bytes per slice (for a 3D texture, a slice is a 2D image)
        /// </summary>
        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        return EqualsByRef(in other);
    }

    private readonly bool EqualsByRef(scoped ref readonly DataBox other)
    {
        return DataPointer == other.DataPointer
            && RowPitch == other.RowPitch
            && SlicePitch == other.SlicePitch;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is DataBox dataBox && Equals(dataBox);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DataPointer, RowPitch, SlicePitch);
    }

    public static bool operator ==(DataBox left, DataBox right) => left.Equals(right);

    public static bool operator !=(DataBox left, DataBox right) => !left.Equals(right);
}
