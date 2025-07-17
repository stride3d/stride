// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Silk.NET.Direct3D12;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public unsafe partial class SamplerState
    {
        /// <summary>
        ///   Gets the internal Direct3D 12 CPU-accessible handle to the Sampler State object.
        /// </summary>
        internal CpuDescriptorHandle NativeSampler;


        /// <summary>
        ///   Initializes a new instance of the <see cref="SamplerState"/> class.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        /// <param name="description">
        ///   A <see cref="SamplerStateDescription"/> structure describing the Sampler State
        ///   object to create.
        /// </param>
        /// <param name="name">An optional name that can be used to identify the Sampler State.</param>
        private SamplerState(GraphicsDevice device, ref readonly SamplerStateDescription samplerStateDescription, string? name = null)
            : base(device, name)
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
            Debug.Assert(sizeof(Color4) == (4 * sizeof(float)));
            Unsafe.AsRef<Color4>(nativeDescription.BorderColor) = Description.BorderColor;

            NativeSampler = GraphicsDevice.SamplerAllocator.Allocate();
            NativeDevice.CreateSampler(in nativeDescription, NativeSampler);
        }
    }
}

#endif
