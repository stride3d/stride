// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// This class represents a blur to apply to a texture to create a bokeh effect. 
    /// It's not supposed to be used as-is, rather you should use subclasses like <see cref="GaussianBokeh"/>, 
    /// <see cref="McIntoshBokeh"/> or <see cref="TripleRhombiBokeh"/>... which do actually implement a blur technique leading to 
    /// a particular bokeh shape (circular, hexagonal).
    /// </summary>
    public abstract class BokehBlur : ImageEffect
    {
        private float radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="BokehBlur"/> class.
        /// </summary>
        protected BokehBlur()
        {
            Radius = 5f; // Default value
            CoCStrength = 1.0f;
        }

        /// <summary>
        /// Provides a color buffer and a depth buffer to apply the blur to.
        /// </summary>
        /// <param name="colorBuffer">A color buffer to process.</param>
        /// <param name="depthBuffer">The depth buffer corresponding to the color buffer provided.</param>
        public void SetColorDepthInput(Texture colorBuffer, Texture depthBuffer)
        {
            SetInput(0, colorBuffer);
            SetInput(1, depthBuffer);
        }

        [DataMemberIgnore]
        public float CoCStrength { get; set; }

        /// <summary>
        /// Sets the radius of the blur.
        /// A child class can override it to do special processing when a new value is provided.
        /// </summary>
        /// <param name="value">The new value of the blur.</param>
        [DataMemberIgnore]
        public virtual float Radius
        {
            get
            {
                return radius;
            }

            set
            {
                if (value < 0f)
                {
                    throw new ArgumentOutOfRangeException("Radius cannot be < 0");
                }
                if (value < 1f)
                {
                    // We need at least a radius of 1 texel to perform a blur. 
                    value = 1f;
                }
                this.radius = value;
            }
        }
    }
}
