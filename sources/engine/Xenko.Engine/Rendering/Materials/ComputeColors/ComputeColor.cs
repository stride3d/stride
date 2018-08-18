// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials.ComputeColors
{
    [DataContract("ComputeColor")]
    [Display("Color")]
    public class ComputeColor : ComputeValueBase<Color4>, IComputeColor
    {
        private bool premultiplyAlpha;
        private bool hasChanged = true;

        // Possible optimization will be to keep this on the ComputeValueBase<T> side
        private Color4 cachedColor;

        /// <summary>
        /// Gets or sets a value indicating whether to convert the texture in pre-multiplied alpha.
        /// </summary>
        /// <value><c>true</c> to convert the texture in pre-multiplied alpha.; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Pre-multiply the color values by the alpha value
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(true)]
        [Display("Premultiply alpha")]
        public bool PremultiplyAlpha
        {
            get { return premultiplyAlpha; }
            set
            {
                hasChanged = (premultiplyAlpha != value);
                premultiplyAlpha = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeColor"/> class.
        /// </summary>
        public ComputeColor()
            : this(Color4.Black)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeColor"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ComputeColor(Color4 value)
            : base(value)
        {
            premultiplyAlpha = true;

            cachedColor = Color4.Black;

            // Force recompilation of the shader mixins the first time ComputeColor is created by setting the value to true
            hasChanged = true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Color";
        }

        /// <inheritdoc/>
        public bool HasChanged
        {
            get
            {
                if (!hasChanged && cachedColor == Value)
                    return false;

                hasChanged = false;
                cachedColor = Value;
                return true;
            }
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var key = context.GetParameterKey(Key ?? baseKeys.ValueBaseKey ?? MaterialKeys.GenericValueColor4);

            // Store the color in Linear space
            var color = baseKeys.IsColor ? Value.ToColorSpace(context.ColorSpace) : Value;
            if (PremultiplyAlpha)
                color = Color4.PremultiplyAlpha(color);
            
            if (key is ValueParameterKey<Color4>)
            {
                context.Parameters.Set((ValueParameterKey<Color4>)key, color);
            }
            else if (key is ValueParameterKey<Vector4>)
            {
                context.Parameters.Set((ValueParameterKey<Vector4>)key, color);
            }
            else if (key is ValueParameterKey<Color3>)
            {
                context.Parameters.Set((ValueParameterKey<Color3>)key, (Color3)color);
            }
            else if (key is ValueParameterKey<Vector3>)
            {
                context.Parameters.Set((ValueParameterKey<Vector3>)key, (Vector3)color);
            }
            else
            {
                context.Log.Error($"Unexpected ParameterKey [{key}] for type [{key.PropertyType}]. Expecting a [Vector3/Color3] or [Vector4/Color4]");
            }
            UsedKey = key;

            return new ShaderClassSource("ComputeColorConstantColorLink", key);
            // return new ShaderClassSource("ComputeColorFixed", MaterialUtility.GetAsShaderString(Value));
        }
    }
}
