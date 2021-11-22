// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        private ComPtr<ID3D11Buffer> nativeBuffer;

        //buffer descrption
        private ComPtr<BufferDesc> nativeDescription;

        internal ComPtr<ID3D11Buffer> NativeBuffer 
        {
            get
            {
                return this.nativeBuffer;
            }
            set
            {
                this.nativeBuffer = value;
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
            unsafe
            {
                var desc = ConvertToNativeDescription(Description);
                nativeDescription.Handle = &desc;
            }
            ViewFlags = viewFlags;
            InitCountAndViewFormat(out this.elementCount, ref viewFormat);
            ViewFormat = viewFormat;
            unsafe
            {
                ID3D11Buffer* pBuff = null;
                SubresourceData srd = new SubresourceData
                {
                    PSysMem = (void*)dataPointer
                };
                SilkMarshal.ThrowHResult(NativeDevice.Get().CreateBuffer(nativeDescription.Handle, dataPointer == IntPtr.Zero ? null : &srd, &pBuff));
                NativeDeviceChild = new ComPtr<ID3D11DeviceChild>((ID3D11DeviceChild*)pBuff);
            }
            

            // Staging resource don't have any views
            if (nativeDescription.Get().Usage != Silk.NET.Direct3D11.Usage.UsageStaging)
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

            //NativeDeviceChild = new SharpDX.Direct3D11.Buffer(GraphicsDevice.NativeDevice, IntPtr.Zero, nativeDescription);
            
            unsafe
            {

                ID3D11Buffer* buff = null;
                SilkMarshal.ThrowHResult(NativeDevice.Get().CreateBuffer(nativeDescription.Handle, null, &buff));
                NativeDeviceChild = new ComPtr<ID3D11DeviceChild>((ID3D11DeviceChild*)buff);
            }
            

            // Staging resource don't have any views
            if (nativeDescription.Get().Usage != Silk.NET.Direct3D11.Usage.UsageStaging)
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
            unsafe
            {
                NativeDeviceChild = new ComPtr<ID3D11DeviceChild>((ID3D11DeviceChild*)GraphicsDevice.NativeDevice.Handle);
            }

            // Staging resource don't have any views
            if (nativeDescription.Get().Usage != Silk.NET.Direct3D11.Usage.UsageStaging)
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
        internal ComPtr<ID3D11ShaderResourceView> GetShaderResourceView(PixelFormat viewFormat)
        {
            ComPtr<ID3D11ShaderResourceView> srv = new();
            if ((nativeDescription.Get().BindFlags & (uint)BindFlag.BindShaderResource) != 0)
            {
                var description = new ShaderResourceViewDesc
                {
                    Format = (Format)viewFormat,
                    ViewDimension = Silk.NET.Core.Native.D3DSrvDimension.D3D11SrvDimensionBufferex,
                    BufferEx = new BufferexSrv
                    {
                        NumElements = (uint)ElementCount,
                        FirstElement = 0,
                        Flags = 0
                    },
                };

                if ((ViewFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer)
                {
                    var buff = description.BufferEx;
                    buff.Flags |= (uint)BufferexSrvFlag.BufferexSrvFlagRaw;
                }
                unsafe
                {
                    //TODO : Instantiate this correctly
                    ID3D11ShaderResourceView* ptr = null;
                    SilkMarshal.ThrowHResult(GraphicsDevice.NativeDevice.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11ShaderResourceView>(), (void**)&ptr));
                    srv = new(ptr);
                }
                
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
        internal ComPtr<ID3D11RenderTargetView> GetRenderTargetView(PixelFormat pixelFormat, int width)
        {
            var rtv = new ComPtr<ID3D11RenderTargetView>();
            if ((nativeDescription.Get().BindFlags & (uint)BindFlag.BindRenderTarget) != 0)
            {
                var description = new RenderTargetViewDesc
                {
                    Format = (Format)pixelFormat,
                    ViewDimension = RtvDimension.RtvDimensionBuffer,
                    Buffer = new BufferRtv
                    {
                        ElementWidth = (uint)(pixelFormat.SizeInBytes() * width),
                        ElementOffset = 0
                    }
                };
                unsafe
                {
                    ID3D11RenderTargetView* ptr = null;
                    SilkMarshal.ThrowHResult(GraphicsDevice.NativeDevice.Get().CreateRenderTargetView((ID3D11Resource*)nativeBuffer.Handle, &description, &ptr)); 
                    rtv = new(ptr);
                }
                
            }
            return rtv;
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
            if (GraphicsDevice != null && GraphicsDevice.IsDebugMode)
            {
                //TODO : Check if null?
                //if (NativeShaderResourceView != null)
                //    NativeShaderResourceView.DebugName = Name == null ? null : string.Format("{0} SRV", Name);

                //if (NativeUnorderedAccessView != null)
                //    NativeUnorderedAccessView.DebugName = Name == null ? null : string.Format("{0} UAV", Name);
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
            {
                desc.BindFlags |= (uint)BindFlag.BindConstantBuffer;
                desc.StructureByteStride = (uint)bufferDescription.StructureByteStride + (16 - ((uint)bufferDescription.StructureByteStride % 16));
                desc.CPUAccessFlags = (uint)CpuAccessFlag.CpuAccessWrite;
            }

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
                desc.MiscFlags |= (uint)BufferFlags.StructuredBuffer;
                if (bufferDescription.StructureByteStride <= 0)
                    throw new ArgumentException("Element size cannot be less or equal 0 for structured buffer");
            }

            if ((bufferFlags & (uint)BufferFlags.RawBuffer) == (uint)BufferFlags.RawBuffer)
                desc.MiscFlags |= (uint)MiscFlags.BUFFER_ALLOW_RAW_VIEWS;

            if ((bufferFlags & (uint)BufferFlags.ArgumentBuffer) == (uint)BufferFlags.ArgumentBuffer)
                desc.MiscFlags |= (uint)MiscFlags.DRAWINDIRECT_ARGS;

            if ((bufferFlags & (uint)BufferFlags.StreamOutput) != 0)
                desc.BindFlags |= (uint)BindFlag.BindStreamOutput;

            if(desc.Usage == Silk.NET.Direct3D11.Usage.UsageDynamic)
            {
                desc.CPUAccessFlags = (uint)CpuAccessFlag.CpuAccessWrite;
                if (desc.BindFlags == 0) desc.BindFlags = (uint)BindFlag.BindConstantBuffer;
            }

            return desc;
        }

        /// <summary>
        /// Initializes the views.
        /// </summary>
        private void InitializeViews()
        {
            var bindFlags = nativeDescription.Get().BindFlags;

            var srvFormat = ViewFormat;
            var uavFormat = ViewFormat;

            if (((ViewFlags & BufferFlags.RawBuffer) != 0))
            {
                srvFormat = PixelFormat.R32_Typeless;
                uavFormat = PixelFormat.R32_Typeless;
            }

            if ((bindFlags & (uint)BindFlag.BindShaderResource) != 0)
            {
                unsafe
                {
                    NativeShaderResourceView = GetShaderResourceView(srvFormat);
                }
            }

            if ((bindFlags & (uint)BindFlag.BindUnorderedAccess) != 0)
            {
                var description = new UnorderedAccessViewDesc()
                {
                    Format = (Format)uavFormat,
                    ViewDimension = UavDimension.UavDimensionBuffer,
                    Buffer = new BufferUav
                    {
                        NumElements = (uint)ElementCount,
                        FirstElement = 0,
                        Flags = 0,
                    },
                };

                var buff = description.Buffer;
                if ((ViewFlags & BufferFlags.RawBuffer) == BufferFlags.RawBuffer)
                    buff.Flags |= (uint)BufferUavFlag.BufferUavFlagRaw;

                if ((ViewFlags & BufferFlags.StructuredAppendBuffer) == BufferFlags.StructuredAppendBuffer)
                    buff.Flags |= (uint)BufferUavFlag.BufferUavFlagAppend;

                if ((ViewFlags & BufferFlags.StructuredCounterBuffer) == BufferFlags.StructuredCounterBuffer)
                    buff.Flags |= (uint)BufferUavFlag.BufferUavFlagCounter;


                unsafe
                {
                    ID3D11UnorderedAccessView* pUav = null;
                    SilkMarshal.ThrowHResult(GraphicsDevice.NativeDevice.Get().CreateUnorderedAccessView((ID3D11Resource*)nativeBuffer.Handle, &description, &pUav));
                    NativeUnorderedAccessView = new ComPtr<ID3D11UnorderedAccessView>(pUav);
                }
            }
        }
    }
} 
#endif 
