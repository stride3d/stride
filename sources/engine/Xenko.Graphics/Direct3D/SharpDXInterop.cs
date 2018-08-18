// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11 || XENKO_GRAPHICS_API_DIRECT3D12

using SharpDX;
#if XENKO_GRAPHICS_API_DIRECT3D11
using SharpDX.Direct3D11;
#elif XENKO_GRAPHICS_API_DIRECT3D12
using SharpDX.Direct3D12;
#endif

namespace Xenko.Graphics
{
    public static class SharpDXInterop
    {
        /// <summary>
        /// Gets the native device (DX11/DX12)
        /// </summary>
        /// <param name="device">The Xenko GraphicsDevice</param>
        /// <returns></returns>
        public static object GetNativeDevice(GraphicsDevice device)
        {
            return GetNativeDeviceImpl(device);
        }

        /// <summary>
        /// Gets the native device context (DX11)
        /// </summary>
        /// <param name="device">The Xenko GraphicsDevice</param>
        /// <returns></returns>
        public static object GetNativeDeviceContext(GraphicsDevice device)
        {
            return GetNativeDeviceContextImpl(device);
        }

        /// <summary>
        /// Gets the native command queue (DX12 only)
        /// </summary>
        /// <param name="device">The Xenko GraphicsDevice</param>
        /// <returns></returns>
        public static object GetNativeCommandQueue(GraphicsDevice device)
        {
            return GetNativeCommandQueueImpl(device);
        }

        /// <summary>
        /// Gets the DX11 native resource handle
        /// </summary>
        /// <param name="resource">The Xenko GraphicsResourceBase</param>
        /// <returns></returns>
        public static object GetNativeResource(GraphicsResource resource)
        {
            return GetNativeResourceImpl(resource);
        }

        public static object GetNativeShaderResourceView(GraphicsResource resource)
        {
            return GetNativeShaderResourceViewImpl(resource);
        }

        public static object GetNativeRenderTargetView(Texture texture)
        {
            return GetNativeRenderTargetViewImpl(texture);
        }

        /// <summary>
        /// Creates a Texture from a DirectX11 native texture
        /// This method internally will call AddReference on the dxTexture2D texture.
        /// </summary>
        /// <param name="device">The GraphicsDevice in use</param>
        /// <param name="dxTexture2D">The DX11 texture</param>
        /// <param name="takeOwnership">If false AddRef will be called on the texture, if true will not, effectively taking ownership</param>
        /// <returns></returns>
        public static Texture CreateTextureFromNative(GraphicsDevice device, object dxTexture2D, bool takeOwnership)
        {
#if XENKO_GRAPHICS_API_DIRECT3D11
            return CreateTextureFromNativeImpl(device, (Texture2D)dxTexture2D, takeOwnership);
#elif XENKO_GRAPHICS_API_DIRECT3D12
            return CreateTextureFromNativeImpl(device, (Resource)dxTexture2D, takeOwnership);
#endif
        }

#if XENKO_GRAPHICS_API_DIRECT3D11
        /// <summary>
        /// Gets the DX11 native device
        /// </summary>
        /// <param name="device">The Xenko GraphicsDevice</param>
        /// <returns></returns>
        private static Device GetNativeDeviceImpl(GraphicsDevice device)
        {
            return device.NativeDevice;
        }

        private static DeviceContext GetNativeDeviceContextImpl(GraphicsDevice device)
        {
            return device.NativeDeviceContext;
        }

        private static object GetNativeCommandQueueImpl(GraphicsDevice device)
        {
            return null;
        }

        /// <summary>
        /// Gets the DX11 native resource handle
        /// </summary>
        /// <param name="resource">The Xenko GraphicsResourceBase</param>
        /// <returns></returns>
        private static Resource GetNativeResourceImpl(GraphicsResource resource)
        {
            return resource.NativeResource;
        }

        private static ShaderResourceView GetNativeShaderResourceViewImpl(GraphicsResource resource)
        {
            return resource.NativeShaderResourceView;
        }

        private static RenderTargetView GetNativeRenderTargetViewImpl(Texture texture)
        {
            return texture.NativeRenderTargetView;
        }

        /// <summary>
        /// Creates a Texture from a DirectX11 native texture
        /// This method internally will call AddReference on the dxTexture2D texture.
        /// </summary>
        /// <param name="device">The GraphicsDevice in use</param>
        /// <param name="dxTexture2D">The DX11 texture</param>
        /// <param name="takeOwnership">If false AddRef will be called on the texture, if true will not, effectively taking ownership</param>
        /// <returns></returns>
        private static Texture CreateTextureFromNativeImpl(GraphicsDevice device, Texture2D dxTexture2D, bool takeOwnership)
        {
            var tex = new Texture(device);

            if (takeOwnership)
            {
                var unknown = dxTexture2D as IUnknown;
                unknown.AddReference();
            }

            tex.InitializeFromImpl(dxTexture2D, false);

            return tex;
        }

#elif XENKO_GRAPHICS_API_DIRECT3D12
        /// <summary>
        /// Gets the DX11 native device
        /// </summary>
        /// <param name="device">The Xenko GraphicsDevice</param>
        /// <returns></returns>
        private static Device GetNativeDeviceImpl(GraphicsDevice device)
        {
            return device.NativeDevice;
        }

        private static object GetNativeDeviceContextImpl(GraphicsDevice device)
        {
            return null;
        }

        private static CommandQueue GetNativeCommandQueueImpl(GraphicsDevice device)
        {
            return device.NativeCommandQueue;
        }

        /// <summary>
        /// Gets the DX11 native resource handle
        /// </summary>
        /// <param name="resource">The Xenko GraphicsResourceBase</param>
        /// <returns></returns>
        private static Resource GetNativeResourceImpl(GraphicsResource resource)
        {
            return resource.NativeResource;
        }

        private static CpuDescriptorHandle GetNativeShaderResourceViewImpl(GraphicsResource resource)
        {
            return resource.NativeShaderResourceView;
        }

        private static CpuDescriptorHandle GetNativeRenderTargetViewImpl(Texture texture)
        {
            return texture.NativeRenderTargetView;
        }

        /// <summary>
        /// Creates a Texture from a DirectX11 native texture
        /// This method internally will call AddReference on the dxTexture2D texture.
        /// </summary>
        /// <param name="device">The GraphicsDevice in use</param>
        /// <param name="dxTexture2D">The DX11 texture</param>
        /// <param name="takeOwnership">If false AddRef will be called on the texture, if true will not, effectively taking ownership</param>
        /// <returns></returns>
        private static Texture CreateTextureFromNativeImpl(GraphicsDevice device, Resource dxTexture2D, bool takeOwnership)
        {
            var tex = new Texture(device);

            if (takeOwnership)
            {
                var unknown = dxTexture2D as IUnknown;
                unknown.AddReference();
            }

            tex.InitializeFromImpl(dxTexture2D, false);

            return tex;
        }
#endif
    }
}

#endif
