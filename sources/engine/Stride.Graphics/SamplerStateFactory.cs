// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   A factory for creating <see cref="SamplerState"/> instances.
///   Contains pre-created Sampler States for commonly used configurations.
/// </summary>
/// <remarks>
///   To access these default Sampler States, you can access them through <see cref="GraphicsDevice.SamplerStates"/>.
/// </remarks>
public class SamplerStateFactory : GraphicsResourceFactoryBase
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="SamplerStateFactory"/> class.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    internal SamplerStateFactory(GraphicsDevice device) : base(device)
    {
        PointWrap = CreateSamplerState("SamplerState.PointWrap", TextureFilter.Point, TextureAddressMode.Wrap);
        PointClamp = CreateSamplerState("SamplerState.PointClamp", TextureFilter.Point, TextureAddressMode.Clamp);
        LinearWrap = CreateSamplerState("SamplerState.LinearWrap", TextureFilter.Linear, TextureAddressMode.Wrap);
        LinearClamp = CreateSamplerState("SamplerState.LinearClamp", TextureFilter.Linear, TextureAddressMode.Clamp);
        AnisotropicWrap = CreateSamplerState("SamplerState.AnisotropicWrap", TextureFilter.Anisotropic, TextureAddressMode.Wrap);
        AnisotropicClamp = CreateSamplerState("SamplerState.AnisotropicClamp", TextureFilter.Anisotropic, TextureAddressMode.Clamp);


        SamplerState CreateSamplerState(string name, TextureFilter filter, TextureAddressMode addressMode)
        {
            var description = new SamplerStateDescription(filter, addressMode);
            var samplerState = SamplerState.New(device, in description, name).DisposeBy(this);
            return samplerState;
        }
    }


    /// <summary>
    ///   Default Sampler State for <strong>point filtering</strong> with texture coordinate <strong>wrapping</strong>.
    /// </summary>
    public readonly SamplerState PointWrap;

    /// <summary>
    ///   Default Sampler State for <strong>point filtering</strong> with texture coordinate <strong>clamping</strong>.
    /// </summary>
    public readonly SamplerState PointClamp;

    /// <summary>
    ///   Default Sampler State for <strong>linear filtering</strong> with texture coordinate <strong>wrapping</strong>.
    /// </summary>
    public readonly SamplerState LinearWrap;

    /// <summary>
    ///   Default Sampler State for <strong>linear filtering</strong> with texture coordinate <strong>clamping</strong>.
    /// </summary>
    public readonly SamplerState LinearClamp;

    /// <summary>
    ///   Default Sampler State for <strong>anisotropic filtering</strong> with texture coordinate <strong>wrapping</strong>.
    /// </summary>
    public readonly SamplerState AnisotropicWrap;

    /// <summary>
    ///   Default Sampler State for <strong>anisotropic filtering</strong> with texture coordinate <strong>clamping</strong>.
    /// </summary>
    public readonly SamplerState AnisotropicClamp;
}
