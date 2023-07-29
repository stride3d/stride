// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Stride.Shaders;

namespace Stride.Graphics
{
    public unsafe partial class DescriptorPool
    {
        internal ID3D12DescriptorHeap* SrvHeap;
        internal ID3D12DescriptorHeap* SamplerHeap;

        internal CpuDescriptorHandle SrvStart;
        internal int SrvOffset;
        internal int SrvCount;

        internal CpuDescriptorHandle SamplerStart;
        internal int SamplerOffset;
        internal int SamplerCount;

        public void Reset()
        {
            SrvOffset = 0;
            SamplerOffset = 0;
        }

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

                ID3D12DescriptorHeap* descriptorHeap;
                HResult result = graphicsDevice.NativeDevice->CreateDescriptorHeap(descriptorHeapDesc,
                                                                                   SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(),
                                                                                   (void**) &descriptorHeap);
                if (result.IsFailure)
                    result.Throw();

                SrvHeap = descriptorHeap;
                SrvStart = SrvHeap->GetCPUDescriptorHandleForHeapStart();
            }

            if (SamplerCount > 0)
            {
                var descriptorHeapDesc = new DescriptorHeapDesc
                {
                    NumDescriptors = (uint) SamplerCount,
                    Flags = DescriptorHeapFlags.None,
                    Type = DescriptorHeapType.Sampler
                };

                ID3D12DescriptorHeap* descriptorHeap;
                HResult result = graphicsDevice.NativeDevice->CreateDescriptorHeap(descriptorHeapDesc,
                                                                                   SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(),
                                                                                   (void**) &descriptorHeap);
                if (result.IsFailure)
                    result.Throw();

                SamplerHeap = descriptorHeap;
                SamplerStart = SamplerHeap->GetCPUDescriptorHandleForHeapStart();
            }
        }

        protected internal override void OnDestroyed()
        {
            if (SrvHeap != null)
                SrvHeap->Release();

            SrvHeap = null;

            if (SamplerHeap != null)
                SamplerHeap->Release();

            SamplerHeap = null;

            base.OnDestroyed();
        }
    }
}

#endif
