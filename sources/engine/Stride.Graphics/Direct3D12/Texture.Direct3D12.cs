// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D12
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
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D12;
using SharpDX.Mathematics.Interop;
using Xenko.Core;

namespace Xenko.Graphics
{
    public partial class Texture
    {
        internal CpuDescriptorHandle NativeRenderTargetView;
        internal CpuDescriptorHandle NativeDepthStencilView;
        public bool HasStencil;

        private int TexturePixelSize => Format.SizeInBytes();
        // D3D12_TEXTURE_DATA_PITCH_ALIGNMENT (not exposed by SharpDX)
        private const int TextureRowPitchAlignment = 256;
        // D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT (not exposed by SharpDX)
        private const int TextureSubresourceAlignment = 512;

        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            return device.Features.CurrentProfile >= GraphicsProfile.Level_11_0;
        }

        internal void SwapInternal(Texture other)
        {
            var deviceChild = NativeDeviceChild;
            NativeDeviceChild = other.NativeDeviceChild;
            other.NativeDeviceChild = deviceChild;
            //
            var srv = NativeShaderResourceView;
            NativeShaderResourceView = other.NativeShaderResourceView;
            other.NativeShaderResourceView = srv;
            //
            var uav = NativeUnorderedAccessView;
            NativeUnorderedAccessView = other.NativeUnorderedAccessView;
            other.NativeUnorderedAccessView = uav;
            //
            Utilities.Swap(ref StagingFenceValue, ref other.StagingFenceValue);
            Utilities.Swap(ref StagingBuilder, ref other.StagingBuilder);
            Utilities.Swap(ref NativeResourceState, ref other.NativeResourceState);
            Utilities.Swap(ref NativeRenderTargetView, ref other.NativeRenderTargetView);
            Utilities.Swap(ref NativeDepthStencilView, ref other.NativeDepthStencilView);
            Utilities.Swap(ref HasStencil, ref other.HasStencil);
        }

        /// <summary>
        /// Initializes from a native SharpDX.Texture
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(SharpDX.Direct3D12.Resource texture, bool isSrgb)
        {
            NativeDeviceChild = texture;
            var newTextureDescription = ConvertFromNativeDescription(texture.Description);

            // We might have created the swapchain as a non-srgb format (esp on Win10&RT) but we want it to behave like it is (esp. for the view and render target)
            if (isSrgb)
                newTextureDescription.Format = newTextureDescription.Format.ToSRgb();

            return InitializeFrom(newTextureDescription);
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            bool hasInitData = dataBoxes != null && dataBoxes.Length > 0;

            if (ParentTexture != null)
            {
                ParentResource = ParentTexture;
                NativeDeviceChild = ParentTexture.NativeDeviceChild;
            }

            if (NativeDeviceChild == null)
            {
                ClearValue? clearValue = GetClearValue();

                ResourceDescription nativeDescription;
                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                        nativeDescription = ConvertToNativeDescription1D();
                        break;
                    case TextureDimension.Texture2D:
                    case TextureDimension.TextureCube:
                        nativeDescription = ConvertToNativeDescription2D();
                        break;
                    case TextureDimension.Texture3D:
                        nativeDescription = ConvertToNativeDescription3D();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var initialResourceState = ResourceStates.GenericRead;

                var heapType = HeapType.Default;
                var currentResourceState = initialResourceState;
                if (Usage == GraphicsResourceUsage.Staging)
                {
                    heapType = HeapType.Readback;
                    NativeResourceState = ResourceStates.CopyDestination;
                    int totalSize = ComputeBufferTotalSize();
                    nativeDescription = ResourceDescription.Buffer(totalSize);

                    // Staging textures on DirectX 12 use buffer internally
                    NativeDeviceChild = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(heapType), HeapFlags.None, nativeDescription, NativeResourceState);

                    if (hasInitData)
                    {
                        var commandList = GraphicsDevice.NativeCopyCommandList;
                        commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, null);
                        
                        Resource uploadResource;
                        int uploadOffset;
                        var uploadMemory = GraphicsDevice.AllocateUploadBuffer(totalSize, out uploadResource, out uploadOffset, TextureSubresourceAlignment);
                        
                        // Copy data to the upload buffer
                        int dataBoxIndex = 0;
                        var uploadMemoryMipStart = uploadMemory;
                        for (int arraySlice = 0; arraySlice < ArraySize; arraySlice++)
                        {
                            for (int mipLevel = 0; mipLevel < MipLevels; mipLevel++)
                            {
                                var databox = dataBoxes[dataBoxIndex++];
                                var mipHeight = CalculateMipSize(Width, mipLevel);
                                var mipRowPitch = ComputeRowPitch(mipLevel);

                                var uploadMemoryCurrent = uploadMemoryMipStart;
                                var dataPointerCurrent = databox.DataPointer;
                                for (int rowIndex = 0; rowIndex < mipHeight; rowIndex++)
                                {
                                    Utilities.CopyMemory(uploadMemoryCurrent, dataPointerCurrent, mipRowPitch);
                                    uploadMemoryCurrent += mipRowPitch;
                                    dataPointerCurrent += databox.RowPitch;
                                }

                                uploadMemoryMipStart += ComputeSubresourceSize(mipLevel);
                            }
                        }
                        
                        // Copy from upload heap to actual resource
                        commandList.CopyBufferRegion(NativeResource, 0, uploadResource, uploadOffset, totalSize);
                        
                        commandList.Close();

                        StagingFenceValue = 0;
                        GraphicsDevice.WaitCopyQueue();
                    }

                    return;
                }

                if (hasInitData)
                    currentResourceState = ResourceStates.CopyDestination;

                // TODO D3D12 move that to a global allocator in bigger committed resources
                NativeDeviceChild = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(heapType), HeapFlags.None, nativeDescription, currentResourceState, clearValue);
                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);

                if (hasInitData)
                {
                    // Trigger copy
                    var commandList = GraphicsDevice.NativeCopyCommandList;
                    commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, null);

                    long textureCopySize;
                    var placedSubresources = new PlacedSubResourceFootprint[dataBoxes.Length];
                    var rowCounts = new int[dataBoxes.Length];
                    var rowSizeInBytes = new long[dataBoxes.Length];
                    GraphicsDevice.NativeDevice.GetCopyableFootprints(ref nativeDescription, 0, dataBoxes.Length, 0, placedSubresources, rowCounts, rowSizeInBytes, out textureCopySize);

                    SharpDX.Direct3D12.Resource uploadResource;
                    int uploadOffset;
                    var uploadMemory = GraphicsDevice.AllocateUploadBuffer((int)textureCopySize, out uploadResource, out uploadOffset, TextureSubresourceAlignment);

                    for (int i = 0; i < dataBoxes.Length; ++i)
                    {
                        var databox = dataBoxes[i];
                        var dataPointer = databox.DataPointer;

                        var rowCount = rowCounts[i];
                        var sliceCount = placedSubresources[i].Footprint.Depth;
                        var rowSize = (int)rowSizeInBytes[i];
                        var destRowPitch = placedSubresources[i].Footprint.RowPitch;

                        // Memcpy data
                        for (int z = 0; z < sliceCount; ++z)
                        {
                            var uploadMemoryCurrent = uploadMemory + (int)placedSubresources[i].Offset + z * destRowPitch * rowCount;
                            var dataPointerCurrent = dataPointer + z * databox.SlicePitch;
                            for (int y = 0; y < rowCount; ++y)
                            {
                                Utilities.CopyMemory(uploadMemoryCurrent, dataPointerCurrent, rowSize);
                                uploadMemoryCurrent += destRowPitch;
                                dataPointerCurrent += databox.RowPitch;
                            }
                        }

                        // Adjust upload offset (circular dependency between GetCopyableFootprints and AllocateUploadBuffer)
                        placedSubresources[i].Offset += uploadOffset;

                        commandList.CopyTextureRegion(new TextureCopyLocation(NativeResource, i), 0, 0, 0, new TextureCopyLocation(uploadResource, placedSubresources[i]), null);
                    }

                    commandList.ResourceBarrierTransition(NativeResource, ResourceStates.CopyDestination, initialResourceState);
                    commandList.Close();

                    GraphicsDevice.WaitCopyQueue();
                }

                NativeResourceState = initialResourceState;
            }

            NativeShaderResourceView = GetShaderResourceView(ViewType, ArraySlice, MipLevel);
            NativeRenderTargetView = GetRenderTargetView(ViewType, ArraySlice, MipLevel);
            NativeDepthStencilView = GetDepthStencilView(out HasStencil);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewType, ArraySlice, MipLevel);
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
        private CpuDescriptorHandle GetShaderResourceView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return new CpuDescriptorHandle();

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayCount, out mipCount);

            // Create the view
            // TODO D3D12 Shader4ComponentMapping is now set to default value D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING (0x00001688); need better control
            var srvDescription = new ShaderResourceViewDescription() { Shader4ComponentMapping = 0x00001688, Format = ComputeShaderResourceViewFormat() };

            // Initialize for texture arrays or texture cube
            if (this.ArraySize > 1)
            {
                // If texture cube
                if (this.Dimension == TextureDimension.TextureCube && viewType == ViewType.Full)
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
                        srvDescription.Dimension = Dimension == TextureDimension.Texture2D || Dimension == TextureDimension.TextureCube ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture1DArray;
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
                    if (Dimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D Textures");
                    }

                    srvDescription.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
                }
                else
                {
                    switch (Dimension)
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
            var descriptorHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            NativeDevice.CreateShaderResourceView(NativeResource, srvDescription, descriptorHandle);
            return descriptorHandle;
        }

        /// <summary>
        /// Gets a specific <see cref="RenderTargetView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="RenderTargetView" /></returns>
        /// <exception cref="System.NotSupportedException">ViewSlice.MipBand is not supported for render targets</exception>
        private CpuDescriptorHandle GetRenderTargetView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return new CpuDescriptorHandle();

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
                    if (Dimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D Textures");
                    }

                    rtvDescription.Dimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                    rtvDescription.Texture2DMSArray.ArraySize = arrayCount;
                    rtvDescription.Texture2DMSArray.FirstArraySlice = arrayOrDepthSlice;
                }
                else
                {
                    if (Dimension == TextureDimension.Texture3D)
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
                    if (Dimension != TextureDimension.Texture2D)
                    {
                        throw new NotSupportedException("Multisample is only supported for 2D RenderTarget Textures");
                    }

                    rtvDescription.Dimension = RenderTargetViewDimension.Texture2DMultisampled;
                }
                else
                {
                    switch (Dimension)
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

            var descriptorHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);
            NativeDevice.CreateRenderTargetView(NativeResource, rtvDescription, descriptorHandle);
            return descriptorHandle;
        }

        private CpuDescriptorHandle GetDepthStencilView(out bool hasStencil)
        {
            hasStencil = false;
            if (!IsDepthStencil)
                return new CpuDescriptorHandle();

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

            var descriptorHandle = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            NativeDevice.CreateDepthStencilView(NativeResource, depthStencilViewDescription, descriptorHandle);
            return descriptorHandle;
        }

        private CpuDescriptorHandle GetUnorderedAccessView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsUnorderedAccess)
                return new CpuDescriptorHandle();

            if (IsMultisample)
                throw new NotSupportedException("Multisampling is not supported for unordered access views");
            
            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayCount, out mipCount);

            // Create a Unordered Access view on this texture2D
            var uavDescription = new UnorderedAccessViewDescription
            {
                Format = (SharpDX.DXGI.Format)ViewFormat
            };

            if (ArraySize > 1)
            {
                switch (Dimension)
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

                uavDescription.Texture2DArray.ArraySize = arrayCount;
                uavDescription.Texture2DArray.FirstArraySlice = arrayOrDepthSlice;
                uavDescription.Texture2DArray.MipSlice = mipIndex;
            }
            else
            {
                switch (Dimension)
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
            
            var descriptorHandle = GraphicsDevice.UnorderedAccessViewAllocator.Allocate(1);
            NativeDevice.CreateUnorderedAccessView(NativeResource, null, uavDescription, descriptorHandle);
            return descriptorHandle;
        }

        internal static ResourceFlags GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            var result = ResourceFlags.None;

            if ((flags & TextureFlags.RenderTarget) != 0)
                result |= ResourceFlags.AllowRenderTarget;

            if ((flags & TextureFlags.UnorderedAccess) != 0)
                result |= ResourceFlags.AllowUnorderedAccess;

            if ((flags & TextureFlags.DepthStencil) != 0)
            {
                result |= ResourceFlags.AllowDepthStencil;
                if ((flags & TextureFlags.ShaderResource) == 0)
                    result |= ResourceFlags.DenyShaderResource;
            }

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

        private ResourceDescription ConvertToNativeDescription1D()
        {
            return ResourceDescription.Texture1D((SharpDX.DXGI.Format)textureDescription.Format, textureDescription.Width, (short)textureDescription.ArraySize, (short)textureDescription.MipLevels, GetBindFlagsFromTextureFlags(textureDescription.Flags));
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

        private static TextureDescription ConvertFromNativeDescription(ResourceDescription description, bool isShaderResource = false)
        {
            var desc = new TextureDescription()
            {
                Dimension = TextureDimension.Texture2D,
                Width = (int)description.Width,
                Height = description.Height,
                Depth = 1,
                MultisampleCount = (MultisampleCount)description.SampleDescription.Count,
                Format = (PixelFormat)description.Format,
                MipLevels = description.MipLevels,
                Usage = GraphicsResourceUsage.Default,
                ArraySize = description.DepthOrArraySize,
                Flags = TextureFlags.None
            };

            if ((description.Flags & ResourceFlags.AllowRenderTarget) != 0)
                desc.Flags |= TextureFlags.RenderTarget;
            if ((description.Flags & ResourceFlags.AllowUnorderedAccess) != 0)
                desc.Flags |= TextureFlags.UnorderedAccess;
            if ((description.Flags & ResourceFlags.AllowDepthStencil) != 0)
                desc.Flags |= TextureFlags.DepthStencil;
            if ((description.Flags & ResourceFlags.DenyShaderResource) == 0 && isShaderResource)
                desc.Flags |= TextureFlags.ShaderResource;

            return desc;
        }

        private ResourceDescription ConvertToNativeDescription2D()
        {
            var format = (SharpDX.DXGI.Format)textureDescription.Format;
            var flags = textureDescription.Flags;

            // If the texture is going to be bound on the depth stencil, for to use TypeLess format
            if (IsDepthStencil)
            {
                if (IsShaderResource && GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                {
                    throw new NotSupportedException(String.Format("ShaderResourceView for DepthStencil Textures are not supported for Graphics profile < 10.0 (Current: [{0}])", GraphicsDevice.Features.CurrentProfile));
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
                                throw new NotSupportedException(String.Format("Unsupported DepthFormat [{0}] for depth buffer", textureDescription.Format));
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
                                throw new NotSupportedException(String.Format("Unsupported DepthFormat [{0}] for depth buffer", textureDescription.Format));
                        }
                    }
                }
            }

            return ResourceDescription.Texture2D(format, textureDescription.Width, textureDescription.Height, (short)textureDescription.ArraySize, (short)textureDescription.MipLevels, (short)textureDescription.MultisampleCount, 0, GetBindFlagsFromTextureFlags(flags));
        }

        internal ClearValue? GetClearValue()
        {
            if (IsDepthStencil)
            {
                return new ClearValue
                {
                    Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                    DepthStencil = new DepthStencilValue
                    {
                        Depth = 1.0f,
                        Stencil = 0,
                    }
                };
            }

            if (IsRenderTarget)
            {
                return new ClearValue
                {
                    Format = (SharpDX.DXGI.Format)textureDescription.Format,
                    Color = new RawVector4(0, 0, 0, 1),
                };
            }

            return null;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            PixelFormat viewFormat;

            // Determine TypeLess Format and ShaderResourceView Format
            switch (format)
            {
                case PixelFormat.D16_UNorm:
                    viewFormat = PixelFormat.R16_Float;
                    break;
                case PixelFormat.D32_Float:
                    viewFormat = PixelFormat.R32_Float;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    viewFormat = PixelFormat.R24_UNorm_X8_Typeless;
                    break;
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
                    throw new NotSupportedException(String.Format("Unsupported depth format [{0}]", format));
            }

            return viewFormat;
        }

        private ResourceDescription ConvertToNativeDescription3D()
        {
            return ResourceDescription.Texture3D((SharpDX.DXGI.Format)textureDescription.Format, textureDescription.Width, textureDescription.Height, (short)textureDescription.Depth, (short)textureDescription.MipLevels, GetBindFlagsFromTextureFlags(textureDescription.Flags));
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
