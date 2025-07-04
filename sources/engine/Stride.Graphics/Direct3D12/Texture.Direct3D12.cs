// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

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

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using Format = Silk.NET.DXGI.Format;

using Stride.Core.Mathematics;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class Texture
    {
        private const int TextureRowPitchAlignment = D3D12.TextureDataPitchAlignment;
        private const int TextureSubresourceAlignment = D3D12.TextureDataPlacementAlignment;

        private int TexturePixelSize => Format.SizeInBytes();

        internal CpuDescriptorHandle NativeRenderTargetView;
        internal CpuDescriptorHandle NativeDepthStencilView;

        public bool HasStencil;


        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            return device.Features.CurrentProfile >= GraphicsProfile.Level_11_0;
        }

        internal partial void SwapInternal(Texture other)
        {
            (NativeDeviceChild, other.NativeDeviceChild) = (other.NativeDeviceChild, NativeDeviceChild);

            (other.NativeShaderResourceView, NativeShaderResourceView)   = (NativeShaderResourceView, other.NativeShaderResourceView);
            (other.NativeUnorderedAccessView, NativeUnorderedAccessView) = (NativeUnorderedAccessView, other.NativeUnorderedAccessView);

            (StagingFenceValue, other.StagingFenceValue)           = (other.StagingFenceValue, StagingFenceValue);
            (StagingBuilder, other.StagingBuilder)                 = (other.StagingBuilder, StagingBuilder);
            (NativeResourceState, other.NativeResourceState)       = (other.NativeResourceState, NativeResourceState);
            (NativeRenderTargetView, other.NativeRenderTargetView) = (other.NativeRenderTargetView, NativeRenderTargetView);
            (NativeDepthStencilView, other.NativeDepthStencilView) = (other.NativeDepthStencilView, NativeDepthStencilView);
            (HasStencil, other.HasStencil)                         = (other.HasStencil, HasStencil);
        }

        /// <summary>
        /// Initializes from a native SharpDX.Texture
        /// </summary>
        /// <param name="texture">The texture.</param>
        internal Texture InitializeFromImpl(ID3D12Resource* texture, bool treatAsSrgb)
        {
            var ptrTexture = ToComPtr(texture);
            NativeDeviceChild = ptrTexture.AsDeviceChild();

            var newTextureDescription = ConvertFromNativeDescription(ptrTexture.GetDesc());

            // We might have created the swapchain as a non-sRGB format (specially on Win 10 & RT) but we want it to
            // behave like it is (specially for the View and Render Target)
            if (treatAsSrgb)
                newTextureDescription.Format = newTextureDescription.Format.ToSRgb();

            return InitializeFrom(newTextureDescription);


            static TextureDescription ConvertFromNativeDescription(ResourceDesc description, bool isShaderResource = false)
            {
                var desc = new TextureDescription()
                {
                    Dimension = TextureDimension.Texture2D,
                    Width = (int) description.Width,
                    Height = (int) description.Height,
                    Depth = 1,
                    MultisampleCount = (MultisampleCount) description.SampleDesc.Count,
                    Format = (PixelFormat) description.Format,
                    MipLevelCount = description.MipLevels,
                    Usage = GraphicsResourceUsage.Default,
                    ArraySize = description.DepthOrArraySize,
                    Flags = TextureFlags.None
                };

                if (description.Flags.HasFlag(ResourceFlags.AllowRenderTarget))
                    desc.Flags |= TextureFlags.RenderTarget;
                if (description.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess))
                    desc.Flags |= TextureFlags.UnorderedAccess;
                if (description.Flags.HasFlag(ResourceFlags.AllowDepthStencil))
                    desc.Flags |= TextureFlags.DepthStencil;

                if (!description.Flags.HasFlag(ResourceFlags.DenyShaderResource) && isShaderResource)
                    desc.Flags |= TextureFlags.ShaderResource;

                return desc;
            }
        }

        private void InitializeFromImpl() => InitializeFromImpl(dataBoxes: null);

        private partial void InitializeFromImpl(DataBox[] dataBoxes)
        {
            // If this is a view, get the underlying resource to copy data to
            if (ParentTexture is not null)
            {
                ParentResource = ParentTexture;
                NativeDeviceChild = ParentTexture.NativeDeviceChild;
            }

            // If no underlying resource, we must create it and copy the init data to it if needed
            if (NativeDeviceChild.IsNull())
            {
                if (Usage == GraphicsResourceUsage.Staging)
                {
                    // Per our own definition of staging resource (read-back only)
                    if (dataBoxes?.Length > 0)
                        throw new NotSupportedException("D3D12: Staging textures can't be created with initial data.");

                    // If it is a staging Texture, initialize it and finish
                    //   (staging resources do not need to have views, they are only intermediate buffers to copy
                    //   to a final destination resource)
                    InitializeStagingTexture();
                    return;
                }

                // Initialize the Texture as a regular Texture resource
                InitializeTexture(dataBoxes);
            }

            // Initialize the views on the resource
            NativeShaderResourceView = GetShaderResourceView(ViewType, ArraySlice, MipLevel);
            NativeRenderTargetView = GetRenderTargetView(ViewType, ArraySlice, MipLevel);
            NativeDepthStencilView = GetDepthStencilView(out HasStencil);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewType, ArraySlice, MipLevel);


            void InitializeStagingTexture()
            {
                NativeResourceState = ResourceStates.CopyDest;

                int totalSize = ComputeBufferTotalSize();
                ResourceDesc nativeDescription = CreateDescriptionForBuffer((ulong) totalSize);

                HeapProperties heap = new HeapProperties { Type = HeapType.Readback };

                HResult result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in nativeDescription, NativeResourceState, pOptimizedClearValue: null,
                                                                      out ComPtr<ID3D12Resource> stagingTextureResource);
                if (result.IsFailure)
                    result.Throw();

                NativeDeviceChild = stagingTextureResource.AsDeviceChild();
            }

            void InitializeTexture(DataBox[] initialData)
            {
                bool hasClearValue = GetClearValue(out ClearValue clearValue);
                scoped ref readonly var pClearValue = ref hasClearValue
                    ? ref clearValue
                    : ref NullRef<ClearValue>();

                var nativeDescription = GetTextureDescription(Dimension);

                var initialResourceState = ResourceStates.GenericRead;
                var currentResourceState = initialResourceState;

                bool hasInitData = initialData?.Length > 0;

                // If the resource must be initialized with data, it is initially in the state
                // CopyDest so we can copy from an upload buffer
                if (hasInitData)
                    currentResourceState = ResourceStates.CopyDest;

                // TODO D3D12 move that to a global allocator in bigger committed resources
                var heap = new HeapProperties { Type = HeapType.Default };

                HResult result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in nativeDescription, currentResourceState,
                                                                      in pClearValue, out ComPtr<ID3D12Resource> textureResource);
                if (result.IsFailure)
                    result.Throw();

                NativeDeviceChild = textureResource.AsDeviceChild();
                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);

                if (hasInitData)
                {
                    var commandList = GraphicsDevice.NativeCopyCommandList;
                    result = commandList->Reset(GraphicsDevice.NativeCopyCommandAllocator, pInitialState: null);

                    if (result.IsFailure)
                        result.Throw();

                    var subresourceCount = initialData.Length;
                    var placedSubresources = stackalloc PlacedSubresourceFootprint[subresourceCount];
                    var rowCounts = stackalloc uint[subresourceCount];
                    var rowSizeInBytes = stackalloc ulong[subresourceCount];

                    ulong textureCopySize = 0;

                    NativeDevice.GetCopyableFootprints(in nativeDescription, FirstSubresource: 0, (uint) subresourceCount,
                                                       BaseOffset: 0, ref placedSubresources[0], ref rowCounts[0], ref rowSizeInBytes[0],
                                                       ref textureCopySize);

                    nint uploadMemory = GraphicsDevice.AllocateUploadBuffer((int) textureCopySize, out var uploadResource, out var uploadOffset,
                                                                            D3D12.TextureDataPlacementAlignment);

                    for (int i = 0; i < subresourceCount; ++i)
                    {
                        var databox = initialData[i];
                        var dataPointer = databox.DataPointer;

                        var rowCount = rowCounts[i];
                        var sliceCount = placedSubresources[i].Footprint.Depth;
                        var rowSize = (int) rowSizeInBytes[i];
                        var destRowPitch = placedSubresources[i].Footprint.RowPitch;

                        // Copy the init data to the upload buffer
                        for (int z = 0; z < sliceCount; ++z)
                        {
                            var uploadMemoryCurrent = uploadMemory + (int) placedSubresources[i].Offset + z * destRowPitch * rowCount;
                            var dataPointerCurrent = dataPointer + z * databox.SlicePitch;
                            for (int y = 0; y < rowCount; ++y)
                            {
                                Utilities.CopyWithAlignmentFallback((void*) uploadMemoryCurrent, (void*) dataPointerCurrent, (uint) rowSize);
                                uploadMemoryCurrent += destRowPitch;
                                dataPointerCurrent += databox.RowPitch;
                            }
                        }

                        // Adjust upload offset (circular dependency between GetCopyableFootprints and AllocateUploadBuffer)
                        placedSubresources[i].Offset += (ulong) uploadOffset;

                        var dest = new TextureCopyLocation { Type = TextureCopyType.SubresourceIndex, PResource = NativeResource, SubresourceIndex = (uint) i };
                        var src = new TextureCopyLocation { Type = TextureCopyType.PlacedFootprint, PResource = uploadResource, PlacedFootprint = placedSubresources[i] };

                        commandList->CopyTextureRegion(in dest, DstX: 0, DstY: 0, DstZ: 0, in src, pSrcBox: in NullRef<Box>());
                    }

                    const uint D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = 0xFFFFFFFF;

                    // Once initialized, transition the Texture (and its subresources) to its final state
                    var resourceBarrier = new ResourceBarrier { Type = ResourceBarrierType.Transition };
                    resourceBarrier.Transition.PResource = NativeResource;
                    resourceBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                    resourceBarrier.Transition.StateBefore = ResourceStates.CopyDest;
                    resourceBarrier.Transition.StateAfter = initialResourceState;

                    commandList->ResourceBarrier(1, in resourceBarrier);

                    result = commandList->Close();

                    if (result.IsFailure)
                        result.Throw();

                    GraphicsDevice.WaitCopyQueue();
                }

                NativeResourceState = initialResourceState;
            }

            bool GetClearValue(out ClearValue clearValue)
            {
                if (IsDepthStencil)
                {
                    clearValue = new ClearValue
                    {
                        Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                        DepthStencil = new DepthStencilValue
                        {
                            Depth = 1.0f,
                            Stencil = 0
                        }
                    };
                    return true;
                }
                else if (IsRenderTarget)
                {
                    var clearColor = new ClearValue { Format = (Format) textureDescription.Format };
                    AsRef<Color4>(clearColor.Anonymous.Color) = Color4.Black;
                    clearValue = clearColor;
                    return true;
                }
                else
                {
                    clearValue = default;
                    return false;
                }
            }

            ResourceDesc GetTextureDescription(TextureDimension textureDimension)
            {
                return textureDimension switch
                {
                    TextureDimension.Texture1D => ConvertToNativeDescription1D(),

                    TextureDimension.Texture2D or
                    TextureDimension.TextureCube => ConvertToNativeDescription2D(),

                    TextureDimension.Texture3D => ConvertToNativeDescription3D(),

                    _ => throw new ArgumentOutOfRangeException(nameof(textureDimension))
                };
            }

            ResourceDesc CreateDescriptionForBuffer(ulong bufferSize)
            {
                return new ResourceDesc
                {
                    Dimension = ResourceDimension.Buffer,
                    Width = bufferSize,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,

                    Format = Silk.NET.DXGI.Format.FormatUnknown,
                    SampleDesc = { Count = 1, Quality = 0 },

                    Flags = ResourceFlags.None,
                    Layout = TextureLayout.LayoutRowMajor,
                    Alignment = 0
                };
            }

        /// <summary>
        /// Gets a specific <see cref="ShaderResourceView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">The mip map slice index.</param>
        /// <returns>An <see cref="ShaderResourceView" /></returns>
            CpuDescriptorHandle GetShaderResourceView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
            {
                if (!IsShaderResource)
                    return default;

                GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out var mipCount);

                // Create the view
                // TODO: D3D12: Shader4ComponentMapping is now set to default value D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING (0x00001688); need better control
                var srvDescription = new ShaderResourceViewDesc
                {
                    Shader4ComponentMapping = 0b1_011_010_001_000, // D3D12_ENCODE_SHADER_4_COMPONENT_MAPPING(0,1,2,3)
                    Format = ComputeShaderResourceViewFormat()
                };

                // Initialize for Texture Arrays or Texture Cube
                if (ArraySize > 1)
                {
                    // If Texture Cube
                    if (Dimension == TextureDimension.TextureCube && viewType == ViewType.Full)
                    {
                        srvDescription.ViewDimension = SrvDimension.Texturecube;
                        srvDescription.TextureCube.MipLevels = (uint) mipCount;
                        srvDescription.TextureCube.MostDetailedMip = (uint) mipIndex;
                    }
                    else // Texture array
                    {
                        if (IsMultiSampled)
                        {
                            if (Dimension != TextureDimension.Texture2D)
                            {
                                throw new NotSupportedException("Multisample is only supported for 2D Textures");
                            }
                            srvDescription.ViewDimension = SrvDimension.Texture2Dmsarray;
                            srvDescription.Texture2DMSArray.ArraySize = (uint) arrayCount;
                            srvDescription.Texture2DMSArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                        }
                        else
                        {
                            srvDescription.ViewDimension = Dimension is TextureDimension.Texture2D or TextureDimension.TextureCube
                                ? SrvDimension.Texture2Darray
                                : SrvDimension.Texture1Darray;

                            srvDescription.Texture2DArray.ArraySize = (uint) arrayCount;
                            srvDescription.Texture2DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                            srvDescription.Texture2DArray.MipLevels = (uint) mipCount;
                            srvDescription.Texture2DArray.MostDetailedMip = (uint) mipIndex;
                        }
                    }
                }
                else // Regular Texture (1D, 2D, 3D)
                {
                    if (IsMultiSampled)
                    {
                        if (Dimension != TextureDimension.Texture2D)
                        {
                            throw new NotSupportedException("Multisample is only supported for 2D Textures");
                        }
                        srvDescription.ViewDimension = SrvDimension.Texture2Dms;
                    }
                    else // Non-multisampled Texture
                    {
                        switch (Dimension)
                        {
                            case TextureDimension.Texture1D:
                                srvDescription.ViewDimension = SrvDimension.Texture1D;
                                break;

                            case TextureDimension.Texture2D:
                                srvDescription.ViewDimension = SrvDimension.Texture2D;
                                break;

                            case TextureDimension.Texture3D:
                                srvDescription.ViewDimension = SrvDimension.Texture3D;
                                break;

                            case TextureDimension.TextureCube:
                                throw new NotSupportedException("A Texture Cube must have an ArraySize > 1");
                        }
                        // Use srvDescription.Texture1D as it matches also Texture2D and Texture3D memory layout
                        srvDescription.Texture1D.MipLevels = (uint) mipCount;
                        srvDescription.Texture1D.MostDetailedMip = (uint) mipIndex;
                    }
                }

                var descriptorHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

                NativeDevice.CreateShaderResourceView(NativeResource, in srvDescription, descriptorHandle);
                return descriptorHandle;
            }

            CpuDescriptorHandle GetRenderTargetView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
            {
                if (!IsRenderTarget)
                    return default;

                if (viewType == ViewType.MipBand)
                    throw new NotSupportedException($"The view type [{nameof(ViewType)}.{nameof(ViewType.MipBand)}] is not supported for Render Targets");

                GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out _);

                var rtvDescription = new RenderTargetViewDesc { Format = (Format) ViewFormat };

                // Initialize for Texture Arrays or Texture Cube
                if (ArraySize > 1)
                {
                    if (MultisampleCount > MultisampleCount.None)
                    {
                        if (Dimension != TextureDimension.Texture2D)
                        {
                            throw new NotSupportedException("Multisample is only supported for 2D Textures");
                        }
                        rtvDescription.ViewDimension = RtvDimension.Texture2Dmsarray;
                        rtvDescription.Texture2DMSArray.ArraySize = (uint)arrayCount;
                        rtvDescription.Texture2DMSArray.FirstArraySlice = (uint)arrayOrDepthSlice;
                    }
                    else // Non-multisampled
                    {
                        if (Dimension == TextureDimension.Texture3D)
                        {
                            throw new NotSupportedException("Texture Array is not supported for 3D Textures");
                        }
                        rtvDescription.ViewDimension = Dimension is TextureDimension.Texture2D or TextureDimension.TextureCube
                            ? RtvDimension.Texture2Darray
                            : RtvDimension.Texture1Darray;

                        // Use rtvDescription.Texture1DArray as it matches also Texture memory layout
                        rtvDescription.Texture1DArray.ArraySize = (uint) arrayCount;
                        rtvDescription.Texture1DArray.FirstArraySlice = (uint) arrayOrDepthSlice;
                        rtvDescription.Texture1DArray.MipSlice = (uint) mipIndex;
                    }
                }
                else // Regular Texture (1D, 2D, 3D)
                {
                    if (IsMultiSampled)
                    {
                        if (Dimension != TextureDimension.Texture2D)
                        {
                            throw new NotSupportedException("Multisample is only supported for 2D Render Target Textures");
                        }
                        rtvDescription.ViewDimension = RtvDimension.Texture2Dms;
                    }
                    else // Non-multisampled Texture
                    {
                        switch (Dimension)
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
                                throw new NotSupportedException("A Texture Cube must have an ArraySize > 1");
                        }
                    }
                }

                var descriptorHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);

                NativeDevice.CreateRenderTargetView(NativeResource, in rtvDescription, descriptorHandle);
                return descriptorHandle;
            }

        /// <summary>
        /// Gets a specific <see cref="RenderTargetView" /> from this texture.
        /// </summary>
        /// <param name="viewType">Type of the view slice.</param>
        /// <param name="arrayOrDepthSlice">The texture array slice index.</param>
        /// <param name="mipIndex">Index of the mip.</param>
        /// <returns>An <see cref="RenderTargetView" /></returns>
        /// <exception cref="NotSupportedException">ViewSlice.MipBand is not supported for render targets</exception>
            CpuDescriptorHandle GetDepthStencilView(out bool hasStencil)
            {
                hasStencil = false;

                if (!IsDepthStencil)
                    return default;

                // Check that the format is supported
                if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
                    throw new NotSupportedException($"Depth-Stencil format [{ViewFormat}] not supported");

                hasStencil = IsStencilFormat(ViewFormat);

                // Create a Depth-Stencil View on this Texture
                var depthStencilViewDescription = new DepthStencilViewDesc
                {
                    Format = ComputeDepthViewFormatFromTextureFormat(ViewFormat),
                    Flags = DsvFlags.None
                };

                // Initialize for Texture Arrays or Texture Cube
                if (ArraySize > 1)
                {
                    depthStencilViewDescription.ViewDimension = DsvDimension.Texture2Darray;
                    depthStencilViewDescription.Texture2DArray.ArraySize = (uint) ArraySize;
                    depthStencilViewDescription.Texture2DArray.FirstArraySlice = 0;
                    depthStencilViewDescription.Texture2DArray.MipSlice = 0;
                }
                else // Regular Texture (2D)
                {
                    depthStencilViewDescription.ViewDimension = DsvDimension.Texture2D;
                    depthStencilViewDescription.Texture2D.MipSlice = 0;
                }

                if (MultisampleCount > MultisampleCount.None)
                    depthStencilViewDescription.ViewDimension = DsvDimension.Texture2Dms;

                if (IsDepthStencilReadOnly)
                {
                    if (!IsDepthStencilReadOnlySupported(GraphicsDevice))
                        throw new NotSupportedException("Cannot initialize a read-only Depth-Stencil Buffer. Not supported on this device.");

                    // Create a Depth-Stencil View on this 2D Texture
                    depthStencilViewDescription.Flags = DsvFlags.ReadOnlyDepth;
                    if (HasStencil)
                        depthStencilViewDescription.Flags |= DsvFlags.ReadOnlyStencil;
                }

                var descriptorHandle = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);

                NativeDevice.CreateDepthStencilView(NativeResource, in depthStencilViewDescription, descriptorHandle);
                return descriptorHandle;
            }

            CpuDescriptorHandle GetUnorderedAccessView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
            {
                if (!IsUnorderedAccess)
                    return default;

                if (IsMultiSampled)
                    throw new NotSupportedException("Multi-sampling is not supported for Unordered Access Views");

                GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out var arrayCount, out _);

                var uavDescription = new UnorderedAccessViewDesc
                {
                    Format = (Format) ViewFormat
                };

                // Initialize for Texture Arrays or Texture Cube
                if (ArraySize > 1)
                {
                    switch (Dimension)
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
                    uavDescription.Texture2DArray.ArraySize = (uint) arrayCount;
                    uavDescription.Texture2DArray.FirstArraySlice = (uint)arrayOrDepthSlice;
                    uavDescription.Texture2DArray.MipSlice = (uint) mipIndex;
                }
                else // Regular Texture (1D, 2D, 3D)
                {
                    switch (Dimension)
                    {
                        case TextureDimension.Texture1D:
                            uavDescription.ViewDimension = UavDimension.Texture1D;
                            uavDescription.Texture1D.MipSlice = (uint) mipIndex;
                            break;

                        case TextureDimension.Texture2D:
                            uavDescription.ViewDimension = UavDimension.Texture2D;
                            uavDescription.Texture2D.MipSlice = (uint) mipIndex;
                            break;

                        case TextureDimension.Texture3D:
                            uavDescription.ViewDimension = UavDimension.Texture3D;
                            uavDescription.Texture3D.FirstWSlice = (uint) arrayOrDepthSlice;
                            uavDescription.Texture3D.MipSlice = (uint) mipIndex;
                            uavDescription.Texture3D.WSize = (uint) arrayCount;
                            break;

                        case TextureDimension.TextureCube:
                            throw new NotSupportedException("A Texture Cube must have an ArraySize > 1");
                    }
                }

                var descriptorHandle = GraphicsDevice.UnorderedAccessViewAllocator.Allocate(1);

                NativeDevice.CreateUnorderedAccessView(NativeResource, pCounterResource: null, in uavDescription, descriptorHandle);
                return descriptorHandle;
            }

            //
            // Returns a DXGI format for a Shader Resource View that is compatible with the
            // current Texture's parameters.
            //
            Format ComputeShaderResourceViewFormat()
            {
                // Special case for Depth-Stencil Shader Resource View that are bound as Float
                var viewFormat = IsDepthStencil
                ? (Format) ComputeShaderResourceFormatFromDepthFormat(ViewFormat)
                : (Format) ViewFormat;

                return viewFormat;
            }

            static Format ComputeDepthViewFormatFromTextureFormat(PixelFormat format)
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

            static bool IsStencilFormat(PixelFormat format)
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

        protected internal override void OnDestroyed()
        {
            // If it was a View, do not release reference
            if (ParentTexture is not null)
            {
                ForgetNativeChildWithoutReleasing();
            }
            else
            {
                GraphicsDevice?.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            base.OnDestroyed();
        }

        private partial void OnRecreateImpl()
        {
            // Dependency: Wait for underlying texture to be recreated
            if (ParentTexture is not null && ParentTexture.LifetimeState != GraphicsResourceLifetimeState.Active)
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

        private static ResourceFlags GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            var result = ResourceFlags.None;

            if (flags.HasFlag(TextureFlags.RenderTarget))
                result |= ResourceFlags.AllowRenderTarget;

            if (flags.HasFlag(TextureFlags.UnorderedAccess))
                result |= ResourceFlags.AllowUnorderedAccess;

            if (flags.HasFlag(TextureFlags.DepthStencil))
            {
                result |= ResourceFlags.AllowDepthStencil;
                if (!flags.HasFlag(TextureFlags.ShaderResource))
                    result |= ResourceFlags.DenyShaderResource;
            }

            return result;
        }

        private partial bool IsFlipped()
        {
            return false;
        }

        internal ResourceDesc ConvertToNativeDescription1D()
        {
            return new ResourceDesc
            {
                Dimension = ResourceDimension.Texture1D,
                Width = (ulong) textureDescription.Width,
                Height = 1,
                DepthOrArraySize = (ushort) textureDescription.ArraySize,
                MipLevels = (ushort) textureDescription.MipLevelCount,

                Format = (Format) textureDescription.Format,
                SampleDesc = { Count = 1, Quality = 0 },

                Flags = GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Layout = TextureLayout.LayoutUnknown,
                Alignment = 0
            };
        }

        internal ResourceDesc ConvertToNativeDescription2D()
        {
            var format = (Format) textureDescription.Format;
            var flags = textureDescription.Flags;

            // If the Texture is going to be bound on the Depth-Stencil, use Typeless format
            if (IsDepthStencil)
            {
                if (IsShaderResource && GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                {
                    throw new NotSupportedException($"Creating Shader Resource Views for Depth-Stencil Buffers are not supported for Graphics Profiles < 10.0 (Current: [{GraphicsDevice.Features.CurrentProfile}])");
                }
                else
                {
                    // Determine Typeless Format and Shader Resource View Format
                    if (GraphicsDevice.Features.CurrentProfile < GraphicsProfile.Level_10_0)
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatD16Unorm,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatD32Float,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatD24UnormS8Uint,
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatD32FloatS8X24Uint,

                            _ => throw new NotSupportedException($"Unsupported Depth format [{textureDescription.Format}] for Depth Buffer")
                        };
                    }
                    else // GraphicsProfile >= 10.0
                    {
                        format = textureDescription.Format switch
                        {
                            PixelFormat.D16_UNorm => Silk.NET.DXGI.Format.FormatR16Typeless,
                            PixelFormat.D32_Float => Silk.NET.DXGI.Format.FormatR32Typeless,
                            PixelFormat.D24_UNorm_S8_UInt => Silk.NET.DXGI.Format.FormatR24G8Typeless,
                            PixelFormat.D32_Float_S8X24_UInt => Silk.NET.DXGI.Format.FormatR32G8X24Typeless,

                            _ => throw new NotSupportedException($"Unsupported Depth format [{textureDescription.Format}] for Depth Buffer")
                        };
                    }
                }
            }

            return new ResourceDesc
            {
                Dimension = ResourceDimension.Texture2D,
                Width = (ulong) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                DepthOrArraySize = (ushort) textureDescription.ArraySize,
                MipLevels = (ushort) textureDescription.MipLevelCount,

                Format = format,
                SampleDesc = { Count = (uint) textureDescription.MultisampleCount, Quality = 0 },

                Flags = GetBindFlagsFromTextureFlags(flags),
                Layout = TextureLayout.LayoutUnknown,
                Alignment = 0
            };
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            var viewFormat = format switch
            {
                PixelFormat.D16_UNorm => PixelFormat.R16_Float,
                PixelFormat.D32_Float => PixelFormat.R32_Float,
                PixelFormat.D24_UNorm_S8_UInt => PixelFormat.R24_UNorm_X8_Typeless,
                PixelFormat.D32_Float_S8X24_UInt => PixelFormat.R32_Float_X8X24_Typeless,

                _ => PixelFormat.None
            };
            return viewFormat;
        }

        internal ResourceDesc ConvertToNativeDescription3D()
        {
            return new ResourceDesc
            {
                Dimension = ResourceDimension.Texture3D,
                Width = (ulong) textureDescription.Width,
                Height = (uint) textureDescription.Height,
                DepthOrArraySize = (ushort) textureDescription.Depth,
                MipLevels = (ushort) textureDescription.MipLevelCount,

                Format = (Format) textureDescription.Format,
                SampleDesc = { Count = 1, Quality = 0 },

                Flags = GetBindFlagsFromTextureFlags(textureDescription.Flags),
                Layout = TextureLayout.LayoutUnknown,
                Alignment = 0
            };
        }

        /// <summary>
        /// Check and modify if necessary the mipmap levels of the image (Troubles with DXT images whose resolution in less than 4x4 in DX9.x).
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="description">The texture description.</param>
        /// <returns>The updated texture description.</returns>
        private static TextureDescription CheckMipLevels(GraphicsDevice device, ref TextureDescription description)
        {
            // Troubles with DXT images whose resolution in less than 4x4 in DX9.x
            // TODO: Stale comment?

            if (device.Features.CurrentProfile < GraphicsProfile.Level_10_0 &&
                !description.Flags.HasFlag(TextureFlags.DepthStencil) && description.Format.IsCompressed())
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
            return Math.Min(CalculateMipCountFromSize(width, minimumSizeLastMip), CalculateMipCountFromSize(height, minimumSizeLastMip));
        }
    }
}

#endif
