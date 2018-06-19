// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_NULL 

namespace Xenko.Graphics
{
    /// <summary>
    /// Base class for texture resources.
    /// </summary>
    public partial class Texture
    {
        /// <summary>
        /// Size of texture in pixel.
        /// </summary>
        private int TexturePixelSize
        {
            get
            {
                NullHelper.ToImplement();
                return 16;
            }
        }

        private const int TextureSubresourceAlignment = 4;
        private const int TextureRowPitchAlignment = 1;

        internal bool HasStencil;

        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            NullHelper.ToImplement();
            return false;
        }

        internal void SwapInternal(Texture other)
        {
            NullHelper.ToImplement();
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            NullHelper.ToImplement();
        }

        private void OnRecreateImpl()
        {
            NullHelper.ToImplement();
        }

        private bool IsFlipped()
        {
            NullHelper.ToImplement();
            return false;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            NullHelper.ToImplement();
            return format;
        }
    }
} 
#endif
