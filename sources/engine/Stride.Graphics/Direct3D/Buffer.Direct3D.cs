// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;

namespace Stride.Graphics
{
    public unsafe partial class Buffer
    {
        // Internal Direct3D 11 Buffer
        private ID3D11Buffer* nativeBuffer;

        // Internal Direct3D 11 Buffer description
        private BufferDesc nativeDescription;


        /// <summary>
        ///   Internal Direct3D 11 Buffer.
        /// </summary>
        internal ID3D11Buffer* NativeBuffer
        {
            get => nativeBuffer;
            set
            {
                if (nativeBuffer == value)
                    return;

                var oldNativeBuffer = nativeBuffer;
                if (oldNativeBuffer != null)
                    oldNativeBuffer->Release();

                nativeBuffer = value;
                if (nativeBuffer != null)
                    nativeBuffer->AddRef();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            nativeDescription = ConvertToNativeDescription(Description);

            ViewFlags = viewFlags;
            InitCountAndViewFormat(out elementCount, ref viewFormat);
            ViewFormat = viewFormat;

            var subresourceData = dataPointer == IntPtr.Zero ? new SubresourceData((void*) dataPointer) : default;

            ID3D11Buffer* pBuffer = null;

            HResult result = dataPointer == IntPtr.Zero
                ? NativeDevice->CreateBuffer(in nativeDescription, pInitialData: null, ref pBuffer)
                : NativeDevice->CreateBuffer(in nativeDescription, in subresourceData, ref pBuffer);

            if (result.IsFailure)
                result.Throw();

            nativeBuffer = pBuffer;
            NativeDeviceChild = (ID3D11DeviceChild*) pBuffer;

            // Staging resource don't have any views
            if (nativeDescription.Usage != Silk.NET.Direct3D11.Usage.Staging)
                InitializeViews();

            GraphicsDevice?.RegisterBufferMemoryUsage(SizeInBytes);

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice?.RegisterBufferMemoryUsage(-SizeInBytes);

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage is GraphicsResourceUsage.Immutable or GraphicsResourceUsage.Default)
                return false;

            ID3D11Buffer* pBuffer = null;

            HResult result = NativeDevice->CreateBuffer(in nativeDescription, in Unsafe.NullRef<SubresourceData>(), ref pBuffer);

            if (result.IsFailure)
                result.Throw();

            nativeBuffer = pBuffer;
            NativeDeviceChild = (ID3D11DeviceChild*) pBuffer;

            // Staging resource don't have any views
            if (nativeDescription.Usage != Silk.NET.Direct3D11.Usage.Staging)
                InitializeViews();

            return true;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public void Recreate(IntPtr dataPointer)
        {
            ID3D11Buffer* pBuffer = null;

            var subresourceData = dataPointer == IntPtr.Zero ? new SubresourceData((void*) dataPointer) : default;

            HResult result = dataPointer == IntPtr.Zero
                ? NativeDevice->CreateBuffer(in nativeDescription, pInitialData: null, ref pBuffer)
                : NativeDevice->CreateBuffer(in nativeDescription, in subresourceData, ref pBuffer);

            if (result.IsFailure)
                result.Throw();

            nativeBuffer = pBuffer;
            NativeDeviceChild = (ID3D11DeviceChild*) pBuffer;

            // Staging resource don't have any views
            if (nativeDescription.Usage != Silk.NET.Direct3D11.Usage.Staging)
                InitializeViews();
        }

        /// <summary>
        ///   Gets a <see cref="ShaderResourceView"/> for this buffer for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="viewFormat">The view format.</param>
        /// <returns>A <see cref="ShaderResourceView"/> for the particular view format.</returns>
        /// <remarks>
        /// The buffer must have been declared with <see cref="BufferFlags.ShaderResource"/>.
        /// The ShaderResourceView instance is kept by this buffer and will be disposed when this buffer is disposed.
        /// </remarks>
        internal ID3D11ShaderResourceView* GetShaderResourceView(PixelFormat viewFormat)
        {
            ID3D11ShaderResourceView* srv = null;

            var bindFlags = (BindFlag) nativeDescription.BindFlags;

            if (bindFlags.HasFlag(BindFlag.ShaderResource))
            {
                var description = new ShaderResourceViewDesc
                {
                    Format = (Format) viewFormat,
                    ViewDimension = D3DSrvDimension.D3D11SrvDimensionBufferex,

                    BufferEx = new()
                    {
                        NumElements = (uint) ElementCount,
                        FirstElement = 0,
                        Flags = 0
                    }
                };

                if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
                {
                    description.BufferEx = description.BufferEx with
                    {
                        Flags = description.BufferEx.Flags | (uint) BufferexSrvFlag.Raw
                    };
                }

                HResult result = NativeDevice->CreateShaderResourceView(NativeResource, &description, &srv);

                if (result.IsFailure)
                    result.Throw();
            }

            return srv;
        }

        /// <summary>
        ///   Gets a <see cref="RenderTargetView" /> for this buffer for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="pixelFormat">The view format.</param>
        /// <param name="width">The width in pixels of the render target.</param>
        /// <returns>A <see cref="RenderTargetView" /> for the particular view format.</returns>
        /// <remarks>The buffer must have been declared with <see cref="BufferFlags.RenderTarget"/>.
        /// The RenderTargetView instance is kept by this buffer and will be disposed when this buffer is disposed.</remarks>
        internal ID3D11RenderTargetView* GetRenderTargetView(PixelFormat pixelFormat, int width)
        {
            ID3D11RenderTargetView* rtv = null;

            var bindFlags = (BindFlag) nativeDescription.BindFlags;

            if (bindFlags.HasFlag(BindFlag.RenderTarget))
            {
                var description = new RenderTargetViewDesc
                {
                    Format = (Format) pixelFormat,
                    ViewDimension = RtvDimension.Buffer,

                    Buffer = new()
                    {
                        ElementWidth = (uint) (pixelFormat.SizeInBytes() * width),
                        ElementOffset = 0
                    }
                };

                HResult result = NativeDevice->CreateRenderTargetView(NativeResource, &description, &rtv);

                if (result.IsFailure)
                    result.Throw();
            }

            return rtv;
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();

            if (GraphicsDevice != null && GraphicsDevice.IsDebugMode)
            {
                if (NativeShaderResourceView != null)
                    DebugHelpers.SetDebugName((ID3D11DeviceChild*) NativeShaderResourceView, Name is null ? null : $"{Name} SRV");

                if (NativeUnorderedAccessView != null)
                    DebugHelpers.SetDebugName((ID3D11DeviceChild*) NativeUnorderedAccessView, Name is null ? null : $"{Name} UAV");
            }
        }

        private void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
        {
            if (Description.StructureByteStride == 0)
            {
                // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                count = ViewFlags.HasFlag(BufferFlags.RawBuffer) ? Description.SizeInBytes / sizeof(int) :
                        ViewFlags.HasFlag(BufferFlags.ShaderResource) ? Description.SizeInBytes / viewFormat.SizeInBytes() :
                        0;
            }
            else
            {
                // For structured buffer
                count = Description.SizeInBytes / Description.StructureByteStride;
                viewFormat = PixelFormat.None;
            }
        }

        private static BufferDesc ConvertToNativeDescription(BufferDescription bufferDescription)
        {
            var bufferDesc = new BufferDesc
            {
                ByteWidth = (uint) bufferDescription.SizeInBytes,
                StructureByteStride = (uint) bufferDescription.StructureByteStride,
                CPUAccessFlags = 0,
                BindFlags = 0,
                Usage = (Usage) bufferDescription.Usage
            };

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.ConstantBuffer))
            {
                bufferDesc.BindFlags |= (uint) BindFlag.ConstantBuffer;
                bufferDesc.StructureByteStride = (uint) bufferDescription.StructureByteStride + (16 - ((uint) bufferDescription.StructureByteStride % 16));
                bufferDesc.CPUAccessFlags = (uint) CpuAccessFlag.Write;
            }

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.IndexBuffer))
                bufferDesc.BindFlags |= (uint) BindFlag.IndexBuffer;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.VertexBuffer))
                bufferDesc.BindFlags |= (uint) BindFlag.VertexBuffer;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.RenderTarget))
                bufferDesc.BindFlags |= (uint) BindFlag.RenderTarget;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.ShaderResource))
                bufferDesc.BindFlags |= (uint) BindFlag.ShaderResource;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.UnorderedAccess))
                bufferDesc.BindFlags |= (uint) BindFlag.UnorderedAccess;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.StructuredBuffer))
            {
                bufferDesc.MiscFlags |= (uint) BufferFlags.StructuredBuffer;
                if (bufferDescription.StructureByteStride <= 0)
                    throw new ArgumentException("Element size cannot be less or equal than 0 for a structured buffer");
            }

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.RawBuffer))
                bufferDesc.MiscFlags |= (uint) MiscFlags.BUFFER_ALLOW_RAW_VIEWS;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.ArgumentBuffer))
                bufferDesc.MiscFlags |= (uint) MiscFlags.DRAWINDIRECT_ARGS;

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.StreamOutput))
                bufferDesc.BindFlags |= (uint) BindFlag.StreamOutput;

            if(bufferDesc.Usage == Silk.NET.Direct3D11.Usage.Dynamic)
            {
                bufferDesc.CPUAccessFlags = (uint) CpuAccessFlag.Write;
                if (bufferDesc.BindFlags == 0)
                    bufferDesc.BindFlags = (uint) BindFlag.ConstantBuffer;
            }

            return bufferDesc;
        }

        /// <summary>
        /// Initializes the views.
        /// </summary>
        private void InitializeViews()
        {
            var bindFlags = (BindFlag) nativeDescription.BindFlags;

            var srvFormat = ViewFormat;
            var uavFormat = ViewFormat;

            if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
            {
                srvFormat = PixelFormat.R32_Typeless;
                uavFormat = PixelFormat.R32_Typeless;
            }

            if (bindFlags.HasFlag(BindFlag.ShaderResource))
            {
                NativeShaderResourceView = GetShaderResourceView(srvFormat);
            }

            if (bindFlags.HasFlag(BindFlag.UnorderedAccess))
            {
                var description = new UnorderedAccessViewDesc()
                {
                    Format = (Format) uavFormat,
                    ViewDimension = UavDimension.Buffer,

                    Buffer = new()
                    {
                        NumElements = (uint) ElementCount,
                        FirstElement = 0,
                        Flags = 0
                    }
                };

                var bufferFlags = (BufferUavFlag) description.Buffer.Flags;

                if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
                    bufferFlags |= BufferUavFlag.Raw;

                if (ViewFlags.HasFlag(BufferFlags.StructuredAppendBuffer))
                    bufferFlags |= BufferUavFlag.Append;

                if (ViewFlags.HasFlag(BufferFlags.StructuredCounterBuffer))
                    bufferFlags |= BufferUavFlag.Counter;

                description.Buffer = description.Buffer with { Flags = (uint) bufferFlags };

                ID3D11UnorderedAccessView* unorderedAccessView = null;
                HResult result = NativeDevice->CreateUnorderedAccessView(NativeResource, &description, &unorderedAccessView);

                if (result.IsFailure)
                    result.Throw();
            }
        }
    }
}

#endif
