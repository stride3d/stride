// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL 

using System;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
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
