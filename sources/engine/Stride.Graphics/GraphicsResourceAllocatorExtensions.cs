// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    /// Extensions for the <see cref="GraphicsResourceAllocator"/>.
    /// </summary>
    public static class GraphicsResourceAllocatorExtensions
    {
        /// <summary>
        /// Gets a <see cref="Texture" /> output for the specified description.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        /// <param name="description">The description.</param>
        /// <returns>A new instance of <see cref="Texture" /> class.</returns>
        public static Texture GetTemporaryTexture2D(this GraphicsResourceAllocator allocator, TextureDescription description)
        {
            return allocator.GetTemporaryTexture(description);
        }

        /// <summary>
        /// Gets a <see cref="Texture" /> output for the specified description with a single mipmap.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <returns>A new instance of <see cref="Texture" /> class.</returns>
        /// <msdn-id>ff476521</msdn-id>
        /// <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>
        /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
        public static Texture GetTemporaryTexture2D(this GraphicsResourceAllocator allocator, int width, int height, PixelFormat format, TextureFlags flags = TextureFlags.RenderTarget | TextureFlags.ShaderResource, int arraySize = 1)
        {
            return allocator.GetTemporaryTexture(TextureDescription.New2D(width, height, 1, format, flags, arraySize));
        }

        /// <summary>
        /// Gets a <see cref="Texture" /> output for the specified description.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int &gt;=1 for a particular mipmap count.</param>
        /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <returns>A new instance of <see cref="Texture" /> class.</returns>
        /// <msdn-id>ff476521</msdn-id>
        /// <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>
        /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
        public static Texture GetTemporaryTexture2D(this GraphicsResourceAllocator allocator, int width, int height, PixelFormat format, MipMapCount mipCount, TextureFlags flags = TextureFlags.RenderTarget | TextureFlags.ShaderResource, int arraySize = 1)
        {
            return allocator.GetTemporaryTexture(TextureDescription.New2D(width, height, mipCount, format, flags, arraySize));
        }        
    }
}
