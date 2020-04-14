// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
using SharpDX.Direct3D12;
using Stride.Shaders;

namespace Stride.Graphics
{
    public partial class DescriptorPool
    {
        internal DescriptorHeap SrvHeap;
        internal DescriptorHeap SamplerHeap;

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
                SrvHeap = graphicsDevice.NativeDevice.CreateDescriptorHeap(new DescriptorHeapDescription
                {
                    DescriptorCount = SrvCount,
                    Flags = DescriptorHeapFlags.None,
                    Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                });
                SrvStart = SrvHeap.CPUDescriptorHandleForHeapStart;
            }

            if (SamplerCount > 0)
            {
                SamplerHeap = graphicsDevice.NativeDevice.CreateDescriptorHeap(new DescriptorHeapDescription
                {
                    DescriptorCount = SamplerCount,
                    Flags = DescriptorHeapFlags.None,
                    Type = DescriptorHeapType.Sampler,
                });
                SamplerStart = SamplerHeap.CPUDescriptorHandleForHeapStart;
            }
        }

        protected internal override void OnDestroyed()
        {
            ReleaseComObject(ref SrvHeap);
            ReleaseComObject(ref SamplerHeap);

            base.OnDestroyed();
        }
    }
}
#endif
