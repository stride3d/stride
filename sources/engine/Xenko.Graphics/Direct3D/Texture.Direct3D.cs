// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11
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
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Xenko.Core;

namespace Xenko.Graphics
{
    public partial class Texture
    {
        private RenderTargetView renderTargetView;
        private DepthStencilView depthStencilView;
        internal bool HasStencil;

        private int TexturePixelSize => Format.SizeInBytes();
        private const int TextureRowPitchAlignment = 1;
        private const int TextureSubresourceAlignment = 1;

        internal DepthStencilView NativeDepthStencilView
        {
            get
            {
                return depthStencilView;
            }
            private set
            {
                depthStencilView = value;
                if (IsDebugMode && depthStencilView != null)
                {
                    depthStencilView.DebugName = string.Format("{0} DSV", Name);
                }
            }
        }

        /// <summary>
        /// Gets the RenderTargetView attached to this GraphicsResource.
        /// Note that only Texture, Texture3D, RenderTarget2D, RenderTarget3D, DepthStencil are using this ShaderResourceView
        /// </summary>
        /// <value>The device child.</value>
        internal RenderTargetView NativeRenderTargetView
        {
            get
            {
                return renderTargetView;
            }
            private set
            {
                renderTargetView = value;
                if (IsDebugMode && renderTargetView != null)
                {
                    renderTargetView.DebugName = string.Format("{0} RTV", Name);
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
        /// Initializes from a native SharpDX.Texture
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(Texture2D texture, bool isSrgb)
        {
            NativeDeviceChild = texture;
            var newTextureDescription = ConvertFromNativeDescription(texture.Description);

            // We might have created the swapchain as a non-srgb format (esp on Win10&RT) but we want it to behave like it is (esp. for the view and render target)
            if (isSrgb)
                newTextureDescription.Format = newTextureDescription.Format.ToSRgb();

            return InitializeFrom(newTextureDescription);
        }

        internal Texture InitializeFromImpl(ShaderResourceView srv)
        {
            return InitializeFromImpl(new Texture2D(srv.Resource.NativePointer), false);
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

            Utilities.Swap(ref renderTargetView, ref other.renderTargetView);
            Utilities.Swap(ref depthStencilView, ref other.depthStencilView);
            Utilities.Swap(ref HasStencil, ref other.HasStencil);
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            if (ParentTexture != null)
            {
                NativeDeviceChild = ParentTexture.NativeDeviceChild;
            }

            if (NativeDeviceChild == null)
            {
                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                        NativeDeviceChild = new Texture1D(GraphicsDevice.NativeDevice, ConvertToNativeDescription1D(), ConvertDataBoxes(dataBoxes));
                        break;
                    case TextureDimension.Texture2D:
                    case TextureDimension.TextureCube:
                        NativeDeviceChild = new Texture2D(GraphicsDevice.NativeDevice, ConvertToNativeDescription2D(), ConvertDataBoxes(dataBoxes));
                        break;
                    case TextureDimension.Texture3D:
                        NativeDeviceChild = new Texture3D(GraphicsDevice.NativeDevice, ConvertToNativeDescription3D(), ConvertDataBoxes(dataBoxes));
                        break;
                }

                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);
            }

            NativeShaderResourceView = GetShaderResourceView(ViewType, ArraySlice, MipLevel);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewType, ArraySlice, MipLevel);
            NativeRenderTargetView = GetRenderTargetView(ViewType, ArraySlice, MipLevel);
            NativeDepthStencilView = GetDepthStencilView(out HasStencil);

            switch (textureDescription.Options)
            {
                case TextureOptions.None:
                    SharedHandle = IntPtr.Zero;
                    break;
                case TextureOptions.Shared:
                    var sharedResource = NativeDeviceChild.QueryInterface<SharpDX.DXGI.Resource>();
                    SharedHandle = sharedResource.SharedHandle;
                    break;
#if XENKO_GRAPHICS_API_DIRECT3D11
                case TextureOptions.SharedNthandle | TextureOptions.SharedKeyedmutex:
                    var sharedResource1 = NativeDeviceChild.QueryInterface<SharpDX.DXGI.Resource1>();
                    var uniqueName = "Xenko:" + Guid.NewGuid().ToString();
                    SharedHandle = sharedResource1.CreateSharedHandle(uniqueName, SharpDX.DXGI.SharedResourceFlags.Write);
                    SharedNtHandleName = uniqueName;
                    break; 
#endif
                default:
                    throw new ArgumentOutOfRangeException("textureDescription.Options");
            }
        }

        protected internal override void OnDestroyed()
        {
            // If it was a View, do not release reference
            if (ParentTexture != null)
            {
                NativeDeviceChild = null;
            }
            else if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            ReleaseComObject(ref renderTargetView);
            ReleaseComObject(ref depthStencilView);

            base.OnDestroyed();
        }

        private void OnRecreateImpl()
        {
            // Dependency: wait for underlying texture to be recreated
            if (ParentTexture != null && ParentTexture.LifetimeState != GraphicsResourceLifetimeState.Active)
                return;

            // Render Target / Depth Stencil are considered as "dynamic"
            if ((Usage == GraphicsResourceUsage.Immutable
                    || Usage == GraphicsResourceUsage.Default)
                && !IsRenderTarget && !IsDepthStencil)
                return;

            if (ParentTexture == null && GraphicsDevice != null)
            {
                GraphicsDevice.RegisterTextureMemoryUsage(-SizeInBytes);
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
        private ShaderResourceView GetShaderResourceView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return null;

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayCount, out mipCount);

            // Create the view
            var srvDescription = new ShaderResourceViewDescription() { Format = ComputeShaderResourceViewFormat() };

            // Initialize for texture arrays or texture cube
            if (this.ArraySize > 1)
            {
                // If texture cube
                if (this.ViewDimension == TextureDimension.TextureCube)
                {
                    srvDescription.Dimension = ShaderResourceViewDimension.TextureCube;
                    srvDescription.TextureCube.MipLevels = mipCount;
                    srvDescription.TextureCube.MostDetailedMip = mipIndex;
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

                        srvDescription.Dimension = ShaderResourceViewDimension.Texture2DMultisampledArray;
                        srvDescription.Texture2DMSArray.ArraySize = arrayCount;
                        srvDescription.Texture2DMSArray.FirstArraySlice = arrayOrDepthSlice;
                    }
                    else
                    {
                        srvDescription.Dimension = ViewDimension == TextureDimension.Texture2D ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture1DArray;
                        srvDescription.Texture2DArray.ArraySize = arrayCount;
                        srvDescription.Texture2DArray.FirstArraySlice = arrayOrDepthSlice;
                        srvDescription.Texture2DArray.MipLevels = mipCount;
                        srvDescription.Texture2DArray.MostDetailedMip = mipIndex;
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

                    srvDescription.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
                }
                else
                {
                    switch (ViewDimension)
                    {
                        case TextureDimension.Texture1D:
                            srvDescription.Dimension = ShaderResourceViewDimension.Texture1D;
                            break;
                        case TextureDimension.Texture2D:
                            srvDescription.Dimension = ShaderResourceViewDimension.Texture2D;
                            break;
                        case TextureDimension.Texture3D:
                            srvDescription.Dimension = ShaderResourceViewDimension.Texture3D;
                            break;
                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");
                    }
                    // Use srvDescription.Texture as it matches also Texture and Texture3D memory layout
                    srvDescription.Texture1D.MipLevels = mipCount;
                    srvDescription.Texture1D.MostDetailedMip = mipIndex;
                }
            }

            // Default ShaderResourceView
            return new ShaderResourceView(this.GraphicsDevice.NativeDevice, NativeResource, srvDescription);
        }

        /// <summary>
        /// Gets a specific <see cref="RenderTargetView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="RenderTargetView" /></returns>
        /// <exception cref="System.NotSupportedException">ViewSlice.MipBand is not supported for render targets</exception>
        private RenderTargetView GetRenderTargetView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return null;

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayCount, out mipCount);

            // Create the render target view
            var rtvDescription = new RenderTargetViewDescription() { Format = (SharpDX.DXGI.Format)ViewFormat };

            if (this.ArraySize > 1)
            {
                if (this.MultisampleCount > MultisampleCount.None)
                {
                    if (ViewDimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D Textures");
                    }

                    rtvDescription.Dimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                    rtvDescription.Texture2DMSArray.ArraySize = arrayCount;
                    rtvDescription.Texture2DMSArray.FirstArraySlice = arrayOrDepthSlice;
                }
                else
                {
                    if (ViewDimension == TextureDimension.Texture3D)
                    {
                        throw new NotSupportedException("Texture Array is not supported for Texture3D");
                    }

                    rtvDescription.Dimension = Dimension == TextureDimension.Texture2D || Dimension == TextureDimension.TextureCube ? RenderTargetViewDimension.Texture2DArray : RenderTargetViewDimension.Texture1DArray;

                    // Use rtvDescription.Texture1DArray as it matches also Texture memory layout
                    rtvDescription.Texture1DArray.ArraySize = arrayCount;
                    rtvDescription.Texture1DArray.FirstArraySlice = arrayOrDepthSlice;
                    rtvDescription.Texture1DArray.MipSlice = mipIndex;
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

                    rtvDescription.Dimension = RenderTargetViewDimension.Texture2DMultisampled;
                }
                else
                {
                    switch (ViewDimension)
                    {
                        case TextureDimension.Texture1D:
                            rtvDescription.Dimension = RenderTargetViewDimension.Texture1D;
                            rtvDescription.Texture1D.MipSlice = mipIndex;
                            break;
                        case TextureDimension.Texture2D:
                            rtvDescription.Dimension = RenderTargetViewDimension.Texture2D;
                            rtvDescription.Texture2D.MipSlice = mipIndex;
                            break;
                        case TextureDimension.Texture3D:
                            rtvDescription.Dimension = RenderTargetViewDimension.Texture3D;
                            rtvDescription.Texture3D.DepthSliceCount = arrayCount;
                            rtvDescription.Texture3D.FirstDepthSlice = arrayOrDepthSlice;
                            rtvDescription.Texture3D.MipSlice = mipIndex;
                            break;
                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");
                    }
                }
            }

            return new RenderTargetView(GraphicsDevice.NativeDevice, NativeResource, rtvDescription);
        }

        /// <summary>
        /// Gets a specific <see cref="UnorderedAccessView" /> from this texture.
        /// </summary>
        /// <param name="viewType">The desired view type on the unordered resource</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="UnorderedAccessView" /></returns>
        private UnorderedAccessView GetUnorderedAccessView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsUnorderedAccess)
                return null;

            if (IsMultisample)
                throw new NotSupportedException("Multisampling is not supported for unordered access views");

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayCount, out mipCount);

            var uavDescription = new UnorderedAccessViewDescription
            {
                Format = (SharpDX.DXGI.Format)ViewFormat,
            };

            if (ArraySize > 1)
            {
                switch (ViewDimension)
                {
                    case TextureDimension.Texture1D:
                        uavDescription.Dimension = UnorderedAccessViewDimension.Texture1DArray;
                        break;
                    case TextureDimension.TextureCube:
                    case TextureDimension.Texture2D:
                        uavDescription.Dimension = UnorderedAccessViewDimension.Texture2DArray;
                        break;
                    case TextureDimension.Texture3D:
                        throw new NotSupportedException("Texture 3D is not supported for Texture Arrays");
                }

                uavDescription.Texture1DArray.ArraySize = arrayCount;
                uavDescription.Texture1DArray.FirstArraySlice = arrayOrDepthSlice;
                uavDescription.Texture1DArray.MipSlice = mipIndex;
            }
            else
            {
                switch (ViewDimension)
                {
                    case TextureDimension.Texture1D:
                        uavDescription.Dimension = UnorderedAccessViewDimension.Texture1D;
                        uavDescription.Texture1D.MipSlice = mipIndex;
                        break;
                    case TextureDimension.Texture2D:
                        uavDescription.Dimension = UnorderedAccessViewDimension.Texture2D;
                        uavDescription.Texture2D.MipSlice = mipIndex;
                        break;
                    case TextureDimension.Texture3D:
                        uavDescription.Dimension = UnorderedAccessViewDimension.Texture3D;
                        uavDescription.Texture3D.FirstWSlice = arrayOrDepthSlice;
                        uavDescription.Texture3D.MipSlice = mipIndex;
                        uavDescription.Texture3D.WSize = arrayCount;
                        break;
                    case TextureDimension.TextureCube:
                        throw new NotSupportedException("TextureCube dimension is expecting an array size > 1");
                }
            }

            return new UnorderedAccessView(GraphicsDevice.NativeDevice, NativeResource, uavDescription);
        }

        private DepthStencilView GetDepthStencilView(out bool hasStencil)
        {
            hasStencil = false;
            if (!IsDepthStencil)
                return null;

            // Check that the format is supported
            if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
                throw new NotSupportedException("Depth stencil format [{0}] not supported".ToFormat(ViewFormat));

            // Setup the HasStencil flag
            hasStencil = IsStencilFormat(ViewFormat);

            // Create a Depth stencil view on this texture2D
            var depthStencilViewDescription = new DepthStencilViewDescription
            {
                Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                Flags = DepthStencilViewFlags.None,
            };

            if (ArraySize > 1)
            {
                depthStencilViewDescription.Dimension = DepthStencilViewDimension.Texture2DArray;
                depthStencilViewDescription.Texture2DArray.ArraySize = ArraySize;
                depthStencilViewDescription.Texture2DArray.FirstArraySlice = 0;
                depthStencilViewDescription.Texture2DArray.MipSlice = 0;
            }
            else
            {
                depthStencilViewDescription.Dimension = DepthStencilViewDimension.Texture2D;
                depthStencilViewDescription.Texture2D.MipSlice = 0;
            }

            if (MultisampleCount > MultisampleCount.None)
                depthStencilViewDescription.Dimension = DepthStencilViewDimension.Texture2DMultisampled;

            if (IsDepthStencilReadOnly)
            {
                if (!IsDepthStencilReadOnlySupported(GraphicsDevice))
                    throw new NotSupportedException("Cannot instantiate ReadOnly DepthStencilBuffer. Not supported on this device.");

                // Create a Depth stencil view on this texture2D
                depthStencilViewDescription.Flags = DepthStencilViewFlags.ReadOnlyDepth;
                if (HasStencil)
                    depthStencilViewDescription.Flags |= DepthStencilViewFlags.ReadOnlyStencil;
            }

            return new DepthStencilView(GraphicsDevice.NativeDevice, NativeResource, depthStencilViewDescription);
        }

        internal static BindFlags GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            var result = BindFlags.None;
            if ((flags & TextureFlags.ShaderResource) != 0)
                result |= BindFlags.ShaderResource;
            if ((flags & TextureFlags.RenderTarget) != 0)
                result |= BindFlags.RenderTarget;
            if ((flags & TextureFlags.UnorderedAccess) != 0)
                result |= BindFlags.UnorderedAccess;
            if ((flags & TextureFlags.DepthStencil) != 0)
                result |= BindFlags.DepthStencil;

            return result;
        }

        internal static unsafe SharpDX.DataBox[] ConvertDataBoxes(DataBox[] dataBoxes)
        {
            if (dataBoxes == null || dataBoxes.Length == 0)
                return null;

            var sharpDXDataBoxes = new SharpDX.DataBox[dataBoxes.Length];
            fixed (void* pDataBoxes = sharpDXDataBoxes)
                Utilities.Write((IntPtr)pDataBoxes, dataBoxes, 0, dataBoxes.Length);

            return sharpDXDataBoxes;
        }

        private bool IsFlipped()
        {
            return false;
        }

        private Texture1DDescription ConvertToNativeDescription1D()
        {
            var desc = new Texture1DDescription()
            {
                Width = textureDescription.Width,
                ArraySize = 1,
                BindFlags = GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (SharpDX.DXGI.Format)textureDescription.Format,
                MipLevels = textureDescription.MipLevels,
                Usage = (ResourceUsage)textureDescription.Usage,
                CpuAccessFlags = GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                OptionFlags = (ResourceOptionFlags)textureDescription.Options,
            };
            return desc;
        }

        private SharpDX.DXGI.Format ComputeShaderResourceViewFormat()
        {
            // Special case for DepthStencil ShaderResourceView that are bound as Float
            var viewFormat = (SharpDX.DXGI.Format)ViewFormat;
            if (IsDepthStencil)
            {
                viewFormat = (SharpDX.DXGI.Format)ComputeShaderResourceFormatFromDepthFormat(ViewFormat);
            }

            return viewFormat;
        }

        private static TextureDescription ConvertFromNativeDescription(Texture2DDescription description)
        {
            var desc = new TextureDescription()
            {
                Dimension = TextureDimension.Texture2D,
                Width = description.Width,
                Height = description.Height,
                Depth = 1,
                MultisampleCount = (MultisampleCount)description.SampleDescription.Count,
                Format = (PixelFormat)description.Format,
                MipLevels = description.MipLevels,
                Usage = (GraphicsResourceUsage)description.Usage,
                ArraySize = description.ArraySize,
                Flags = TextureFlags.None,
                Options = TextureOptions.None
            };

            if ((description.BindFlags & BindFlags.RenderTarget) != 0)
                desc.Flags |= TextureFlags.RenderTarget;
            if ((description.BindFlags & BindFlags.UnorderedAccess) != 0)
                desc.Flags |= TextureFlags.UnorderedAccess;
            if ((description.BindFlags & BindFlags.DepthStencil) != 0)
                desc.Flags |= TextureFlags.DepthStencil;
            if ((description.BindFlags & BindFlags.ShaderResource) != 0)
                desc.Flags |= TextureFlags.ShaderResource;

            if ((description.OptionFlags & ResourceOptionFlags.Shared) != 0)
                desc.Options |= TextureOptions.Shared;
#if XENKO_GRAPHICS_API_DIRECT3D11
            if ((description.OptionFlags & ResourceOptionFlags.SharedKeyedmutex) != 0)
                desc.Options |= TextureOptions.SharedKeyedmutex;
            if ((description.OptionFlags & ResourceOptionFlags.SharedNthandle) != 0)
                desc.Options |= TextureOptions.SharedNthandle;
#endif
            return desc;
        }

        private Texture2DDescription ConvertToNativeDescription2D()
        {
            var format = (SharpDX.DXGI.Format)textureDescription.Format;
            var flags = textureDescription.Flags;

            // If the texture is going to be bound on the depth stencil, for to use TypeLess format
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
                        switch (textureDescription.Format)
                        {
                            case PixelFormat.D16_UNorm:
                                format = SharpDX.DXGI.Format.D16_UNorm;
                                break;
                            case PixelFormat.D32_Float:
                                format = SharpDX.DXGI.Format.D32_Float;
                                break;
                            case PixelFormat.D24_UNorm_S8_UInt:
                                format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt;
                                break;
                            case PixelFormat.D32_Float_S8X24_UInt:
                                format = SharpDX.DXGI.Format.D32_Float_S8X24_UInt;
                                break;
                            default:
                                throw new NotSupportedException($"Unsupported DepthFormat [{textureDescription.Format}] for depth buffer");
                        }
                    }
                    else
                    {
                        switch (textureDescription.Format)
                        {
                            case PixelFormat.D16_UNorm:
                                format = SharpDX.DXGI.Format.R16_Typeless;
                                break;
                            case PixelFormat.D32_Float:
                                format = SharpDX.DXGI.Format.R32_Typeless;
                                break;
                            case PixelFormat.D24_UNorm_S8_UInt:
                                //format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt;
                                format = SharpDX.DXGI.Format.R24G8_Typeless;
                                break;
                            case PixelFormat.D32_Float_S8X24_UInt:
                                format = SharpDX.DXGI.Format.R32G8X24_Typeless;
                                break;
                            default:
                                throw new NotSupportedException($"Unsupported DepthFormat [{textureDescription.Format}] for depth buffer");
                        }
                    }
                }
            }

            int quality = 0;
            if (GraphicsDevice.Features.CurrentProfile >= GraphicsProfile.Level_10_1 && textureDescription.IsMultisample)
                quality = (int)StandardMultisampleQualityLevels.StandardMultisamplePattern;

            var desc = new Texture2DDescription()
            {
                Width = textureDescription.Width,
                Height = textureDescription.Height,
                ArraySize = textureDescription.ArraySize,
                SampleDescription = new SharpDX.DXGI.SampleDescription((int)textureDescription.MultisampleCount, quality),
                BindFlags = GetBindFlagsFromTextureFlags(flags),
                Format = format,
                MipLevels = textureDescription.MipLevels,
                Usage = (ResourceUsage)textureDescription.Usage,
                CpuAccessFlags = GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                OptionFlags = (ResourceOptionFlags)textureDescription.Options,
            };

            if (textureDescription.Dimension == TextureDimension.TextureCube)
                desc.OptionFlags = ResourceOptionFlags.TextureCube;

            return desc;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            PixelFormat viewFormat;

            // Determine TypeLess Format and ShaderResourceView Format
            switch (format)
            {
                case PixelFormat.R16_Typeless:
                case PixelFormat.D16_UNorm:
                    viewFormat = PixelFormat.R16_Float;
                    break;
                case PixelFormat.R32_Typeless:
                case PixelFormat.D32_Float:
                    viewFormat = PixelFormat.R32_Float;
                    break;
                case PixelFormat.R24G8_Typeless:
                case PixelFormat.D24_UNorm_S8_UInt:
                    viewFormat = PixelFormat.R24_UNorm_X8_Typeless;
                    break;
                case PixelFormat.R32_Float_X8X24_Typeless:
                case PixelFormat.D32_Float_S8X24_UInt:
                    viewFormat = PixelFormat.R32_Float_X8X24_Typeless;
                    break;
                default:
                    viewFormat = PixelFormat.None;
                    break;
            }

            return viewFormat;
        }

        internal static SharpDX.DXGI.Format ComputeDepthViewFormatFromTextureFormat(PixelFormat format)
        {
            SharpDX.DXGI.Format viewFormat;

            switch (format)
            {
                case PixelFormat.R16_Typeless:
                case PixelFormat.D16_UNorm:
                    viewFormat = SharpDX.DXGI.Format.D16_UNorm;
                    break;
                case PixelFormat.R32_Typeless:
                case PixelFormat.D32_Float:
                    viewFormat = SharpDX.DXGI.Format.D32_Float;
                    break;
                case PixelFormat.R24G8_Typeless:
                case PixelFormat.D24_UNorm_S8_UInt:
                    viewFormat = SharpDX.DXGI.Format.D24_UNorm_S8_UInt;
                    break;
                case PixelFormat.R32G8X24_Typeless:
                case PixelFormat.D32_Float_S8X24_UInt:
                    viewFormat = SharpDX.DXGI.Format.D32_Float_S8X24_UInt;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported depth format [{format}]");
            }

            return viewFormat;
        }

        private Texture3DDescription ConvertToNativeDescription3D()
        {
            var desc = new Texture3DDescription()
            {
                Width = textureDescription.Width,
                Height = textureDescription.Height,
                Depth = textureDescription.Depth,
                BindFlags = GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Format = (SharpDX.DXGI.Format)textureDescription.Format,
                MipLevels = textureDescription.MipLevels,
                Usage = (ResourceUsage)textureDescription.Usage,
                CpuAccessFlags = GetCpuAccessFlagsFromUsage(textureDescription.Usage),
                OptionFlags = (ResourceOptionFlags)textureDescription.Options,
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
            if (device.Features.CurrentProfile < GraphicsProfile.Level_10_0 && (description.Flags & TextureFlags.DepthStencil) == 0 && description.Format.IsCompressed())
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
        /// <exception cref="System.ArgumentOutOfRangeException">Value must be > 0;size</exception>
        private static int CalculateMipCountFromSize(int size, int minimumSizeLastMip = 4)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("Value must be > 0", "size");
            }

            if (minimumSizeLastMip <= 0)
            {
                throw new ArgumentOutOfRangeException("Value must be > 0", "minimumSizeLastMip");
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
        /// <exception cref="System.ArgumentOutOfRangeException">Value must be &gt; 0;size</exception>
        private static int CalculateMipCount(int width, int height, int minimumSizeLastMip = 4)
        {
            return Math.Min(CalculateMipCountFromSize(width, minimumSizeLastMip), CalculateMipCountFromSize(height, minimumSizeLastMip));
        }

        internal static bool IsStencilFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R24G8_Typeless:
                case PixelFormat.D24_UNorm_S8_UInt:
                case PixelFormat.R32G8X24_Typeless:
                case PixelFormat.D32_Float_S8X24_UInt:
                    return true;
            }

            return false;
        }
    }
}
#endif
