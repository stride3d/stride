// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using Stride.Shaders;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class DescriptorPool
    {
        private ID3D12DescriptorHeap* nativeSrvHeap;
        private ID3D12DescriptorHeap* nativeSamplerHeap;

        /// <summary>
        ///   Gets the internal Direct3D 12 Descriptor Heap for Shader Resource Views.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        protected internal ComPtr<ID3D12DescriptorHeap> SrvHeap => ToComPtr(nativeSrvHeap);

        /// <summary>
        ///   Gets the internal Direct3D 12 Descriptor Heap for Samplers.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        protected internal ComPtr<ID3D12DescriptorHeap> SamplerHeap => ToComPtr(nativeSamplerHeap);

        internal CpuDescriptorHandle SrvStart;  // CPU handle to the start of the Shader Resource View heap
        internal int SrvOffset;                 // Offset in the SRV heap from SrvStart
        internal int SrvCount;                  // Number of SRVs allocated in the pool

        internal CpuDescriptorHandle SamplerStart;  // CPU handle to the start of the Sampler heap
        internal int SamplerOffset;                 // Offset in the Sampler heap from SamplerStart
        internal int SamplerCount;                  // Number of Samplers allocated in the pool


        private DescriptorPool(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts) : base(graphicsDevice)
        {
            // For now, we put everything together so let's compute total count
            foreach (var count in counts)
            {
                if (count.Type == EffectParameterClass.Sampler)
                    SamplerCount += count.Count;
                else
                    SrvCount += count.Count;
            }

            if (SrvCount > 0)
            {
                var descriptorHeapDesc = new DescriptorHeapDesc
                {
                    NumDescriptors = (uint) SrvCount,
                    Flags = DescriptorHeapFlags.None,
                    Type = DescriptorHeapType.CbvSrvUav
                };

                HResult result = graphicsDevice.NativeDevice.CreateDescriptorHeap(in descriptorHeapDesc,
                                                                                  out ComPtr<ID3D12DescriptorHeap> descriptorHeap);
                if (result.IsFailure)
                    result.Throw();

                nativeSrvHeap = descriptorHeap;
                SrvStart = SrvHeap.GetCPUDescriptorHandleForHeapStart();
            }

            if (SamplerCount > 0)
            {
                var descriptorHeapDesc = new DescriptorHeapDesc
                {
                    NumDescriptors = (uint) SamplerCount,
                    Flags = DescriptorHeapFlags.None,
                    Type = DescriptorHeapType.Sampler
                };

                HResult result = graphicsDevice.NativeDevice.CreateDescriptorHeap(in descriptorHeapDesc,
                                                                                  out ComPtr<ID3D12DescriptorHeap> descriptorHeap);
                if (result.IsFailure)
                    result.Throw();

                nativeSamplerHeap = descriptorHeap;
                SamplerStart = SamplerHeap.GetCPUDescriptorHandleForHeapStart();
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            SafeRelease(ref nativeSrvHeap);
            SafeRelease(ref nativeSamplerHeap);

            base.OnDestroyed(immediately);
        }

        /// <summary>
        ///   Clears the Descriptor Pool, resetting all allocated Descriptors.
        /// </summary>
        public void Reset()
        {
            SrvOffset = 0;
            SamplerOffset = 0;
        }
    }
}

#endif
