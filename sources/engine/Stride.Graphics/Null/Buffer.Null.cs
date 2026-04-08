// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

using System;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        /// <summary>
        ///   Initializes this <see cref="Buffer"/> instance with the provided options.
        /// </summary>
        /// <param name="description">A <see cref="BufferDescription"/> structure describing the buffer characteristics.</param>
        /// <param name="viewFlags">A combination of flags determining how the Views over this buffer should behave.</param>
        /// <param name="viewFormat">
        ///   View format used if the buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="dataPointer">The data pointer to the data to initialize the buffer with.</param>
        /// <returns>This same instance of <see cref="Buffer"/> already initialized.</returns>
        /// <exception cref="ArgumentException">Element size (<c>StructureByteStride</c>) must be greater than zero for Structured Buffers.</exception>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            ViewFlags = viewFlags;
            ViewFormat = viewFormat;
            Recreate(dataPointer);

            NullHelper.ToImplement();

            return this;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <param name="dataPointer">The data Pointer</param>
        public void Recreate(IntPtr dataPointer)
        {
        }
    }
}

#endif
