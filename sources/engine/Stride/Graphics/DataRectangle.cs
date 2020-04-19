// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides a pointer to 2D data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DataRectangle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataRectangle"/> class.
        /// </summary>
        /// <param name="dataPointer">The pointer to the data.</param>
        /// <param name="pitch">The stride.</param>
        public DataRectangle(IntPtr dataPointer, int pitch)
        {
            DataPointer = dataPointer;
            Pitch = pitch;
        }

        /// <summary>
        /// Gets or sets a pointer to the data.
        /// </summary>
        /// <value>
        /// The stream.
        /// </value>
        public IntPtr DataPointer;

        /// <summary>
        /// Gets or sets the number of bytes per row.
        /// </summary>
        /// <value>
        /// The row pitch in bytes.
        /// </value>
        public int Pitch;
    }
}
