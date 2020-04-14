// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Known values for <see cref="BlendStateDescription"/>.
    /// </summary>
    public static class BlendStates
    {
        static BlendStates()
        {
            var blendDescription = new BlendStateDescription(Blend.One, Blend.Zero);
            blendDescription.SetDefaults();
            Default = blendDescription;

            var colorDisabledDescription = new BlendStateDescription();
            colorDisabledDescription.SetDefaults();
            colorDisabledDescription.RenderTarget0.ColorWriteChannels = ColorWriteChannels.None;
            ColorDisabled = colorDisabledDescription;
        }

        /// <summary>
        /// A built-in state object with settings for default blend, that is no blend at all.
        /// </summary>
        public static readonly BlendStateDescription Default;

        /// <summary>
        /// A built-in state object with settings for additive blend, that is adding the destination data to the source data without using alpha.
        /// </summary>
        public static readonly BlendStateDescription Additive = new BlendStateDescription(Blend.SourceAlpha, Blend.One);

        /// <summary>
        /// A built-in state object with settings for alpha blend, that is blending the source and destination data using alpha.
        /// </summary>
        public static readonly BlendStateDescription AlphaBlend = new BlendStateDescription(Blend.One, Blend.InverseSourceAlpha);

        /// <summary>
        /// A built-in state object with settings for blending with non-premultipled alpha, that is blending source and destination data using alpha while assuming the color data contains no alpha information.
        /// </summary>
        public static readonly BlendStateDescription NonPremultiplied = new BlendStateDescription(Blend.SourceAlpha, Blend.InverseSourceAlpha);

        /// <summary>
        /// A built-in state object with settings for opaque blend, that is overwriting the source with the destination data.
        /// </summary>
        public static readonly BlendStateDescription Opaque = new BlendStateDescription(Blend.One, Blend.Zero);

        /// <summary>
        /// A built-in state object with settings for no color rendering on target 0, that is only render to depth stencil buffer.
        /// </summary>
        public static readonly BlendStateDescription ColorDisabled;
    }
}
