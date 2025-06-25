// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public static class BlendStates
{
    /// <summary>
    /// Known values for <see cref="BlendStateDescription"/>.
    /// </summary>
    static BlendStates()
    {
        /// <summary>
        /// A built-in state object with settings for default blend, that is no blend at all.
        /// </summary>
        /// <summary>
        /// A built-in state object with settings for additive blend, that is adding the destination data to the source data without using alpha.
        /// </summary>
        /// <summary>
        /// A built-in state object with settings for alpha blend, that is blending the source and destination data using alpha.
        /// </summary>
        /// <summary>
        /// A built-in state object with settings for blending with non-premultipled alpha, that is blending source and destination data using alpha while assuming the color data contains no alpha information.
        /// </summary>
        /// <summary>
        /// A built-in state object with settings for opaque blend, that is overwriting the source with the destination data.
        /// </summary>
        /// <summary>
        /// A built-in state object with settings for no color rendering on target 0, that is only render to depth stencil buffer.
        /// </summary>
        var blendDescription = new BlendStateDescription(Blend.One, Blend.Zero);
        blendDescription.SetDefaults();
        Default = blendDescription;

        var colorDisabledDescription = new BlendStateDescription();
        colorDisabledDescription.SetDefaults();
        colorDisabledDescription.RenderTargets[0].ColorWriteChannels = ColorWriteChannels.None;
        ColorDisabled = colorDisabledDescription;
    }


    public static readonly BlendStateDescription Default;

    public static readonly BlendStateDescription Additive = new(sourceBlend: Blend.SourceAlpha, destinationBlend: Blend.One);

    public static readonly BlendStateDescription AlphaBlend = new(sourceBlend: Blend.One, destinationBlend: Blend.InverseSourceAlpha);

    public static readonly BlendStateDescription NonPremultiplied = new(sourceBlend : Blend.SourceAlpha, destinationBlend : Blend.InverseSourceAlpha);

    public static readonly BlendStateDescription Opaque = new(sourceBlend : Blend.One, destinationBlend : Blend.Zero);

    public static readonly BlendStateDescription ColorDisabled;
}
