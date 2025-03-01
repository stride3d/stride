// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    public unsafe partial class SamplerState
    {
        /// <summary>
        ///   Gets the native Direct3D 11 sampler state object.
        /// </summary>
        internal ID3D11SamplerState* NativeSamplerState { get; private set; }


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

        private unsafe void CreateNativeDeviceChild()
        {
            var nativeDescription = new SamplerDesc
            {
                AddressU = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressU,
                AddressV = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressV,
                AddressW = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressW,
                ComparisonFunc = (ComparisonFunc)Description.CompareFunction,
                Filter = (Filter) Description.Filter,
                MaxAnisotropy = (uint) Description.MaxAnisotropy,
                MaxLOD = (uint) Description.MaxMipLevel,
                MinLOD = (uint) Description.MinMipLevel,
                MipLODBias = Description.MipMapLevelOfDetailBias
            };
            Unsafe.AsRef<Color4>(nativeDescription.BorderColor) = Description.BorderColor;

            // For 9.1, anisotropy cannot be larger than 2.
            // Mirror once is not supported either.
            if (GraphicsDevice.Features.CurrentProfile == GraphicsProfile.Level_9_1)
            {
                // TODO: Min with user-value instead?
                nativeDescription.MaxAnisotropy = 2;

                if (nativeDescription.AddressU == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                    nativeDescription.AddressU = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
                if (nativeDescription.AddressV == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                    nativeDescription.AddressV = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
                if (nativeDescription.AddressW == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                    nativeDescription.AddressW = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
            }

            ID3D11SamplerState* samplerState;
            HResult result = NativeDevice->CreateSamplerState(in nativeDescription, &samplerState);

            if (result.IsFailure)
                result.Throw();

            NativeSamplerState = samplerState;
            NativeDeviceChild = (ID3D11DeviceChild*) samplerState;
        }
    }
}

#endif
