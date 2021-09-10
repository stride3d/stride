// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using Silk.NET.Direct3D11;
using Silk.NET.Maths;
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
            
            unsafe
            {
                var nativeDescription = new SamplerDesc
                {
                    AddressU = (Silk.NET.Direct3D11.TextureAddressMode)Description.AddressU,
                    AddressV = (Silk.NET.Direct3D11.TextureAddressMode)Description.AddressV,
                    AddressW = (Silk.NET.Direct3D11.TextureAddressMode)Description.AddressW,
                    ComparisonFunc = (ComparisonFunc)Description.CompareFunction,
                    Filter = (Filter)Description.Filter,
                    MaxAnisotropy = (uint)Description.MaxAnisotropy,
                    MaxLOD = (uint)Description.MaxMipLevel,
                    MinLOD = (uint)Description.MinMipLevel,
                    MipLODBias = Description.MipMapLevelOfDetailBias
                };
                nativeDescription.BorderColor[0] = Description.BorderColor[0];
                nativeDescription.BorderColor[1] = Description.BorderColor[1];
                nativeDescription.BorderColor[2] = Description.BorderColor[2];
                nativeDescription.BorderColor[3] = Description.BorderColor[3];
                //call(nativeDescription)




                // For 9.1, anisotropy cannot be larger then 2
                // mirror once is not supported either
                if (GraphicsDevice.Features.CurrentProfile == GraphicsProfile.Level_9_1)
                {
                    // TODO: Min with user-value instead?
                    nativeDescription.MaxAnisotropy = 2;

                    if (nativeDescription.AddressU == Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirrorOnce)
                        nativeDescription.AddressU = Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirror;
                    if (nativeDescription.AddressV == Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirrorOnce)
                        nativeDescription.AddressV = Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirror;
                    if (nativeDescription.AddressW == Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirrorOnce)
                        nativeDescription.AddressW = Silk.NET.Direct3D11.TextureAddressMode.TextureAddressMirror;
                }
            
                ID3D11SamplerState* v = null;
                NativeDevice.CreateSamplerState(&nativeDescription, &v);
                NativeDeviceChild = *v;
            }

            //NativeDeviceChild = new SamplerState(NativeDevice, nativeDescription);
        }
    }
} 
#endif
