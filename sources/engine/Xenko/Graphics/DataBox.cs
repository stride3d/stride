// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace Xenko.Graphics
{
    /// <summary>
    /// Provides access to data organized in 3D.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DataBox : IEquatable<DataBox>
    {
        /// <summary>
        /// An empty DataBox.
        /// </summary>
        private static DataBox empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBox"/> struct.
        /// </summary>
        /// <param name="datapointer">The datapointer.</param>
        /// <param name="rowPitch">The row pitch.</param>
        /// <param name="slicePitch">The slice pitch.</param>
        public DataBox(IntPtr datapointer, int rowPitch, int slicePitch)
        {
            DataPointer = datapointer;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        /// <summary>
        /// Pointer to the data.
        /// </summary>
        public IntPtr DataPointer;

        /// <summary>
        /// Gets the number of bytes per row.
        /// </summary>
        public int RowPitch;

        /// <summary>
        /// Gets the number of bytes per slice (for a 3D texture, a slice is a 2D image)
        /// </summary>
        public int SlicePitch;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty
        {
            get
            {
                return EqualsByRef(ref empty);
            }
        }

        public bool Equals(DataBox other)
        {
            return EqualsByRef(ref other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DataBox && Equals((DataBox)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DataPointer.GetHashCode();
                hashCode = (hashCode * 397) ^ RowPitch;
                hashCode = (hashCode * 397) ^ SlicePitch;
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DataBox left, DataBox right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DataBox left, DataBox right)
        {
            return !left.Equals(right);
        }

        private bool EqualsByRef(ref DataBox other)
        {
            return DataPointer.Equals(other.DataPointer) && RowPitch == other.RowPitch && SlicePitch == other.SlicePitch;
        }
    }
}
