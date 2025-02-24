// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System.Runtime.CompilerServices;
using Silk.NET.Direct3D12;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    public unsafe partial class SamplerState
    {
        internal CpuDescriptorHandle NativeSampler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerState"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="samplerStateDescription">The sampler state description.</param>
        private SamplerState(GraphicsDevice device, SamplerStateDescription samplerStateDescription) : base(device)
        {
            Description = samplerStateDescription;

            CreateNativeDeviceChild();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            CreateNativeDeviceChild();
            return true;
        }

        private void CreateNativeDeviceChild()
        {
            var nativeDescription = new SamplerDesc
            {
                AddressU = (Silk.NET.Direct3D12.TextureAddressMode) Description.AddressU,
                AddressV = (Silk.NET.Direct3D12.TextureAddressMode) Description.AddressV,
                AddressW = (Silk.NET.Direct3D12.TextureAddressMode) Description.AddressW,
                ComparisonFunc = (ComparisonFunc) Description.CompareFunction,
                Filter = (Filter) Description.Filter,
                MaxAnisotropy = (uint) Description.MaxAnisotropy,
                MaxLOD = Description.MaxMipLevel,
                MinLOD = Description.MinMipLevel,
                MipLODBias = Description.MipMapLevelOfDetailBias
            };
            Unsafe.AsRef<Color4>(nativeDescription.BorderColor) = Description.BorderColor;

            NativeSampler = GraphicsDevice.SamplerAllocator.Allocate(1);
            NativeDevice->CreateSampler(nativeDescription, NativeSampler);
        }
    }
}

#endif
