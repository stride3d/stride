// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Colors
{
    /// <summary>
    /// A light color described by a rgb color
    /// </summary>
    [DataContract("ColorRgbProvider")]
    [DataAlias("LightColorRgb")]
    [Display("RGB")]
    public class ColorRgbProvider : IColorProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRgbProvider"/> class.
        /// </summary>
        public ColorRgbProvider()
        {
            Value = new Color3(1.0f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRgbProvider"/> class.
        /// </summary>
        /// <param name="color">The color.</param>
        public ColorRgbProvider(Color3 color)
        {
            Value = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRgbProvider"/> class.
        /// </summary>
        /// <param name="color">The color.</param>
        public ColorRgbProvider(Color color)
        {
            Value = (Color3)color;
        }

        /// <summary>
        /// Gets or sets the light color in rgb.
        /// </summary>
        /// <value>The color.</value>
        [DataMember(10)]
        [DataAlias("Color")]
        [InlineProperty]
        public Color3 Value { get; set; }

        public Color3 ComputeColor()
        {
            return Value;
        }
    }
}
