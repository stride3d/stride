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
    ///   Gets the internal Direct3D 11 Sampler State object.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    internal ComPtr<ID3D11SamplerState> NativeSamplerState => ComPtrHelpers.ToComPtr(samplerState);


    /// <summary>
    ///   Initializes a new instance of the <see cref="SamplerState"/> class.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    /// <param name="description">
    ///   A <see cref="SamplerStateDescription"/> structure describing the Sampler State
    ///   object to create.
    /// </param>
    /// <param name="name">An optional name that can be used to identify the Sampler State.</param>
    private SamplerState(GraphicsDevice device, ref readonly SamplerStateDescription description, string? name = null)
        : base(device, name)
    {
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

    /// <inheritdoc/>
    protected internal override void OnDestroyed(bool immediately = false)
    {
        // As we set the Sampler State as the internal ID3D11DeviceChild,
        // it would be released when the GraphicsDevice disposes the GraphicsResourceBase.
        // We unset it here to avoid double-release.
        UnsetNativeDeviceChild();
        ComPtrHelpers.SafeRelease(ref samplerState);

        base.OnDestroyed(immediately);
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
            MaxLOD = Description.MaxMipLevel,
            MinLOD = Description.MinMipLevel,
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

        ComPtr<ID3D11SamplerState> newSamplerState = default;
        HResult result = NativeDevice.CreateSamplerState(in samplerDescription, ref newSamplerState);

        if (result.IsFailure)
            result.Throw();

        // As we are setting the Sampler State as the internal ID3D11DeviceChild,
        // it would be released when the GraphicsDevice disposes the GraphicsResourceBase
        samplerState = newSamplerState;
        SetNativeDeviceChild(newSamplerState.AsDeviceChild());
    }
}

#endif
