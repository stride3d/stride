// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Core.Mathematics;

namespace Stride.Graphics;

public unsafe partial class SamplerState
{
    private ID3D11SamplerState* samplerState;

    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    internal ComPtr<ID3D11SamplerState> NativeSamplerState => ComPtrHelpers.ToComPtr(samplerState);


    private SamplerState(GraphicsDevice device, SamplerStateDescription description) : base(device)
    {
        /// <summary>
        ///   Gets the native Direct3D 11 sampler state object.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerState"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="name">The name.</param>
        /// <param name="samplerStateDescription">The sampler state description.</param>
        Description = description;

        CreateNativeSamplerState();
    }

        /// <inheritdoc/>
    protected internal override bool OnRecreate()
    {
        base.OnRecreate();

        CreateNativeSamplerState();
        return true;
    }

    private unsafe void CreateNativeSamplerState()
    {
        var samplerDescription = new SamplerDesc
        {
            AddressU = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressU,
            AddressV = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressV,
            AddressW = (Silk.NET.Direct3D11.TextureAddressMode) Description.AddressW,
            ComparisonFunc = (ComparisonFunc) Description.CompareFunction,
            Filter = (Filter) Description.Filter,
            MaxAnisotropy = (uint) Description.MaxAnisotropy,
            MaxLOD = (uint) Description.MaxMipLevel,
            MinLOD = (uint) Description.MinMipLevel,
            MipLODBias = Description.MipMapLevelOfDetailBias
        };
        Debug.Assert(sizeof(Color4) == (4 * sizeof(float)));
        Unsafe.AsRef<Color4>(samplerDescription.BorderColor) = Description.BorderColor;

        // For 9.1, anisotropy cannot be larger than 2.
        // Mirror once is not supported either.
        if (GraphicsDevice.Features.CurrentProfile == GraphicsProfile.Level_9_1)
        {
            // TODO: Min with user-value instead?
            samplerDescription.MaxAnisotropy = 2;

            if (samplerDescription.AddressU == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                samplerDescription.AddressU = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
            if (samplerDescription.AddressV == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                samplerDescription.AddressV = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
            if (samplerDescription.AddressW == Silk.NET.Direct3D11.TextureAddressMode.MirrorOnce)
                samplerDescription.AddressW = Silk.NET.Direct3D11.TextureAddressMode.Mirror;
        }

        ID3D11SamplerState* samplerState;
        HResult result = NativeDevice.CreateSamplerState(in samplerDescription, &samplerState);

        if (result.IsFailure)
            result.Throw();

        this.samplerState = samplerState;
        NativeDeviceChild = NativeSamplerState.AsDeviceChild();
    }
}

#endif
