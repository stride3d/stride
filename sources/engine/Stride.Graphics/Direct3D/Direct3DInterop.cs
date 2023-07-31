// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Direct3D11;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using Silk.NET.Direct3D12;
#endif

namespace Stride.Graphics
{
    public static unsafe class Direct3DInterop
    {
        //public static ref SharpDX.DataBox AsSharpDX(ref this DataBox @this) => ref Unsafe.As<DataBox, SharpDX.DataBox>(ref @this);
        //public static ref Silk.NET.Direct3D11.ResourceRegion AsSharpDX(ref this ResourceRegion @this) => ref Unsafe.As<ResourceRegion, Silk.NET.Direct3D11.ResourceRegion>(ref @this);
        //public static ref DataBox AsStride(ref this SharpDX.DataBox @this) => ref Unsafe.As<SharpDX.DataBox, DataBox>(ref @this);
        //public static ref ResourceRegion AsStride(ref this Silk.NET.Direct3D11.ResourceRegion @this) => ref Unsafe.As<Silk.NET.Direct3D11.ResourceRegion, ResourceRegion>(ref @this);

#if STRIDE_GRAPHICS_API_DIRECT3D11
        /// <summary>
        ///   Gets the underlying Direct3D 11 native device.
        /// </summary>
        /// <param name="device">The Stride graphics device.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D11Device"/> instance representing the native Direct3D 11 device.
        /// </returns>
        public static ID3D11Device* GetNativeDevice(GraphicsDevice device) => device.NativeDevice;

        /// <summary>
        ///   Gets the underlying Direct3D 11 native device context.
        /// </summary>
        /// <param name="device">The Stride graphics device.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D11DeviceContext"/> instance representing the native Direct3D 11 command queue.
        /// </returns>
        public static ID3D11DeviceContext* GetNativeDeviceContext(GraphicsDevice device) => device.NativeDeviceContext;

        /// <summary>
        ///   Gets the underlying Direct3D 11 native resource.
        /// </summary>
        /// <param name="resource">The Stride graphics resource.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D11Resource"/> instance representing the native Direct3D 11 resource.
        /// </returns>
        public static ID3D11Resource* GetNativeResource(GraphicsResource resource) => resource.NativeResource;

        /// <summary>
        ///   Gets the underlying Direct3D 11 native shader resource view.
        /// </summary>
        /// <param name="resource">The Stride graphics resource.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D11ShaderResourceView"/> instance representing the native Direct3D 11
        ///   shader resource view on the resource.
        /// </returns>
        public static ID3D11ShaderResourceView* GetNativeShaderResourceView(GraphicsResource resource) => resource.NativeShaderResourceView;

        /// <summary>
        ///   Gets the underlying Direct3D 11 native render target view.
        /// </summary>
        /// <param name="resource">The Stride texture.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D11RenderTargetView"/> instance representing the native Direct3D 11
        ///   render target view on the texture.
        /// </returns>
        public static ID3D11RenderTargetView* GetNativeRenderTargetView(Texture texture) => texture.NativeRenderTargetView;

        /// <summary>
        ///   Creates a <see cref="Texture"/> from a Direct3D 11 texture.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> in use.</param>
        /// <param name="dxTexture2D">The Direct3D 11 texture.</param>
        /// <param name="takeOwnership">
        ///   <see langword="false"/> to call <see cref="ID3D11Resource.AddRef"/> on the texture;
        ///   <see langword="true"/> to not call it, effectively taking ownership of the texture.</param>
        /// <param name="isSRgb">A value indicating whether to set the format of the texture to sRGB.</param>
        /// <returns>A Stride <see cref="Texture"/>.</returns>
        public static Texture CreateTextureFromNative(GraphicsDevice device, ID3D11Texture2D* dxTexture2D, bool takeOwnership, bool isSRgb = false)
        {
            var texture = new Texture(device);

            if (takeOwnership)
            {
                dxTexture2D->AddRef();
            }

            texture.InitializeFromImpl(dxTexture2D, isSRgb);

            return texture;
        }

#elif STRIDE_GRAPHICS_API_DIRECT3D12
        /// <summary>
        ///   Gets the underlying Direct3D 12 native device.
        /// </summary>
        /// <param name="device">The Stride graphics device.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D12Device"/> instance representing the native Direct3D 12 device.
        /// </returns>
        public static ID3D12Device* GetNativeDevice(GraphicsDevice device) => device.NativeDevice;

        /// <summary>
        ///   Gets the underlying Direct3D 12 native command queue.
        /// </summary>
        /// <param name="device">The Stride graphics device.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D12CommandQueue"/> instance representing the native Direct3D 12 command queue.
        /// </returns>
        public static ID3D12CommandQueue* GetNativeCommandQueue(GraphicsDevice device) => device.NativeCommandQueue;

        /// <summary>
        ///   Gets the underlying Direct3D 12 native resource.
        /// </summary>
        /// <param name="resource">The Stride graphics resource.</param>
        /// <returns>
        ///   A pointer to a <see cref="ID3D12Resource"/> instance representing the native Direct3D 12 resource.
        /// </returns>
        public static ID3D12Resource* GetNativeResource(GraphicsResource resource) => resource.NativeResource;

        /// <summary>
        ///   Gets the underlying Direct3D 12 native shader resource view CPU-accessible handle.
        /// </summary>
        /// <param name="resource">The Stride graphics resource.</param>
        /// <returns>
        ///   A <see cref="CpuDescriptorHandle"/> representing the native Direct3D 12 handle.
        /// </returns>
        public static CpuDescriptorHandle GetNativeShaderResourceView(GraphicsResource resource) => resource.NativeShaderResourceView;

        /// <summary>
        ///   Gets the underlying Direct3D 12 native render target view CPU-accessible handle.
        /// </summary>
        /// <param name="resource">The Stride texture.</param>
        /// <returns>
        ///   A <see cref="CpuDescriptorHandle"/> representing the native Direct3D 12 handle.
        /// </returns>
        public static CpuDescriptorHandle GetNativeRenderTargetView(Texture texture) => texture.NativeRenderTargetView;

        /// <summary>
        ///   Creates a <see cref="Texture"/> from a Direct3D 12 texture resource.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> in use.</param>
        /// <param name="dxTexture2D">The Direct3D 12 texture resource.</param>
        /// <param name="takeOwnership">
        ///   <see langword="false"/> to call <see cref="ID3D12Resource.AddRef"/> on the texture;
        ///   <see langword="true"/> to not call it, effectively taking ownership of the resource.</param>
        /// <param name="isSRgb">A value indicating whether to set the format of the texture to sRGB.</param>
        /// <returns>A Stride <see cref="Texture"/>.</returns>
        public static Texture CreateTextureFromNative(GraphicsDevice device, ID3D12Resource* dxTexture2D, bool takeOwnership, bool isSRgb = false)
        {
            var texture = new Texture(device);

            if (takeOwnership)
            {
                dxTexture2D->AddRef();
            }

            texture.InitializeFromImpl(dxTexture2D, isSRgb);

            return texture;
        }
#endif
    }
}

#endif
