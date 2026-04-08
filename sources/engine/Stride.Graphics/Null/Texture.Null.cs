// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
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

        /// <summary>
        ///   Swaps the Texture's internal data with another Texture.
        /// </summary>
        /// <param name="other">The other Texture.</param>
        internal partial void SwapInternal(Texture other)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Initializes the Texture from the specified data.
        /// </summary>
        /// <param name="dataBoxes">
        ///   An array of <see cref="DataBox"/> structures pointing to the data for all the subresources to
        ///   initialize for the Texture.
        /// </param>
        private partial void InitializeFromImpl(DataBox[] dataBoxes)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Perform platform-specific recreation of the Texture.
        /// </summary>
        private partial void OnRecreateImpl()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Indicates if the Texture is flipped vertically, i.e. if the rows are ordered bottom-to-top instead of top-to-bottom.
        /// </summary>
        /// <returns><see langword="true"/> if the Texture is flipped; <see langword="false"/> otherwise.</returns>
        private partial bool IsFlipped()
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
