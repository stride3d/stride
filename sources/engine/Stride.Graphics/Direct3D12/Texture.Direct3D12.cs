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

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.UnsafeExtensions;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class Texture
    {
        private const int TextureRowPitchAlignment = D3D12.TextureDataPitchAlignment;
        private const int TextureSubresourceAlignment = D3D12.TextureDataPlacementAlignment;

        private int TexturePixelSize => Format.SizeInBytes;

        /// <summary>
        ///   A handle to the CPU-accessible Render Target View (RTV) Descriptor.
        /// </summary>
        internal CpuDescriptorHandle NativeRenderTargetView;
        /// <summary>
        ///   A handle to the CPU-accessible Depth-Stencil View (DSV) Descriptor.
        /// </summary>
        internal CpuDescriptorHandle NativeDepthStencilView;

        internal ResourceDesc NativeTextureDescription;

        /// <summary>
        ///   A value indicating whether the Texture is a Depth-Stencil Buffer with a stencil component.
        /// </summary>
        public bool HasStencil;


        /// <summary>
        ///   Recreates this Texture explicitly with the provided data. Usually called after the <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <param name="dataBoxes">
        ///   An array of <see cref="DataBox"/> structures that contain the initial data for the Texture's
        ///   sub-resources (the mip-levels in the mipmap chain).
        ///   Specify <see langword="null"/> if no initial data is needed.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   The Texture is initialized with <see cref="GraphicsResourceUsage.Staging"/> and staging Textures
        ///   cannot be created with initial data (<paramref name="dataBoxes"/> must be <see langword="null"/> or empty).
        /// </exception>
        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        /// <summary>
        ///   Determines if a Graphics Device supports read-only Depth-Stencil Buffers.
        /// </summary>
        /// <param name="device">The Graphics Device to check.</param>
        /// <returns>
        ///   <see langword="true"/> if the Graphics Device supports read-only Depth-Stencil Buffers;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            return device.Features.CurrentProfile >= GraphicsProfile.Level_11_0;
        }

        /// <inheritdoc/>
        internal override void SwapInternal(GraphicsResourceBase other)
        {
            var otherTexture = (Texture)other;

            base.SwapInternal(other);

            (NativeRenderTargetView, otherTexture.NativeRenderTargetView)     = (otherTexture.NativeRenderTargetView, NativeRenderTargetView);
            (NativeDepthStencilView, otherTexture.NativeDepthStencilView)     = (otherTexture.NativeDepthStencilView, NativeDepthStencilView);
            (NativeTextureDescription, otherTexture.NativeTextureDescription) = (otherTexture.NativeTextureDescription, NativeTextureDescription);
            (HasStencil, otherTexture.HasStencil)                             = (otherTexture.HasStencil, HasStencil);
        }

        /// <summary>
        ///   Initializes the <see cref="Texture"/> from a native <see cref="ID3D12Resource"/>.
        /// </summary>
        /// <param name="texture">The underlying native Texture.</param>
        /// <param name="treatAsSrgb">
        ///   <see langword="true"/> to treat the Texture's pixel format as if it were an sRGB format, even if it was created as non-sRGB;
        ///   <see langword="false"/> to respect the Texture's original pixel format.
        /// </param>
        /// <returns>This Texture after being initialized.</returns>
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


            //
            // Converts a Silk.NET's ResourceDesc structure to a TextureDescription.
            //
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

        /// <summary>
        ///   Initializes the Texture from the specified data.
        /// </summary>
        /// <param name="dataBoxes">
        ///   An array of <see cref="DataBox"/> structures pointing to the data for all the subresources to
        ///   initialize for the Texture.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Invalid Texture share options (<see cref="TextureOptions"/>) specified.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Invalid Texture dimension (<see cref="TextureDimension"/>) specified.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is only supported for 2D Textures.</exception>
        /// <exception cref="NotSupportedException">A Texture Cube must have an array size greater than 1.</exception>
        /// <exception cref="NotSupportedException">Texture Arrays are not supported for 3D Textures.</exception>
        /// <exception cref="NotSupportedException"><see cref="ViewType.MipBand"/> is not supported for Render Targets.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is not supported for Unordered Access Views.</exception>
        /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
        /// <exception cref="NotSupportedException">Cannot create a read-only Depth-Stencil View because the device does not support it.</exception>
        /// <exception cref="NotSupportedException">
        ///   For a <see cref="GraphicsProfile"/> lower than <see cref="GraphicsProfile.Level_10_0"/>, creating Shader Resource Views
        ///   for Depth-Stencil Textures is not supported,
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The Texture is initialized with <see cref="GraphicsResourceUsage.Staging"/> and staging Textures
        ///   cannot be created with initial data (<paramref name="dataBoxes"/> must be <see langword="null"/> or empty).
        /// </exception>
        private partial void InitializeFromImpl(DataBox[] dataBoxes)
        {
            // If this is a view, get the underlying resource to copy data to
            if (ParentTexture is not null)
            {
                SetNativeDeviceChild(ParentTexture.NativeDeviceChild);
            }

            // If no underlying resource, we must create it and copy the init data to it if needed
            if (NativeDeviceChild.IsNull())
            {
                if (Usage == GraphicsResourceUsage.Staging)
                {
                    // Per our own definition of staging resource (read-back only)
                    if (dataBoxes?.Length > 0)
                        throw new NotSupportedException("D3D12: Staging Textures can't be created with initial data.");

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


            //
            // Initializes the Texture as a staging texture, which in Direct3D 12 is essentially a Buffer,
            // and copies the initialization data to that Buffer.
            //
            void InitializeStagingTexture()
            {
                NativeResourceState = ResourceStates.CopyDest;
                NativeTextureDescription = GetTextureDescription(Dimension);

                int totalSize = ComputeBufferTotalSize();
                ResourceDesc nativeDescription = CreateDescriptionForBuffer((ulong) totalSize);

                HeapProperties heap = new HeapProperties { Type = HeapType.Readback };

                HResult result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in nativeDescription, NativeResourceState, pOptimizedClearValue: null,
                                                                      out ComPtr<ID3D12Resource> stagingTextureResource);
                if (result.IsFailure)
                    result.Throw();

                SetNativeDeviceChild(stagingTextureResource.AsDeviceChild());
            }

            //
            // Initializes the Texture as a regular texture resource and copies the initial data
            // to that new texture.
            //
            void InitializeTexture(DataBox[] initialData)
            {
                bool hasClearValue = GetClearValue(out ClearValue clearValue);
                scoped ref readonly var clearValueRef = ref hasClearValue
                    ? ref clearValue
                    : ref NullRef<ClearValue>();

                var nativeDescription = NativeTextureDescription = GetTextureDescription(Dimension);

                var desiredResourceState = NativeResourceState;
                var currentResourceState = desiredResourceState;

                bool hasInitData = initialData?.Length > 0;

                // If the resource must be initialized with data, it is initially in the state
                // CopyDest so we can copy from an upload buffer
                if (hasInitData)
                    currentResourceState = ResourceStates.CopyDest;

                // TODO: D3D12: Move that to a global allocator in bigger committed resources
                var heap = new HeapProperties { Type = HeapType.Default };

                HResult result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in nativeDescription, currentResourceState,
                                                                      in clearValueRef, out ComPtr<ID3D12Resource> textureResource);
                if (result.IsFailure)
                    result.Throw();

                SetNativeDeviceChild(textureResource.AsDeviceChild());
                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);

                if (hasInitData || currentResourceState != desiredResourceState)
                {
                    var commandList = GraphicsDevice.NativeCopyCommandList;
                    lock (GraphicsDevice.NativeCopyCommandListLock)
                    {
                        scoped ref var nullPipelineState = ref NullRef<ID3D12PipelineState>();
                        result = commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, pInitialState: ref nullPipelineState);

                        if (result.IsFailure)
                            result.Throw();

                        if (hasInitData)
                        {
                            var subresourceCount = initialData.Length;
                            scoped Span<PlacedSubresourceFootprint> placedSubresources = stackalloc PlacedSubresourceFootprint[subresourceCount];
                            scoped Span<uint> rowCounts = stackalloc uint[subresourceCount];
                            scoped Span<ulong> rowSizeInBytes = stackalloc ulong[subresourceCount];

                            ulong textureCopySize = 0;

                            NativeDevice.GetCopyableFootprints(in nativeDescription, FirstSubresource: 0, (uint) subresourceCount, BaseOffset: 0,
                                                               ref placedSubresources.GetReference(),
                                                               ref rowCounts.GetReference(),
                                                               ref rowSizeInBytes.GetReference(),
                                                               ref textureCopySize);

                            nint uploadMemory = GraphicsDevice.AllocateUploadBuffer((int) textureCopySize,
                                                                                    out ComPtr<ID3D12Resource> uploadResource,
                                                                                    out int uploadOffset,
                                                                                    D3D12.TextureDataPlacementAlignment);
                            for (int i = 0; i < subresourceCount; ++i)
                            {
                                scoped ref readonly var databox = ref initialData[i];
                                scoped ref var placedSubresource = ref placedSubresources[i];

                                var dataPointer = databox.DataPointer;

                                var rowCount = rowCounts[i];
                                var sliceCount = placedSubresource.Footprint.Depth;
                                var rowSize = (int) rowSizeInBytes[i];
                                var destRowPitch = placedSubresource.Footprint.RowPitch;

                                // Copy the init data to the upload buffer
                                for (int zSlice = 0; zSlice < sliceCount; zSlice++)
                                {
                                    var uploadMemoryCurrent = uploadMemory + (int) placedSubresource.Offset + zSlice * destRowPitch * rowCount;
                                    var dataPointerCurrent = dataPointer + zSlice * databox.SlicePitch;

                                    for (int row = 0; row < rowCount; ++row)
                                    {
                                        MemoryUtilities.CopyWithAlignmentFallback((void*) uploadMemoryCurrent, (void*) dataPointerCurrent, (uint) rowSize);
                                        uploadMemoryCurrent += destRowPitch;
                                        dataPointerCurrent += databox.RowPitch;
                                    }
                                }

                                // Adjust upload offset (circular dependency between GetCopyableFootprints and AllocateUploadBuffer)
                                placedSubresource.Offset += (ulong) uploadOffset;

                                var dest = new TextureCopyLocation { Type = TextureCopyType.SubresourceIndex, PResource = NativeResource, SubresourceIndex = (uint) i };
                                var src = new TextureCopyLocation { Type = TextureCopyType.PlacedFootprint, PResource = uploadResource, PlacedFootprint = placedSubresource };

                                commandList.CopyTextureRegion(in dest, DstX: 0, DstY: 0, DstZ: 0, in src, pSrcBox: in NullRef<Box>());
                            }
                        }

                        const uint D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = 0xFFFFFFFF;

                        // Once initialized, transition the Texture (and its subresources) to its final state
                        var resourceBarrier = new ResourceBarrier { Type = ResourceBarrierType.Transition };
                        resourceBarrier.Transition.PResource = NativeResource;
                        resourceBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                        resourceBarrier.Transition.StateBefore = currentResourceState;
                        resourceBarrier.Transition.StateAfter = desiredResourceState;

                        commandList.ResourceBarrier(1, in resourceBarrier);

                        result = commandList.Close();

                        if (result.IsFailure)
                            result.Throw();

                        var copyFenceValue = GraphicsDevice.ExecuteAndWaitCopyQueueGPU();

                        // Make sure any subsequent CPU access (i.e. MapSubresource) will wait for copy command list to be finished
                        CopyFenceValue = copyFenceValue;
                    }
                }

                NativeResourceState = desiredResourceState;
            }

            //
            // Gets the most appropriate clear value for the resource, or no clear value if the
            // resource does not need one.
            //
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

            //
            // Constructs the appropriate Direct3D 12 resource description for the specified texture type.
            //
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

            //
            // Creates a description for a Buffer with the specified size.
            //
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

            //
            // Gets a specific Shader Resource View from the Texture.
            //
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

            //
            // Gets a specific Render Target View from the Texture.
            //
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

            //
            // Gets a Depth-Stencil View from the Texture.
            //
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

            //
            // Gets a specific Unordered Access View from the Texture.
            //
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

            //
            // Given a pixel format, returns the corresponding Silk.NET's depth format.
            //
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

            //
            // Determines if a format is a Depth-Stencil format that also contains Stencil data.
            //
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

        /// <inheritdoc cref="GraphicsResourceBase.OnDestroyed" path="/summary"/>
        /// <param name="immediately">
        ///   A value indicating whether the Texture should be destroyed immediately (<see langword="true"/>),
        ///   or if it can be deferred until it's safe to do so (<see langword="false"/>).
        /// </param>
        /// <remarks>
        ///   This method releases all the native resources associated with the Texture:
        ///   <list type="bullet">
        ///     <item>
        ///       If it is a <strong>Texture</strong>, this releases the underlying native texture resource and also the associated Views.
        ///     </item>
        ///     <item>
        ///       If it is a <strong>Texture View</strong>, it releases only the resources related to the View, not the parent Texture's.
        ///     </item>
        ///   </list>
        /// </remarks>
        protected internal override void OnDestroyed(bool immediately = false)
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

            base.OnDestroyed(immediately);
        }

        /// <summary>
        ///   Perform Direct3D 12-specific recreation of the Texture.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Invalid Texture share options (<see cref="TextureOptions"/>) specified.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is only supported for 2D Textures.</exception>
        /// <exception cref="NotSupportedException">A Texture Cube must have an array size greater than 1.</exception>
        /// <exception cref="NotSupportedException">Texture Arrays are not supported for 3D Textures.</exception>
        /// <exception cref="NotSupportedException"><see cref="ViewType.MipBand"/> is not supported for Render Targets.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is not supported for Unordered Access Views.</exception>
        /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
        /// <exception cref="NotSupportedException">Cannot create a read-only Depth-Stencil View because the device does not support it.</exception>
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

        /// <summary>
        ///   Converts the specified <see cref="TextureFlags"/> to Silk.NET's <see cref="BindFlag"/>.
        /// </summary>
        /// <param name="flags">The flags to convert.</param>
        /// <returns>The corresponding <see cref="BindFlag"/>.</returns>
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

        /// <summary>
        ///   Indicates if the Texture is flipped vertically, i.e. if the rows are ordered bottom-to-top instead of top-to-bottom.
        /// </summary>
        /// <returns><see langword="true"/> if the Texture is flipped; <see langword="false"/> otherwise.</returns>
        /// <remarks>
        ///   For Direct3D, Textures are not flipped, meaning the first row is at the top and the last row is at the bottom.
        /// </remarks>
        private partial bool IsFlipped()
        {
            return false;
        }

        /// <summary>
        ///   Returns a native <see cref="ResourceDesc"/> describing a 1D Texture from the current <see cref="TextureDescription"/>.
        /// </summary>
        /// <returns>A Silk.NET's <see cref="ResourceDesc"/> describing the Texture.</returns>
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

        /// <summary>
        ///   Returns a native <see cref="ResourceDesc"/> describing a 2D Texture from the current <see cref="TextureDescription"/>.
        /// </summary>
        /// <returns>A Silk.NET's <see cref="ResourceDesc"/> describing the Texture.</returns>
        /// <exception cref="NotSupportedException">
        ///   For a <see cref="GraphicsProfile"/> lower than <see cref="GraphicsProfile.Level_10_0"/>, creating Shader Resource Views
        ///   for Depth-Stencil Textures is not supported,
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The specified pixel format is not supported for Depth-Stencil Textures.
        /// </exception>
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

        /// <summary>
        ///   Given a Depth Texture format, returns the corresponding Shader Resource View format.
        /// </summary>
        /// <param name="depthFormat">The depth format.</param>
        /// <returns>
        ///   The View format corresponding to <paramref name="depthFormat"/>,
        ///   or <see cref="PixelFormat.None"/> if no compatible format could be computed.
        /// </returns>
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

        /// <summary>
        ///   Returns a native <see cref="ResourceDesc"/> describing a 3D Texture from the current <see cref="TextureDescription"/>.
        /// </summary>
        /// <returns>A Silk.NET's <see cref="ResourceDesc"/> describing the Texture.</returns>
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
        ///   Checks a <see cref="TextureDescription"/> for invalid mip-levels and modifies the description if necessary.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="description">The Texture description to check.</param>
        /// <returns>The updated Texture description.</returns>
        /// <remarks>
        ///   This check is to prevent issues with Direct3D 9.x where the driver may not be able to create mipmaps
        ///   whose resolution in less than 4x4 pixels.
        /// </remarks>
        private static TextureDescription CheckMipLevels(GraphicsDevice device, ref TextureDescription description)
        {
            // Troubles with DXT images whose resolution in less than 4x4 in DX9.x
            // TODO: Stale comment?

            if (device.Features.CurrentProfile < GraphicsProfile.Level_10_0 &&
                !description.Flags.HasFlag(TextureFlags.DepthStencil) && description.Format.IsCompressed)
            {
                description.MipLevelCount = Math.Min(CalculateMipCount(description.Width, description.Height), description.MipLevelCount);
            }
            return description;
        }

        /// <summary>
        ///   Calculates the number of mip-levels that can be created for a specified size, taking into account
        ///   a minimum mip-level size.
        /// </summary>
        /// <param name="size">The size in pixels.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip-level. By default, this is 4 pixels.</param>
        /// <returns>The number of possible mip-levels.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   Both <paramref name="size"/> and <paramref name="minimumSizeLastMip"/> must be greater than 0.
        /// </exception>
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
        ///   Calculates the number of mip-levels that can be created for a specified size, taking into account
        ///   a minimum mip-level size.
        /// </summary>
        /// <param name="width">The width in pixels.</param>
        /// <param name="height">The height in pixels.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip-level. By default, this is 4 pixels.</param>
        /// <returns>The number of possible mip-levels.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="width"/> and <paramref name="height"/> must be greater than 0, and
        ///   <paramref name="minimumSizeLastMip"/> must also be greater than 0.
        /// </exception>
        private static int CalculateMipCount(int width, int height, int minimumSizeLastMip = 4)
        {
            return Math.Min(CalculateMipCountFromSize(width, minimumSizeLastMip), CalculateMipCountFromSize(height, minimumSizeLastMip));
        }
    }
}

#endif
