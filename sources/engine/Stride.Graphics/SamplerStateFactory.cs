// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

public class SamplerStateFactory : GraphicsResourceFactoryBase
{
    /// <summary>
    /// Base factory for <see cref="SamplerState"/>.
    /// </summary>
    internal SamplerStateFactory(GraphicsDevice device) : base(device)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerStateFactory"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        PointWrap = CreateSamplerState("SamplerState.PointWrap", TextureFilter.Point, TextureAddressMode.Wrap);
        PointClamp = CreateSamplerState("SamplerState.PointClamp", TextureFilter.Point, TextureAddressMode.Clamp);
        LinearWrap = CreateSamplerState("SamplerState.LinearWrap", TextureFilter.Linear, TextureAddressMode.Wrap);
        LinearClamp = CreateSamplerState("SamplerState.LinearClamp", TextureFilter.Linear, TextureAddressMode.Clamp);
        AnisotropicWrap = CreateSamplerState("SamplerState.AnisotropicWrap", TextureFilter.Anisotropic, TextureAddressMode.Wrap);
        AnisotropicClamp = CreateSamplerState("SamplerState.AnisotropicClamp", TextureFilter.Anisotropic, TextureAddressMode.Clamp);


        SamplerState CreateSamplerState(string name, TextureFilter filter, TextureAddressMode addressMode)
        {
            var description = new SamplerStateDescription(filter, addressMode);
            var samplerState = SamplerState.New(device, description).DisposeBy(this);
            AnisotropicClamp.Name = name;
            return samplerState;
        }
    }

        /// <summary>
        /// Default state for point filtering with texture coordinate wrapping.
        /// </summary>

        /// <summary>
        /// Default state for point filtering with texture coordinate clamping.
        /// </summary>
    public readonly SamplerState PointWrap;

        /// <summary>
        /// Default state for linear filtering with texture coordinate wrapping.
        /// </summary>
    public readonly SamplerState PointClamp;

    public readonly SamplerState LinearWrap;

        /// <summary>
        /// Default state for linear filtering with texture coordinate clamping.
        /// </summary>
    public readonly SamplerState LinearClamp;

        /// <summary>
        /// Default state for anisotropic filtering with texture coordinate wrapping.
        /// </summary>
    public readonly SamplerState AnisotropicWrap;

        /// <summary>
        /// Default state for anisotropic filtering with texture coordinate clamping.
        /// </summary>
    }
    public readonly SamplerState AnisotropicClamp;
}
