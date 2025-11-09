// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using SharpDX;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    public partial class SamplerState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerState"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="name">The name.</param>
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
            SharpDX.Direct3D11.SamplerStateDescription nativeDescription;

            nativeDescription.AddressU = (SharpDX.Direct3D11.TextureAddressMode)Description.AddressU;
            nativeDescription.AddressV = (SharpDX.Direct3D11.TextureAddressMode)Description.AddressV;
            nativeDescription.AddressW = (SharpDX.Direct3D11.TextureAddressMode)Description.AddressW;
            nativeDescription.BorderColor = ColorHelper.Convert(Description.BorderColor);
            nativeDescription.ComparisonFunction = (SharpDX.Direct3D11.Comparison)Description.CompareFunction;
            nativeDescription.Filter = (SharpDX.Direct3D11.Filter)Description.Filter;
            nativeDescription.MaximumAnisotropy = Description.MaxAnisotropy;
            nativeDescription.MaximumLod = Description.MaxMipLevel;
            nativeDescription.MinimumLod = Description.MinMipLevel;
            nativeDescription.MipLodBias = Description.MipMapLevelOfDetailBias;

            NativeDeviceChild = new SharpDX.Direct3D11.SamplerState(NativeDevice, nativeDescription);
        }
    }
} 
#endif
