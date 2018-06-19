// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using System.Globalization;

using Xenko.Core;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// A color transform to output the luminance to a specific channel.
    /// </summary>
    [DataContract("LuminanceToChannelTransform")]
    public class LuminanceToChannelTransform : ColorTransform
    {
        private ColorChannel colorChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuminanceToChannelTransform"/> class.
        /// </summary>
        public LuminanceToChannelTransform()
            : this("LuminanceToChannelShader")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuminanceToChannelTransform" /> class.
        /// </summary>
        /// <param name="colorTransformShader">Name of the shader.</param>
        public LuminanceToChannelTransform(string colorTransformShader) : base(colorTransformShader)
        {
            ColorChannel = ColorChannel.A;
        }

        /// <summary>
        /// Gets or sets the color channel to output the luminance. Default is alpha.
        /// </summary>
        /// <value>The color channel.</value>
        [DefaultValue(ColorChannel.A)]
        public ColorChannel ColorChannel
        {
            get
            {
                return colorChannel;
            }
            set
            {
                colorChannel = value;
                GenericArguments = new object[] { value.ToString().ToLowerInvariant() };
            }
        }
    }
}
