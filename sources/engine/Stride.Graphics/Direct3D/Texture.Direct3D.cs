// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics
{
    public unsafe partial class Texture
    {
        private ID3D11RenderTargetView* renderTargetView;
        private ID3D11DepthStencilView* depthStencilView;

        internal bool HasStencil;

        private int TexturePixelSize => Format.SizeInBytes();
        private const int TextureRowPitchAlignment = 1;
        private const int TextureSubresourceAlignment = 1;

        /// <summary>
        ///   Gets the internal Direct3D 11 Depth-Stencil View attached to this Texture resource.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D11DepthStencilView> NativeDepthStencilView
        {
            get => ComPtrHelpers.ToComPtr(depthStencilView);

            private set
            {
                // Private use: We don't handle AddRef() or Release() here because the ComPtr is strictly assigned
                // and disposed by us.
                // If in the future we need this to be public/internal/protected and it can be assigned from other
                // places, this should manage the ComPtr lifetime appropriately.

                depthStencilView = value.Handle;

                if (IsDebugMode && depthStencilView is not null)
                {
                    NativeDepthStencilView.SetDebugName(Name is null ? null : $"{Name} DSV");
                }
            }
        }

        /// <summary>
        ///   Gets the internal Direct3D 11 Render Target View attached to this Texture resource.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D11RenderTargetView> NativeRenderTargetView
        {
            get => ComPtrHelpers.ToComPtr(renderTargetView);

            private set
            {
                // Private use: We don't handle AddRef() or Release() here because the ComPtr is strictly assigned
                // and disposed by us.
                // If in the future we need this to be public/internal/protected and it can be assigned from other
                // places, this should manage the ComPtr lifetime appropriately.

                renderTargetView = value.Handle;

                if (IsDebugMode && renderTargetView is not null)
                {
                    NativeRenderTargetView.SetDebugName(Name is null ? null : $"{Name} RTV");
                }
            }
        }


        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            return device.Features.HasDepthAsReadOnlyRT;
        }

        /// <summary>
        ///   Initializes from a native ID3D11Texture2D.
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(ID3D11Texture2D* texture, bool treatAsSrgb)
        {
            var ptrTexture = ComPtrHelpers.ToComPtr(texture);
            NativeDeviceChild = ptrTexture.AsDeviceChild();

            Unsafe.SkipInit(out Texture2DDesc textureDesc);
            ptrTexture.GetDesc(ref textureDesc);

            var newTextureDescription = ConvertFromNativeDescription(textureDesc);

            // We might have created the swapchain as a non-sRGB format (specially on Win 10 & RT) but we want it to
            // behave like it is (specially for the View and Render Target)
            if (treatAsSrgb)
                newTextureDescription.Format = newTextureDescription.Format.ToSRgb();

            return InitializeFrom(newTextureDescription);
        }

        /// <summary>
        ///   Initializes from a native ID3D11ShaderResourceView.
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(ID3D11ShaderResourceView* srv)
        {
            Unsafe.SkipInit(out ShaderResourceViewDesc srvDescription);
            srv->GetDesc(ref srvDescription);

            if (srvDescription.ViewDimension == D3DSrvDimension.D3D101SrvDimensionTexture2D)
            {
                NativeShaderResourceView = srv; // Implicit conversion to ComPtr calls AddRef()

                ID3D11Resource* resource;
                srv->GetResource(&resource);

                HResult result = resource->QueryInterface(out ComPtr<ID3D11Texture2D> texture);

                using var _ = texture; // Release the resource when done

                if (result.IsFailure)
                    result.Throw();

                NativeDeviceChild = texture.AsDeviceChild();

                Unsafe.SkipInit(out Texture2DDesc textureDesc);
                texture.GetDesc(ref textureDesc);

                var newTextureDescription = ConvertFromNativeDescription(textureDesc);
                var newTextureViewDescription = new TextureViewDescription
                {
                    Format = (PixelFormat) srvDescription.Format,
                    Flags = newTextureDescription.Flags
                };

                return InitializeFrom(parentTexture: null, newTextureDescription, newTextureViewDescription, textureDatas: null);
            }
            else
            {
                // TODO: Implement other view types?
                throw new NotImplementedException($"Creating a texture from a SRV with dimension {srvDescription.ViewDimension} is not implemented");
            }
        }

        internal partial void SwapInternal(Texture other)
        {
            (other.NativeDeviceChild, NativeDeviceChild) = (NativeDeviceChild, other.NativeDeviceChild);
            (other.NativeShaderResourceView, NativeShaderResourceView) = (NativeShaderResourceView, other.NativeShaderResourceView);
            (other.NativeUnorderedAccessView, NativeUnorderedAccessView) = (NativeUnorderedAccessView, other.NativeUnorderedAccessView);

            var rtv = renderTargetView;
            renderTargetView = other.renderTargetView;
            other.renderTargetView = rtv;

            var dsv = depthStencilView;
            depthStencilView = other.depthStencilView;
            other.depthStencilView = dsv;

            (HasStencil, other.HasStencil) = (other.HasStencil, HasStencil);
        }

        private void InitializeFromImpl() => InitializeFromImpl(null as DataBox[]);

        /// <summary>
        ///   Initializes the Texture from the specified data.
        /// </summary>
        private partial void InitializeFromImpl(DataBox[] dataBoxes)
        {
            if (ParentTexture is not null)
            {
                NativeDeviceChild = ParentTexture.NativeDeviceChild;
            }

            if (NativeDeviceChild.IsNull())
            {
                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                    {
                        ComPtr<ID3D11Texture1D> texture1D = CreateTexture1D(dataBoxes);
                        NativeDeviceChild = texture1D.AsDeviceChild();
                        break;
                    }
                    case TextureDimension.Texture2D:
                    case TextureDimension.TextureCube:
                    {
                        ComPtr<ID3D11Texture2D> texture2D = CreateTexture2D(dataBoxes);
                        NativeDeviceChild = texture2D.AsDeviceChild();
                        break;
                    }
                    case TextureDimension.Texture3D:
                    {
                        ComPtr<ID3D11Texture3D> texture3D = CreateTexture3D(dataBoxes);
                        NativeDeviceChild = texture3D.AsDeviceChild();
                        break;
                    }
                }

                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);
            }

            if (NativeShaderResourceView.Handle == null)
                NativeShaderResourceView = GetShaderResourceView(ViewType, ArraySlice, MipLevel);

            NativeUnorderedAccessView = GetUnorderedAccessView(ViewType, ArraySlice, MipLevel);
            NativeRenderTargetView = GetRenderTargetView(ViewType, ArraySlice, MipLevel);
            NativeDepthStencilView = GetDepthStencilView(out HasStencil);

            if (textureDescription.Options == TextureOptions.None)
            {
                SharedHandle = IntPtr.Zero;
            }
#if STRIDE_GRAPHICS_API_DIRECT3D11
            else if (textureDescription.Options.HasFlag(TextureOptions.SharedNtHandle) ||
                     textureDescription.Options.HasFlag(TextureOptions.SharedKeyedMutex))
            {
                HResult result = NativeDeviceChild.QueryInterface(out ComPtr<IDXGIResource1> sharedResource1);

                using var _ = sharedResource1; // Release the resource when done

                if (result.IsFailure)
                    result.Throw();

                var uniqueName = $"Stride:{Guid.NewGuid()}";

                void* sharedHandle;
                result = sharedResource1.CreateSharedHandle(pAttributes: (SecurityAttributes*) null, dwAccess: DxgiConstants.SharedAccessResourceWrite, uniqueName, &sharedHandle);

                if (result.IsFailure)
                    result.Throw();

                SharedHandle = new(sharedHandle);
                SharedNtHandleName = uniqueName;
            }
#endif
            else if (textureDescription.Options.HasFlag(TextureOptions.Shared))
            {
                HResult result = NativeDeviceChild.QueryInterface(out ComPtr<IDXGIResource> sharedResource);

                using var _ = sharedResource; // Release the resource when done

                if (result.IsFailure)
                    result.Throw();

                void* sharedHandle;
                result = sharedResource.GetSharedHandle(&sharedHandle);

                if (result.IsFailure)
                    result.Throw();

                SharedHandle = new(sharedHandle);
            }
            else
            {
                // Argument `textureDescription` comes from the constructor
                throw new ArgumentOutOfRangeException("textureDescription.Options");
            }

            //
            // Creates the internal Direct3D resource for a 1D Texture.
            //
            ComPtr<ID3D11Texture1D> CreateTexture1D(DataBox[] initialData)
            {
                ID3D11Texture1D* texture1D;

                Texture1DDesc description = ConvertToNativeDescription1D();
                ReadOnlySpan<SubresourceData> initiatDataPerSubresource = ConvertDataBoxes(initialData);

                HResult result = initiatDataPerSubresource.IsEmpty
                    ? NativeDevice.CreateTexture1D(in description, pInitialData: null, &texture1D)
                    : NativeDevice.CreateTexture1D(in description, in initiatDataPerSubresource[0], &texture1D);

                if (result.IsFailure)
                    result.Throw();

                return ComPtrHelpers.ToComPtr(texture1D);
            }

            //
            // Creates the internal Direct3D resource for a 2D Texture.
            //
            ComPtr<ID3D11Texture2D> CreateTexture2D(DataBox[] initialData)
            {
                ID3D11Texture2D* texture2D;

                Texture2DDesc description = ConvertToNativeDescription2D();
                ReadOnlySpan<SubresourceData> initiatDataPerSubresource = ConvertDataBoxes(initialData);

                HResult result = initiatDataPerSubresource.IsEmpty
                    ? NativeDevice.CreateTexture2D(in description, pInitialData: null, &texture2D)
                    : NativeDevice.CreateTexture2D(in description, in initiatDataPerSubresource[0], &texture2D);

                if (result.IsFailure)
                    result.Throw();

                return ComPtrHelpers.ToComPtr(texture2D);
            }

            //
            // Creates the internal Direct3D resource for a 3D Texture.
            //
            ComPtr<ID3D11Texture3D> CreateTexture3D(DataBox[] initialData)
            {
                ID3D11Texture3D* texture3D;

                Texture3DDesc description = ConvertToNativeDescription3D();
                ReadOnlySpan<SubresourceData> initiatDataPerSubresource = ConvertDataBoxes(initialData);

                HResult result = initiatDataPerSubresource.IsEmpty
                    ? NativeDevice.CreateTexture3D(in description, pInitialData: null, &texture3D)
                    : NativeDevice.CreateTexture3D(in description, in initiatDataPerSubresource[0], &texture3D);

                if (result.IsFailure)
                    result.Throw();

                return ComPtrHelpers.ToComPtr(texture3D);
            }
        }

        protected internal override void OnDestroyed()
        {
            // If it was a View, do not release reference, just forget it
            if (ParentTexture is not null)
            {
                NativeDeviceChild = null;
            }
            else
            {
                GraphicsDevice?.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            if (depthStencilView is not null)
                depthStencilView->Release();
            depthStencilView = null;

            if (renderTargetView is not null)
                renderTargetView->Release();
            renderTargetView = null;

            base.OnDestroyed();
        }

        private partial void OnRecreateImpl()
        {
            // Dependency: Wait for the underlying Texture to be recreated
            if (ParentTexture is { LifetimeState: not GraphicsResourceLifetimeState.Active })
                return;

            // Render Target / Depth Stencil are considered as "dynamic", i.e. nothing to reinitialize
            if (Usage is GraphicsResourceUsage.Immutable or GraphicsResourceUsage.Default &&
                !IsRenderTarget && !IsDepthStencil)
                return;

            if (ParentTexture is null)
            {
                GraphicsDevice?.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            InitializeFromImpl();
        }


        /// <summary>
        /// Gets a specific <see cref="ShaderResourceView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">The mip map slice index.</param>
        /// <returns>An <see cref="ShaderResourceView" /></returns>
        private ComPtr<ID3D11ShaderResourceView> GetShaderResourceView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return null;

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

            var srvDescription = new ShaderResourceViewDesc { Format = ComputeShaderResourceViewFormat() };

            // Initialize for Texture Array or Texture Cube
            if (ArraySize > 1)
            {
                if (ViewDimension == TextureDimension.TextureCube)
                {
                    srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexturecube;
                    srvDescription.TextureCube.MipLevels = (uint) mipCount;
                    srvDescription.TextureCube.MostDetailedMip = (uint) mipIndex;
                }
                else // Regular Texture Array
                {
                    if (IsMultiSampled)
                    {
                        if (Dimension != TextureDimension.Texture2D)
                        {
                            throw new NotSupportedException("Multi-sampling is only supported for 2D Textures");
                        }

                        srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Dmsarray;
                        srvDescription.Texture2DMSArray.ArraySize = (uint) arrayCount;
                        srvDescription.Texture2DMSArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                    }
                    else // Not multi-sampled
                    {
                        if (ViewDimension == TextureDimension.Texture2D)
                        {
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Darray;
                            srvDescription.Texture2DArray.ArraySize = (uint) arrayCount;
                            srvDescription.Texture2DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                            srvDescription.Texture2DArray.MipLevels = (uint) mipCount;
                            srvDescription.Texture2DArray.MostDetailedMip = (uint) mipIndex;
                        }
                        else if (ViewDimension != TextureDimension.Texture1D)
                        {
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture1Darray;
                            srvDescription.Texture1DArray.ArraySize = (uint) arrayCount;
                            srvDescription.Texture1DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                            srvDescription.Texture1DArray.MipLevels = (uint) mipCount;
                            srvDescription.Texture1DArray.MostDetailedMip = (uint) mipIndex;
                        }
                    }
                }
            }
            else // Not a Texture Array or Texture Cube
            {
                if (IsMultiSampled)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multi-sampling is only supported for 2D Textures");
                    }

                    srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Dms;
                }
                else // Not multi-sampled
                {
                    switch (ViewDimension)
                    {
                        case TextureDimension.Texture1D:
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture1D;
                            break;

                        case TextureDimension.Texture2D:
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2D;
                            break;

                        case TextureDimension.Texture3D:
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture3D;
                            break;

                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("Texture Cubes must have an array size greater than 1");
                    }
                    // Use srvDescription.Texture1D as it matches also Texture and Texture3D memory layout
                    srvDescription.Texture1D.MipLevels = (uint) mipCount;
                    srvDescription.Texture1D.MostDetailedMip = (uint) mipIndex;
                }
            }

            ID3D11ShaderResourceView* srv;
            HResult result = NativeDevice.CreateShaderResourceView(NativeResource, &srvDescription, &srv);

            if (result.IsFailure)
                result.Throw();

            return ComPtrHelpers.ToComPtr(srv);
        }

        /// <summary>
        /// Gets a specific <see cref="ID3D11RenderTargetView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="ID3D11RenderTargetView" /></returns>
        /// <exception cref="NotSupportedException">ViewSlice.MipBand is not supported for render targets</exception>
        private ComPtr<ID3D11RenderTargetView> GetRenderTargetView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return null;

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException($"{nameof(ViewType)}.{nameof(ViewType.MipBand)} is not supported for Render Targets");

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

            var rtvDescription = new RenderTargetViewDesc { Format = (Format) ViewFormat };

            // Initialize for Texture Array or Texture Cube
            if (ArraySize > 1)
            {
                if (MultisampleCount > MultisampleCount.None)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multi-sampling is only supported for 2D Textures");
                    }

                    rtvDescription.ViewDimension = RtvDimension.Texture2Dmsarray;
                    rtvDescription.Texture2DMSArray.ArraySize = (uint) arrayCount;
                    rtvDescription.Texture2DMSArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                }
                else // Not multi-sampled
                {
                    if (ViewDimension == TextureDimension.Texture3D)
                    {
                        throw new NotSupportedException("Texture Array is not supported for 3D Textures");
                    }

                    rtvDescription.ViewDimension = Dimension is TextureDimension.Texture2D or TextureDimension.TextureCube
                        ? RtvDimension.Texture2Darray
                        : RtvDimension.Texture1Darray;

                    // Use rtvDescription.Texture1DArray as it matches also Texture2DArray memory layout
                    rtvDescription.Texture1DArray.ArraySize = (uint) arrayCount;
                    rtvDescription.Texture1DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                    rtvDescription.Texture1DArray.MipSlice = (uint) mipIndex;
                }
            }
            else // Regular Texture Array
            {
                if (IsMultiSampled)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multi-sampling is only supported for 2D Render Target Textures");
                    }

                    rtvDescription.ViewDimension = RtvDimension.Texture2Dms;
                }
                else // Not multi-sampled
                {
                    switch (ViewDimension)
                    {
                        case TextureDimension.Texture1D:
                            rtvDescription.ViewDimension = RtvDimension.Texture1D;
                            rtvDescription.Texture1D.MipSlice = (uint) mipIndex;
                            break;

                        case TextureDimension.Texture2D:
                            rtvDescription.ViewDimension = RtvDimension.Texture2D;
                            rtvDescription.Texture2D.MipSlice = (uint) mipIndex;
                            break;

                        case TextureDimension.Texture3D:
                            rtvDescription.ViewDimension = RtvDimension.Texture3D;
                            rtvDescription.Texture3D.WSize = (uint) arrayCount;
                            rtvDescription.Texture3D.FirstWSlice = (uint) arrayOrDepthSlice;
                            rtvDescription.Texture3D.MipSlice = (uint) mipIndex;
                            break;

                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("Texture Cubes must have an array size greater than 1");
                    }
                }
            }

            ID3D11RenderTargetView* rtv;
            HResult result = NativeDevice.CreateRenderTargetView(NativeResource, &rtvDescription, &rtv);

            if (result.IsFailure)
                result.Throw();

            return ComPtrHelpers.ToComPtr(rtv);
        }

        /// <summary>
        /// Gets a specific <see cref="UnorderedAccessView" /> from this texture.
        /// </summary>
        /// <param name="viewType">The desired view type on the unordered resource</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="UnorderedAccessView" /></returns>
        private ComPtr<ID3D11UnorderedAccessView> GetUnorderedAccessView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsUnorderedAccess)
                return null;

            if (IsMultiSampled)
                throw new NotSupportedException("Multi-sampling is not supported for Unordered Access Views");

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out _);

            var uavDescription = new UnorderedAccessViewDesc { Format = (Format) ViewFormat };

            if (ArraySize > 1)
            {
                switch (ViewDimension)
                {
                    case TextureDimension.Texture1D:
                        uavDescription.ViewDimension = UavDimension.Texture1Darray;
                        break;

                    case TextureDimension.TextureCube:
                    case TextureDimension.Texture2D:
                        uavDescription.ViewDimension = UavDimension.Texture2Darray;
                        break;

                    case TextureDimension.Texture3D:
                        throw new NotSupportedException("Texture 3D is not supported for Texture Arrays");
                }

                uavDescription.Texture1DArray.ArraySize = (uint)arrayCount;
                uavDescription.Texture1DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                uavDescription.Texture1DArray.MipSlice = (uint) mipIndex;
            }
            else // Not a Texture Array or Texture Cube
            {
                switch (ViewDimension)
                {
                    case TextureDimension.Texture1D:
                        uavDescription.ViewDimension = UavDimension.Texture1D;
                        uavDescription.Texture1D.MipSlice = (uint)mipIndex;
                        break;

                    case TextureDimension.Texture2D:
                        uavDescription.ViewDimension = UavDimension.Texture2D;
                        uavDescription.Texture2D.MipSlice = (uint) mipIndex;
                        break;

                    case TextureDimension.Texture3D:
                        uavDescription.ViewDimension = UavDimension.Texture3D;
                        uavDescription.Texture3D.WSize = (uint) arrayCount;
                        uavDescription.Texture3D.FirstWSlice = (uint) arrayOrDepthSlice;
                        uavDescription.Texture3D.MipSlice = (uint) mipIndex;
                        break;

                    case TextureDimension.TextureCube:
                        throw new NotSupportedException("Texture Cubes must have an array size greater than 1");
                }
            }

            ID3D11UnorderedAccessView* uav;
            HResult result = NativeDevice.CreateUnorderedAccessView(NativeResource, &uavDescription, &uav);

            if (result.IsFailure)
                result.Throw();

            return ComPtrHelpers.ToComPtr(uav);
        }

        private ComPtr<ID3D11DepthStencilView> GetDepthStencilView(out bool hasStencil)
        {
            hasStencil = false;

            if (!IsDepthStencil)
                return null;

            // Check that the format is supported
            if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
                throw new NotSupportedException($"The Depth-Stencil format [{ViewFormat}] is not supported");

            // Setup the HasStencil flag
            hasStencil = IsStencilFormat(ViewFormat);

            // Create a Depth-Stencil View
            var dsvDescription = new DepthStencilViewDesc
            {
                Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                Flags = 0
            };

            if (ArraySize > 1)
            {
                dsvDescription.ViewDimension = DsvDimension.Texture2Darray;
                dsvDescription.Texture2DArray.ArraySize = (uint) ArraySize;
                dsvDescription.Texture2DArray.FirstArraySlice = 0;
                dsvDescription.Texture2DArray.MipSlice = 0;
            }
            else // Not a Texture Array or Texture Cube
            {
                dsvDescription.ViewDimension = DsvDimension.Texture2D;
                dsvDescription.Texture2D.MipSlice = 0;
            }

            if (MultisampleCount > MultisampleCount.None)
                dsvDescription.ViewDimension = DsvDimension.Texture2Dms;

            if (IsDepthStencilReadOnly)
            {
                if (!IsDepthStencilReadOnlySupported(GraphicsDevice))
                    throw new NotSupportedException("Cannot create a read-only Depth-Stencil View. Not supported on this device");

                dsvDescription.Flags = (uint) DsvFlag.Depth;
                if (HasStencil)
                    dsvDescription.Flags |= (uint) DsvFlag.Stencil;
            }

            ID3D11DepthStencilView* dsv;
            HResult result = NativeDevice.CreateDepthStencilView(NativeResource, &dsvDescription, &dsv);

            if (result.IsFailure)
                result.Throw();

            return ComPtrHelpers.ToComPtr(dsv);
        }

        private static BindFlag GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            BindFlag result = 0;

            if (flags.HasFlag(TextureFlags.ShaderResource))   result |= BindFlag.ShaderResource;
            if (flags.HasFlag(TextureFlags.RenderTarget))     result |= BindFlag.RenderTarget;
            if (flags.HasFlag(TextureFlags.UnorderedAccess))  result |= BindFlag.UnorderedAccess;
            if (flags.HasFlag(TextureFlags.DepthStencil))     result |= BindFlag.DepthStencil;

            return result;
        }

        private static unsafe ReadOnlySpan<SubresourceData> ConvertDataBoxes(ReadOnlySpan<DataBox> dataBoxes)
        {
            if (dataBoxes.IsEmpty)
                return default;

            // NOTE: This conversion works only IF the memory layout of DataBox matches that of SubresourceData
            Debug.Assert(sizeof(DataBox) == sizeof(SubresourceData));

            return dataBoxes.Cast<DataBox, SubresourceData>();
        }

        private partial bool IsFlipped()
        {
            return false;
        }

        private Texture1DDesc ConvertToNativeDescription1D()
        {
            var desc = new Texture1DDesc
            {
                Width = (uint) textureDescription.Width,
                ArraySize = 1,
                BindFlags = (uint) GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (Format) textureDescription.Format,
                MipLevels = (uint) textureDescription.MipLevelCount,
                Usage = (Usage) textureDescription.Usage,
                CPUAccessFlags = (uint) GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                MiscFlags = (uint) textureDescription.Options
            };
            return desc;
        }

        private Format ComputeShaderResourceViewFormat()
        {
            // Special case for Depth-Stencil Shader Resource Views that are bound as Float
            var viewFormat = IsDepthStencil
                ? (Format) ComputeShaderResourceFormatFromDepthFormat(ViewFormat)
                : (Format) ViewFormat;

            return viewFormat;
        }

        private static TextureDescription ConvertFromNativeDescription(Texture2DDesc description)
        {
            var desc = new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = (int) description.Width,
                Height = (int) description.Height,
                Depth = 1,
                MultisampleCount = (MultisampleCount) description.SampleDesc.Count,
                Format = (PixelFormat) description.Format,
                MipLevelCount = (int) description.MipLevels,
                Usage = (GraphicsResourceUsage) description.Usage,
                ArraySize = (int) description.ArraySize,
                Flags = TextureFlags.None,
                Options = TextureOptions.None
            };

            var bindFlags = (BindFlag) description.BindFlags;

            if (bindFlags.HasFlag(BindFlag.RenderTarget))     desc.Flags |= TextureFlags.RenderTarget;
            if (bindFlags.HasFlag(BindFlag.UnorderedAccess))  desc.Flags |= TextureFlags.UnorderedAccess;
            if (bindFlags.HasFlag(BindFlag.DepthStencil))     desc.Flags |= TextureFlags.DepthStencil;
            if (bindFlags.HasFlag(BindFlag.ShaderResource))   desc.Flags |= TextureFlags.ShaderResource;

            var miscFlags = (ResourceMiscFlag) description.MiscFlags;

            if (miscFlags.HasFlag(ResourceMiscFlag.Shared))
                desc.Options |= TextureOptions.Shared;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (miscFlags.HasFlag(ResourceMiscFlag.SharedKeyedmutex))
                desc.Options |= TextureOptions.SharedKeyedMutex;
            if (miscFlags.HasFlag(ResourceMiscFlag.SharedNthandle))
                desc.Options |= TextureOptions.SharedNtHandle;
#endif
            return desc;
        }

        private Texture2DDesc ConvertToNativeDescription2D()
        {
            var format = (Format) textureDescription.Format;
            var flags = textureDescription.Flags;

            // If the Texture is going to be bound as Depth-Stencil, use a typeless format
            if (IsDepthStencil)
            {
                if (IsShaderResource && GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                {
                    throw new NotSupportedException($"Shader Resource Views for Depth-Stencil Textures are not supported for Graphics profile < 10.0 (Current: [{GraphicsDevice.Features.CurrentProfile}])");
                }
                else
                {
                    // Determine a typeless format and a Shader Resource View Format
                    if (GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatD16Unorm,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatD32Float,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatD24UnormS8Uint,
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint,

                            _ => throw new NotSupportedException($"Unsupported Depth Format [{textureDescription.Format}] for the Depth-Stencil Buffer")
                        };
                    }
                    else // GraphicsProfile.Level_10_0 or higher
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatR16Typeless,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatR32Typeless,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatR24G8Typeless,//Silk.NET.DXGI.Format.FormatD24UnormS8Uint
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatR32G8X24Typeless,

                            _ => throw new NotSupportedException($"Unsupported Depth Format [{textureDescription.Format}] for the Depth-Stencil Buffer")
                        };
                    }
                }
            }

            int quality = 0;
            if (GraphicsDevice.Features.CurrentProfile >= GraphicsProfile.Level_10_1 && textureDescription.IsMultiSampled)
                quality = (int) StandardMultisampleQualityLevels.StandardMultisamplePattern;

            var desc = new Texture2DDesc
            {
                Width = (uint) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                ArraySize = (uint) textureDescription.ArraySize,
                SampleDesc = new SampleDesc((uint) textureDescription.MultisampleCount, (uint) quality),
                BindFlags = (uint) GetBindFlagsFromTextureFlags(flags),
                Format = format,
                MipLevels = (uint) textureDescription.MipLevelCount,
                Usage = (Usage) textureDescription.Usage,
                CPUAccessFlags = (uint) GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                MiscFlags = (uint) textureDescription.Options
            };

            if (textureDescription.Dimension == TextureDimension.TextureCube)
                desc.MiscFlags = (uint) ResourceMiscFlag.Texturecube;

            return desc;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat depthFormat)
        {
            var viewFormat = depthFormat switch
            {
                PixelFormat.R16_Typeless or PixelFormat.D16_UNorm => PixelFormat.R16_Float,
                PixelFormat.R32_Typeless or PixelFormat.D32_Float => PixelFormat.R32_Float,
                PixelFormat.R24G8_Typeless or PixelFormat.D24_UNorm_S8_UInt => PixelFormat.R24_UNorm_X8_Typeless,
                PixelFormat.R32_Float_X8X24_Typeless or PixelFormat.D32_Float_S8X24_UInt => PixelFormat.R32_Float_X8X24_Typeless,

                _ => PixelFormat.None
            };
            return viewFormat;
        }

        internal static Format ComputeDepthViewFormatFromTextureFormat(PixelFormat format)
        {
            var viewFormat = format switch
            {
                PixelFormat.R16_Typeless or PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatD16Unorm,
                PixelFormat.R32_Typeless or PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatD32Float,
                PixelFormat.R24G8_Typeless or PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatD24UnormS8Uint,
                PixelFormat.R32G8X24_Typeless or PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint,

                _ => throw new NotSupportedException($"Unsupported depth format [{format}]")
            };
            return viewFormat;
        }

        private Texture3DDesc ConvertToNativeDescription3D()
        {
            var desc = new Texture3DDesc
            {
                Width = (uint) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                Depth = (uint) textureDescription.Depth,
                BindFlags = (uint) GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (Format) textureDescription.Format,
                MipLevels = (uint) textureDescription.MipLevelCount,
                Usage = (Usage) textureDescription.Usage,
                CPUAccessFlags = (uint) GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                MiscFlags = (uint) textureDescription.Options
            };
            return desc;
        }

        /// <summary>
        /// Check and modify if necessary the mipmap levels of the image (Troubles with DXT images whose resolution in less than 4x4 in DX9.x).
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="description">The texture description.</param>
        /// <returns>The updated texture description.</returns>
        private static TextureDescription CheckMipLevels(GraphicsDevice device, ref TextureDescription description)
        {
            if (device.Features.CurrentProfile < GraphicsProfile.Level_10_0 &&
                description.Flags.HasFlag(TextureFlags.DepthStencil) && description.Format.IsCompressed())
            {
                description.MipLevelCount = Math.Min(CalculateMipCount(description.Width, description.Height), description.MipLevelCount);
            }
            return description;
        }

        /// <summary>
        /// Calculates the mip level from a specified size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip.</param>
        /// <returns>The mip level.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Value must be > 0;size</exception>
        private static int CalculateMipCountFromSize(int size, int minimumSizeLastMip = 4)
        {
            // TODO: CountMips?

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumSizeLastMip);

            int level = 1;
            while ((size / 2) >= minimumSizeLastMip)
            {
                size = Math.Max(1, size / 2);
                level++;
            }
            return level;
        }

        /// <summary>
        /// Calculates the mip level from a specified width,height,depth.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip.</param>
        /// <returns>The mip level.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Value must be &gt; 0;size</exception>
        private static int CalculateMipCount(int width, int height, int minimumSizeLastMip = 4)
        {
            return Math.Min(CalculateMipCountFromSize(width, minimumSizeLastMip),
                            CalculateMipCountFromSize(height, minimumSizeLastMip));
        }

        internal static bool IsStencilFormat(PixelFormat format)
        {
            return format switch
            {
                PixelFormat.R24G8_Typeless or
                PixelFormat.D24_UNorm_S8_UInt or
                PixelFormat.R32G8X24_Typeless or
                PixelFormat.D32_Float_S8X24_UInt => true,

                _ => false
            };
        }

        /// <summary>
        ///   Gets the CPU access flags from the intended texture usage.
        /// </summary>
        /// <param name="usage">The usage.</param>
        /// <returns>A combination of one or more <see cref="CpuAccessFlag"/> flags.</returns>
        private new CpuAccessFlag GetCpuAccessFlagsFromUsage(GraphicsResourceUsage usage)
        {
            return usage switch
            {
                // Depth-Stencil Textures may not be used in combination with CpuAccessFlags
                // when the usage is Default
                GraphicsResourceUsage.Default when IsDepthStencil => CpuAccessFlag.None,

                _ => GraphicsResourceBase.GetCpuAccessFlagsFromUsage(usage)
            };
        }
    }
}

#endif
