// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using Silk.NET.Direct3D11;

//using SharpDX;
//using SharpDX.Direct3D11;
//using SharpDX.DXGI;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        //buffer descrption
        private Silk.NET.Direct3D11.BufferDesc nativeDescription;

        internal Silk.NET.Direct3D11.ID3D11Buffer NativeBuffer { get; set; }

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
            InitCountAndViewFormat(out this.elementCount, ref viewFormat);
            ViewFormat = viewFormat;
            NativeDeviceChild = new SharpDX.Direct3D11.Buffer(GraphicsDevice.NativeDevice, dataPointer, nativeDescription);

            // Staging resource don't have any views
            if (nativeDescription.Usage != ResourceUsage.Staging)
                this.InitializeViews();

            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(SizeInBytes);
            }

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);
            }

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage == GraphicsResourceUsage.Immutable
                || Description.Usage == GraphicsResourceUsage.Default)
                return false;

            NativeDeviceChild = new SharpDX.Direct3D11.Buffer(GraphicsDevice.NativeDevice, IntPtr.Zero, nativeDescription);

            // Staging resource don't have any views
            if (nativeDescription.Usage != ResourceUsage.Staging)
                this.InitializeViews();

            return true;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public void Recreate(IntPtr dataPointer)
        {
            NativeDeviceChild = new SharpDX.Direct3D11.Buffer(GraphicsDevice.NativeDevice, dataPointer, nativeDescription);

            // Staging resource don't have any views
            if (nativeDescription.Usage != ResourceUsage.Staging)
                this.InitializeViews();
        }

        /// <summary>
        /// Gets a <see cref="ShaderResourceView"/> for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="viewFormat">The view format.</param>
        /// <returns>A <see cref="ShaderResourceView"/> for the particular view format.</returns>
        /// <remarks>
        /// The buffer must have been declared with <see cref="Graphics.BufferFlags.ShaderResource"/>. 
        /// The ShaderResourceView instance is kept by this buffer and will be disposed when this buffer is disposed.
        /// </remarks>
        internal ShaderResourceView GetShaderResourceView(PixelFormat viewFormat)
        {
            ShaderResourceView srv = null;
            if ((nativeDescription.BindFlags & BindFlags.ShaderResource) != 0)
            {
                var description = new ShaderResourceViewDescription
                {
                    Format = (SharpDX.DXGI.Format)viewFormat,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.ExtendedBuffer,
                    BufferEx =
                    {
                        ElementCount = this.ElementCount,
                        FirstElement = 0,
                        Flags = ShaderResourceViewExtendedBufferFlags.None,
                    },
                };

                if (((ViewFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer))
                    description.BufferEx.Flags |= ShaderResourceViewExtendedBufferFlags.Raw;

                srv = new ShaderResourceView(this.GraphicsDevice.NativeDevice, NativeResource, description);
            }
            return srv;
        }

        /// <summary>
        /// Gets a <see cref="RenderTargetView" /> for a particular <see cref="PixelFormat" />.
        /// </summary>
        /// <param name="pixelFormat">The view format.</param>
        /// <param name="width">The width in pixels of the render target.</param>
        /// <returns>A <see cref="RenderTargetView" /> for the particular view format.</returns>
        /// <remarks>The buffer must have been declared with <see cref="Graphics.BufferFlags.RenderTarget" />.
        /// The RenderTargetView instance is kept by this buffer and will be disposed when this buffer is disposed.</remarks>
        internal RenderTargetView GetRenderTargetView(PixelFormat pixelFormat, int width)
        {
            RenderTargetView srv = null;
            if ((nativeDescription.BindFlags & BindFlags.RenderTarget) != 0)
            {
                var description = new RenderTargetViewDescription()
                {
                    Format = (SharpDX.DXGI.Format)pixelFormat,
                    Dimension = RenderTargetViewDimension.Buffer,
                    Buffer =
                    {
                        ElementWidth = pixelFormat.SizeInBytes() * width,
                        ElementOffset = 0,
                    },
                };

                srv = new RenderTargetView(this.GraphicsDevice.NativeDevice, NativeBuffer, description);
            }
            return srv;
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
            if (GraphicsDevice != null && GraphicsDevice.IsDebugMode)
            {
                if (NativeShaderResourceView != null)
                    NativeShaderResourceView.DebugName = Name == null ? null : string.Format("{0} SRV", Name);

                if (NativeUnorderedAccessView != null)
                    NativeUnorderedAccessView.DebugName = Name == null ? null : string.Format("{0} UAV", Name);
            }
        }

        private void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
        {
            if (Description.StructureByteStride == 0)
            {
                // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                if ((ViewFlags & BufferFlags.RawBuffer) != 0)
                {
                    count = Description.SizeInBytes / sizeof(int);
                }
                else if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    count = Description.SizeInBytes / viewFormat.SizeInBytes();
                }
                else
                {
                    count = 0;
                }
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
            var desc = new BufferDesc
            {
                ByteWidth = (uint)bufferDescription.SizeInBytes,
                StructureByteStride = (uint)bufferDescription.StructureByteStride,
                CPUAccessFlags = 0,
                BindFlags = 0,
                Usage = (Usage)bufferDescription.Usage
            };

            var bufferFlags = (uint)bufferDescription.BufferFlags;

            if ((bufferFlags & (uint)BufferFlags.ConstantBuffer) != 0)
                desc.BindFlags |= (uint)BindFlag.BindConstantBuffer;

            if ((bufferFlags & (uint)BufferFlags.IndexBuffer) != 0)
                desc.BindFlags |= (uint)BindFlag.BindIndexBuffer;

            if ((bufferFlags & (uint)BufferFlags.VertexBuffer) != 0)
                desc.BindFlags |= (uint)BindFlag.BindVertexBuffer;

            if ((bufferFlags & (uint)BufferFlags.RenderTarget) != 0)
                desc.BindFlags |= (uint)BindFlag.BindRenderTarget;

            if ((bufferFlags & (uint)BufferFlags.ShaderResource) != 0)
                desc.BindFlags |= (uint)BindFlag.BindShaderResource;

            if ((bufferFlags & (uint)BufferFlags.UnorderedAccess) != 0)
                desc.BindFlags |= (uint)BindFlag.BindUnorderedAccess;

            if ((bufferFlags & (uint)BufferFlags.StructuredBuffer) != 0)
            {
                desc.MiscFlags |= (uint) .BufferStructured;
                if (bufferDescription.StructureByteStride <= 0)
                    throw new ArgumentException("Element size cannot be less or equal 0 for structured buffer");
            }

            if ((bufferFlags & (uint)BufferFlags.RawBuffer) == (uint)BufferFlags.RawBuffer)
                desc.MiscFlags |= (uint)MiscFlags.BUFFER_ALLOW_RAW_VIEWS;

            if ((bufferFlags & (uint)BufferFlags.ArgumentBuffer) == (uint)BufferFlags.ArgumentBuffer)
                desc.MiscFlags |= (uint)MiscFlags.DRAWINDIRECT_ARGS;

            if ((bufferFlags & (uint)BufferFlags.StreamOutput) != 0)
                desc.BindFlags |= (uint)BindFlag.BindStreamOutput;

            return desc;
        }

        /// <summary>
        /// Initializes the views.
        /// </summary>
        private void InitializeViews()
        {
            var bindFlags = nativeDescription.BindFlags;

            var srvFormat = ViewFormat;
            var uavFormat = ViewFormat;

            if (((ViewFlags & BufferFlags.RawBuffer) != 0))
            {
                srvFormat = PixelFormat.R32_Typeless;
                uavFormat = PixelFormat.R32_Typeless;
            }

            if ((bindFlags & BindFlags.ShaderResource) != 0)
            {
                this.NativeShaderResourceView = GetShaderResourceView(srvFormat);
            }

            if ((bindFlags & BindFlags.UnorderedAccess) != 0)
            {
                var description = new UnorderedAccessViewDescription()
                {
                    Format = (SharpDX.DXGI.Format)uavFormat,
                    Dimension = UnorderedAccessViewDimension.Buffer,
                    Buffer =
                    {
                        ElementCount = this.ElementCount,
                        FirstElement = 0,
                        Flags = UnorderedAccessViewBufferFlags.None,
                    },
                };

                if (((ViewFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer))
                    description.Buffer.Flags |= UnorderedAccessViewBufferFlags.Raw;

                if (((ViewFlags & BufferFlags.StructuredAppendBuffer) == BufferFlags.StructuredAppendBuffer))
                    description.Buffer.Flags |= UnorderedAccessViewBufferFlags.Append;

                if (((ViewFlags & BufferFlags.StructuredCounterBuffer) == BufferFlags.StructuredCounterBuffer))
                    description.Buffer.Flags |= UnorderedAccessViewBufferFlags.Counter;

                this.NativeUnorderedAccessView = new UnorderedAccessView(this.GraphicsDevice.NativeDevice, NativeBuffer, description);
            }
        }
    }
} 
#endif 
