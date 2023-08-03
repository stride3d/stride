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
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

using static Stride.Graphics.DebugHelpers;

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
        ///   Gets the DepthStencilView attached to this Texture resource.
        /// </summary>
        internal ID3D11DepthStencilView* NativeDepthStencilView
        {
            get => depthStencilView;

            private set
            {
                depthStencilView = value;
                if (IsDebugMode && depthStencilView != null)
                {
                    SetDebugName((ID3D11DeviceChild*) depthStencilView, Name is null ? null : $"{Name} DSV");
                }
            }
        }

        /// <summary>
        ///   Gets the RenderTargetView attached to this Texture resource.
        ///   Note that only Texture, Texture3D, RenderTarget2D, RenderTarget3D, DepthStencil are using this ShaderResourceView.
        /// </summary>
        internal ID3D11RenderTargetView* NativeRenderTargetView
        {
            get => renderTargetView;

            private set
            {
                renderTargetView = value;
                if (IsDebugMode && renderTargetView != null)
                {
                    SetDebugName((ID3D11DeviceChild*) renderTargetView, Name is null ? null : $"{Name} RTV");
                }
            }
        }


        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            return device.Features.CurrentProfile >= GraphicsProfile.Level_11_0;
        }

        /// <summary>
        ///   Initializes from a native ID3D11Texture2D.
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(ID3D11Texture2D* texture, bool isSrgb)
        {
            NativeDeviceChild = (ID3D11DeviceChild*) texture;

            Texture2DDesc textureDesc;
            texture->GetDesc(&textureDesc);

            var newTextureDescription = ConvertFromNativeDescription(textureDesc);

            // We might have created the swapchain as a non-sRGB format (specially on Win 10 & RT) but we want it to
            // behave like it is (specially for the view and render target)
            if (isSrgb)
                newTextureDescription.Format = newTextureDescription.Format.ToSRgb();

            return InitializeFrom(newTextureDescription);
        }

        /// <summary>
        ///   Initializes from a native ID3D11ShaderResourceView.
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(ID3D11ShaderResourceView* srv)
        {
            ShaderResourceViewDesc srvDescription;
            srv->GetDesc(&srvDescription);

            if (srvDescription.ViewDimension == D3DSrvDimension.D3D101SrvDimensionTexture2D)
            {
                NativeShaderResourceView = srv;
                NativeShaderResourceView->AddRef();

                ID3D11Resource* resource;
                srv->GetResource(&resource);

                ID3D11Texture2D* texture;
                HResult result = resource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Texture2D>(), (void**) &texture);

                if (result.IsFailure)
                    result.Throw();

                NativeDeviceChild = (ID3D11DeviceChild*) texture;

                Texture2DDesc textureDesc;
                texture->GetDesc(&textureDesc);

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
                throw new NotImplementedException($"Creating a texture from a SRV with dimension {srvDescription.ViewDimension} is not implemented");
            }
        }

        internal void SwapInternal(Texture other)
        {
            var deviceChild = NativeDeviceChild;
            NativeDeviceChild = other.NativeDeviceChild;
            other.NativeDeviceChild = deviceChild;

            var srv = NativeShaderResourceView;
            NativeShaderResourceView = other.NativeShaderResourceView;
            other.NativeShaderResourceView = srv;

            var uav = NativeUnorderedAccessView;
            NativeUnorderedAccessView = other.NativeUnorderedAccessView;
            other.NativeUnorderedAccessView = uav;

            var rtv = renderTargetView;
            renderTargetView = other.renderTargetView;
            other.renderTargetView = rtv;

            var dsv = depthStencilView;
            depthStencilView = other.depthStencilView;
            other.depthStencilView = dsv;

            (HasStencil, other.HasStencil) = (other.HasStencil, HasStencil);
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            if (ParentTexture is not null)
            {
                NativeDeviceChild = ParentTexture.NativeDeviceChild;
            }

            if (NativeDeviceChild is null)
            {
                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                    {
                        ID3D11Texture1D* texture1D = CreateTexture1D(dataBoxes);
                        NativeDeviceChild = (ID3D11DeviceChild*) texture1D;
                        break;
                    }
                    case TextureDimension.Texture2D:
                    case TextureDimension.TextureCube:
                    {
                        ID3D11Texture2D* texture2D = CreateTexture2D(dataBoxes);
                        NativeDeviceChild = (ID3D11DeviceChild*) texture2D;
                        break;
                    }
                    case TextureDimension.Texture3D:
                    {
                        ID3D11Texture3D* texture3D = CreateTexture3D(dataBoxes);
                        NativeDeviceChild = (ID3D11DeviceChild*) texture3D;
                        break;
                    }
                }

                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);
            }

            if (NativeShaderResourceView is null)
                NativeShaderResourceView = GetShaderResourceView(ViewType, ArraySlice, MipLevel);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewType, ArraySlice, MipLevel);
            NativeRenderTargetView = GetRenderTargetView(ViewType, ArraySlice, MipLevel);
            NativeDepthStencilView = GetDepthStencilView(out HasStencil);



            if (textureDescription.Options == TextureOptions.None)
            {
                SharedHandle = IntPtr.Zero;
            }
#if STRIDE_GRAPHICS_API_DIRECT3D11
            else if (textureDescription.Options.HasFlag(TextureOptions.SharedNthandle) ||
                     textureDescription.Options.HasFlag(TextureOptions.SharedKeyedmutex))
            {
                IDXGIResource1* sharedResource1;
                HResult result = NativeDeviceChild->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIResource1>(), (void**) &sharedResource1);

                if (result.IsFailure)
                    result.Throw();

                var uniqueName = $"Stride:{Guid.NewGuid()}";

                const uint DXGI_SHARED_RESOURCE_WRITE = 1;

                void* sharedHandle;
                result = sharedResource1->CreateSharedHandle(pAttributes: null, dwAccess: DXGI_SHARED_RESOURCE_WRITE, uniqueName, &sharedHandle);

                if (result.IsFailure)
                    result.Throw();

                SharedHandle = new(sharedHandle);
                SharedNtHandleName = uniqueName;
            }
#endif
            else if (textureDescription.Options.HasFlag(TextureOptions.Shared))
            {
                IDXGIResource* sharedResource;
                HResult result = NativeDeviceChild->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIResource>(), (void**) &sharedResource);

                if (result.IsFailure)
                    result.Throw();

                void* sharedHandle;
                result = sharedResource->GetSharedHandle(&sharedHandle);

                if (result.IsFailure)
                    result.Throw();

                SharedHandle = new(sharedHandle);
            }
            else
            {
                throw new ArgumentOutOfRangeException("textureDescription.Options");
            }
        }

        protected internal override void OnDestroyed()
        {
            // If it was a View, do not release reference
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

        private void OnRecreateImpl()
        {
            // Dependency: wait for underlying texture to be recreated
            if (ParentTexture != null && ParentTexture.LifetimeState != GraphicsResourceLifetimeState.Active)
                return;

            // Render Target / Depth Stencil are considered as "dynamic"
            if (Usage is GraphicsResourceUsage.Immutable or GraphicsResourceUsage.Default &&
                !IsRenderTarget && !IsDepthStencil)
                return;

            if (ParentTexture is null)
            {
                GraphicsDevice?.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            InitializeFromImpl();
        }

        private ID3D11Texture1D* CreateTexture1D(DataBox[] initialData)
        {
            ID3D11Texture1D* texture1D;

            Texture1DDesc description = ConvertToNativeDescription1D();
            SubresourceData[] initiatDataPerSubresource = ConvertDataBoxes(initialData);

            HResult result = NativeDevice->CreateTexture1D(in description, in initiatDataPerSubresource[0], &texture1D);

            if (result.IsFailure)
                result.Throw();

            return texture1D;
        }

        private ID3D11Texture2D* CreateTexture2D(DataBox[] initialData)
        {
            ID3D11Texture2D* texture2D;

            Texture2DDesc description = ConvertToNativeDescription2D();
            SubresourceData[] initiatDataPerSubresource = ConvertDataBoxes(initialData);

            HResult result = initiatDataPerSubresource is null
                ? NativeDevice->CreateTexture2D(in description, pInitialData: null, &texture2D)
                : NativeDevice->CreateTexture2D(in description, in initiatDataPerSubresource[0], &texture2D);

            if (result.IsFailure)
                result.Throw();

            return texture2D;
        }

        private ID3D11Texture3D* CreateTexture3D(DataBox[] initialData)
        {
            ID3D11Texture3D* texture3D;

            Texture3DDesc description = ConvertToNativeDescription3D();
            SubresourceData[] initiatDataPerSubresource = ConvertDataBoxes(initialData);

            HResult result = NativeDevice->CreateTexture3D(in description, in initiatDataPerSubresource[0], &texture3D);

            if (result.IsFailure)
                result.Throw();

            return texture3D;
        }

        /// <summary>
        /// Gets a specific <see cref="ShaderResourceView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">The mip map slice index.</param>
        /// <returns>An <see cref="ShaderResourceView" /></returns>
        private ID3D11ShaderResourceView* GetShaderResourceView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return null;

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

            // Create the view
            var srvDescription = new ShaderResourceViewDesc() { Format = ComputeShaderResourceViewFormat() };

            // Initialize for texture arrays or texture cube
            if (ArraySize > 1)
            {
                // If texture cube
                if (ViewDimension == TextureDimension.TextureCube)
                {
                    srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexturecube;
                    srvDescription.TextureCube = new()
                    {
                        MipLevels = (uint) mipCount,
                        MostDetailedMip = (uint) mipIndex
                    };
                }
                else
                {
                    // Else regular Texture array
                    // Multisample?
                    if (IsMultisample)
                    {
                        if (Dimension != TextureDimension.Texture2D)
                        {
                            throw new NotSupportedException("Multisample is only supported for 2D Textures");
                        }

                        srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Dmsarray;
                        srvDescription.Texture2DMSArray = new()
                        {
                            ArraySize = (uint) arrayCount,
                            FirstArraySlice = (uint) arrayOrDepthSlice
                        };
                    }
                    else
                    {
                        if (ViewDimension == TextureDimension.Texture2D)
                        {
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Darray;
                            srvDescription.Texture2DArray = new()
                            {
                                ArraySize = (uint) arrayCount,
                                FirstArraySlice = (uint) arrayOrDepthSlice,
                                MipLevels = (uint) mipCount,
                                MostDetailedMip = (uint) mipIndex
                            };
                        }
                        else if (ViewDimension != TextureDimension.Texture1D)
                        {
                            srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture1Darray;
                            srvDescription.Texture1DArray = new()
                            {
                                ArraySize = (uint) arrayCount,
                                FirstArraySlice = (uint) arrayOrDepthSlice,
                                MipLevels = (uint) mipCount,
                                MostDetailedMip = (uint) mipIndex
                            };
                        }
                    }
                }
            }
            else
            {
                if (IsMultisample)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D Textures");
                    }

                    srvDescription.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Dms;
                }
                else
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
                            throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");
                    }
                    // Use srvDescription.Texture1D as it matches also Texture and Texture3D memory layout
                    srvDescription.Texture1D = new()
                    {
                        MipLevels = (uint) mipCount,
                        MostDetailedMip = (uint) mipIndex
                    };
                }
            }

            ID3D11ShaderResourceView* srv;
            HResult result = NativeDevice->CreateShaderResourceView(NativeResource, &srvDescription, &srv);

            if (result.IsFailure)
                result.Throw();

            return srv;
        }

        /// <summary>
        /// Gets a specific <see cref="ID3D11RenderTargetView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="ID3D11RenderTargetView" /></returns>
        /// <exception cref="NotSupportedException">ViewSlice.MipBand is not supported for render targets</exception>
        private ID3D11RenderTargetView* GetRenderTargetView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return null;

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

            // Create the render target view
            var rtvDescription = new RenderTargetViewDesc() { Format = (Format) ViewFormat };

            if (ArraySize > 1)
            {
                if (MultisampleCount > MultisampleCount.None)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D Textures");
                    }

                    rtvDescription.ViewDimension = RtvDimension.Texture2Dmsarray;
                    rtvDescription.Texture2DMSArray = new()
                    {
                        ArraySize = (uint) arrayCount,
                        FirstArraySlice = (uint) arrayOrDepthSlice
                    };
                }
                else
                {
                    if (ViewDimension == TextureDimension.Texture3D)
                    {
                        throw new NotSupportedException("Texture Array is not supported for Texture3D");
                    }

                    rtvDescription.ViewDimension = Dimension is TextureDimension.Texture2D or TextureDimension.TextureCube
                        ? RtvDimension.Texture2Darray
                        : RtvDimension.Texture1Darray;

                    // Use rtvDescription.Texture1DArray as it matches also Texture2DArray memory layout
                    rtvDescription.Texture1DArray = new()
                    {
                        ArraySize = (uint) arrayCount,
                        FirstArraySlice = (uint) arrayOrDepthSlice,
                        MipSlice = (uint) mipIndex
                    };
                }
            }
            else
            {
                if (IsMultisample)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D RenderTarget Textures");
                    }

                    rtvDescription.ViewDimension = RtvDimension.Texture2Dms;
                }
                else
                {
                    switch (ViewDimension)
                    {
                        case TextureDimension.Texture1D:
                            rtvDescription.ViewDimension = RtvDimension.Texture1D;
                            rtvDescription.Texture1D = rtvDescription.Texture1D with { MipSlice = (uint) mipIndex };
                            break;

                        case TextureDimension.Texture2D:
                            rtvDescription.ViewDimension = RtvDimension.Texture2D;
                            rtvDescription.Texture2D = rtvDescription.Texture2D with { MipSlice = (uint) mipIndex };
                            break;

                        case TextureDimension.Texture3D:
                            rtvDescription.ViewDimension = RtvDimension.Texture3D;
                            rtvDescription.Texture3D = rtvDescription.Texture3D with
                            {
                                WSize = (uint) arrayCount,
                                FirstWSlice = (uint) arrayOrDepthSlice,
                                MipSlice = (uint) mipIndex
                            };
                            break;

                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");
                    }
                }
            }

            ID3D11RenderTargetView* rtv;
            HResult result = NativeDevice->CreateRenderTargetView(NativeResource, &rtvDescription, &rtv);

            if (result.IsFailure)
                result.Throw();

            return rtv;
        }

        /// <summary>
        /// Gets a specific <see cref="UnorderedAccessView" /> from this texture.
        /// </summary>
        /// <param name="viewType">The desired view type on the unordered resource</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="UnorderedAccessView" /></returns>
        private ID3D11UnorderedAccessView* GetUnorderedAccessView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsUnorderedAccess)
                return null;

            if (IsMultisample)
                throw new NotSupportedException("Multisampling is not supported for unordered access views");

            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

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

                uavDescription.Texture1DArray = new()
                {
                    ArraySize = (uint) arrayCount,
                    FirstArraySlice = (uint) arrayOrDepthSlice,
                    MipSlice = (uint) mipIndex
                };
            }
            else
            {
                switch (ViewDimension)
                {
                    case TextureDimension.Texture1D:
                        uavDescription.ViewDimension = UavDimension.Texture1D;
                        uavDescription.Texture1D = uavDescription.Texture1D with { MipSlice = (uint)mipIndex };
                        break;

                    case TextureDimension.Texture2D:
                        uavDescription.ViewDimension = UavDimension.Texture2D;
                        uavDescription.Texture2D = uavDescription.Texture2D with { MipSlice = (uint) mipIndex };
                        break;

                    case TextureDimension.Texture3D:
                        uavDescription.ViewDimension = UavDimension.Texture3D;
                        uavDescription.Texture3D = uavDescription.Texture3D with
                        {
                            WSize = (uint) arrayCount,
                            FirstWSlice = (uint) arrayOrDepthSlice,
                            MipSlice = (uint) mipIndex
                        };
                        break;

                    case TextureDimension.TextureCube:
                        throw new NotSupportedException("TextureCube dimension is expecting an array size > 1");
                }
            }

            ID3D11UnorderedAccessView* uav;
            HResult result = NativeDevice->CreateUnorderedAccessView(NativeResource, &uavDescription, &uav);

            if (result.IsFailure)
                result.Throw();

            return uav;
        }

        private ID3D11DepthStencilView* GetDepthStencilView(out bool hasStencil)
        {
            hasStencil = false;

            if (!IsDepthStencil)
                return null;

            // Check that the format is supported
            if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
                throw new NotSupportedException($"Depth stencil format [{ViewFormat}] not supported");

            // Setup the HasStencil flag
            hasStencil = IsStencilFormat(ViewFormat);

            // Create a Depth stencil view on this Texture2D
            var dsvDescription = new DepthStencilViewDesc
            {
                Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                Flags = 0
            };

            if (ArraySize > 1)
            {
                dsvDescription.ViewDimension = DsvDimension.Texture2Darray;
                dsvDescription.Texture2DArray = dsvDescription.Texture2DArray with
                {
                    ArraySize = (uint) ArraySize,
                    FirstArraySlice = 0,
                    MipSlice = 0
                };
            }
            else
            {
                dsvDescription.ViewDimension = DsvDimension.Texture2D;
                dsvDescription.Texture2D = dsvDescription.Texture2D with { MipSlice = 0 };
            }

            if (MultisampleCount > MultisampleCount.None)
                dsvDescription.ViewDimension = DsvDimension.Texture2Dms;

            if (IsDepthStencilReadOnly)
            {
                if (!IsDepthStencilReadOnlySupported(GraphicsDevice))
                    throw new NotSupportedException("Cannot instantiate ReadOnly DepthStencilBuffer. Not supported on this device.");

                // Create a Depth stencil view on this Texture2D
                dsvDescription.Flags = (uint) DsvFlag.Depth;
                if (HasStencil)
                    dsvDescription.Flags |= (uint) DsvFlag.Stencil;
            }

            ID3D11DepthStencilView* dsv;
            HResult result = NativeDevice->CreateDepthStencilView(NativeResource, &dsvDescription, &dsv);

            if (result.IsFailure)
                result.Throw();

            return dsv;
        }

        internal static BindFlag GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            BindFlag result = 0;

            if (flags.HasFlag(TextureFlags.ShaderResource))   result |= BindFlag.ShaderResource;
            if (flags.HasFlag(TextureFlags.RenderTarget))     result |= BindFlag.RenderTarget;
            if (flags.HasFlag(TextureFlags.UnorderedAccess))  result |= BindFlag.UnorderedAccess;
            if (flags.HasFlag(TextureFlags.DepthStencil))     result |= BindFlag.DepthStencil;

            return result;
        }

        internal static unsafe SubresourceData[] ConvertDataBoxes(DataBox[] dataBoxes)
        {
            if (dataBoxes == null || dataBoxes.Length == 0)
                return null;

            // NOTE: This conversion works only IF the memory layout of DataBox matches that of SubresourceData
            Debug.Assert(Unsafe.SizeOf<DataBox>() == Unsafe.SizeOf<SubresourceData>());
            return MemoryMarshal.Cast<DataBox, SubresourceData>(dataBoxes.AsSpan()).ToArray();
        }

        private bool IsFlipped()
        {
            return false;
        }

        private Texture1DDesc ConvertToNativeDescription1D()
        {
            var desc = new Texture1DDesc()
            {
                Width = (uint) textureDescription.Width,
                ArraySize = 1,
                BindFlags = (uint) GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (Format) textureDescription.Format,
                MipLevels = (uint) textureDescription.MipLevels,
                Usage = (Usage) textureDescription.Usage,
                CPUAccessFlags = (uint) GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                MiscFlags = (uint) textureDescription.Options
            };
            return desc;
        }

        private Format ComputeShaderResourceViewFormat()
        {
            // Special case for DepthStencil ShaderResourceView that are bound as Float
            var viewFormat = IsDepthStencil
                ? (Format) ComputeShaderResourceFormatFromDepthFormat(ViewFormat)
                : (Format) ViewFormat;

            return viewFormat;
        }

        private static TextureDescription ConvertFromNativeDescription(Texture2DDesc description)
        {
            var desc = new TextureDescription()
            {
                Dimension = TextureDimension.Texture2D,
                Width = (int) description.Width,
                Height = (int) description.Height,
                Depth = 1,
                MultisampleCount = (MultisampleCount)(int) description.SampleDesc.Count,
                Format = (PixelFormat)description.Format,
                MipLevels = (int) description.MipLevels,
                Usage = (GraphicsResourceUsage)description.Usage,
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

            if (miscFlags.HasFlag(ResourceMiscFlag.Shared))  desc.Options |= TextureOptions.Shared;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            if (miscFlags.HasFlag(ResourceMiscFlag.SharedKeyedmutex))
                desc.Options |= TextureOptions.SharedKeyedmutex;
            if (miscFlags.HasFlag(ResourceMiscFlag.SharedNthandle))
                desc.Options |= TextureOptions.SharedNthandle;
#endif
            return desc;
        }

        private Texture2DDesc ConvertToNativeDescription2D()
        {
            var format = (Format) textureDescription.Format;
            var flags = textureDescription.Flags;

            // If the texture is going to be bound as depth stencil, use TypeLess format
            if (IsDepthStencil)
            {
                if (IsShaderResource && GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                {
                    throw new NotSupportedException($"ShaderResourceView for DepthStencil Textures are not supported for Graphics profile < 10.0 (Current: [{GraphicsDevice.Features.CurrentProfile}])");
                }
                else
                {
                    // Determine TypeLess Format and ShaderResourceView Format
                    if (GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatD16Unorm,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatD32Float,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatD24UnormS8Uint,
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint,

                            _ => throw new NotSupportedException($"Unsupported DepthFormat [{textureDescription.Format}] for depth buffer")
                        };
                    }
                    else
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatR16Typeless,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatR32Typeless,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatR24G8Typeless,//Silk.NET.DXGI.Format.FormatD24UnormS8Uint
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatR32G8X24Typeless,

                            _ => throw new NotSupportedException($"Unsupported DepthFormat [{textureDescription.Format}] for depth buffer")
                        };
                    }
                }
            }

            int quality = 0;
            if (GraphicsDevice.Features.CurrentProfile >= GraphicsProfile.Level_10_1 && textureDescription.IsMultisample)
                quality = (int) StandardMultisampleQualityLevels.StandardMultisamplePattern;

            var desc = new Texture2DDesc()
            {
                Width = (uint) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                ArraySize = (uint) textureDescription.ArraySize,
                SampleDesc = new SampleDesc((uint) textureDescription.MultisampleCount, (uint) quality),
                BindFlags = (uint) GetBindFlagsFromTextureFlags(flags),
                Format = format,
                MipLevels = (uint) textureDescription.MipLevels,
                Usage = (Usage) textureDescription.Usage,
                CPUAccessFlags = (uint) GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                MiscFlags = (uint) textureDescription.Options
            };

            if (textureDescription.Dimension == TextureDimension.TextureCube)
                desc.MiscFlags = (uint) ResourceMiscFlag.Texturecube;

            return desc;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            var viewFormat = format switch
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
            var desc = new Texture3DDesc()
            {
                Width = (uint) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                Depth = (uint) textureDescription.Depth,
                BindFlags = (uint) GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (Format) textureDescription.Format,
                MipLevels = (uint) textureDescription.MipLevels,
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
                description.MipLevels = Math.Min(CalculateMipCount(description.Width, description.Height), description.MipLevels);
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
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "The size must be > 0");
            }
            if (minimumSizeLastMip <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSizeLastMip), minimumSizeLastMip, "The last mip's minimum size must be > 0");
            }

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
    }
}

#endif
