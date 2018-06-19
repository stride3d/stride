// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// A bright pass filter.
    /// </summary>
    [DataContract("BrightFilter")]
    public class BrightFilter : ImageEffect
    {
        // TODO: Add Brightpass filters based on average luminance and key value, taking into account the tonemap
        private readonly ImageEffectShader brightPassFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrightFilter"/> class.
        /// </summary>
        public BrightFilter()
            : this("BrightFilterShader")
        {
            Threshold = 0.2f;
            Steepness = 1.0f;
            Color = new Color3(1.0f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrightFilter"/> class.
        /// </summary>
        /// <param name="brightPassShaderName">Name of the bright pass shader.</param>
        public BrightFilter(string brightPassShaderName) : base(brightPassShaderName)
        {
            if (brightPassShaderName == null) throw new ArgumentNullException("brightPassShaderName");
            brightPassFilter = new ImageEffectShader(brightPassShaderName);
        }

        /// <summary>
        /// Gets or sets the threshold relative to the <see cref="WhitePoint"/>.
        /// </summary>
        /// <value>The threshold.</value>
        /// <userdoc>The value of the intensity threshold used to identify bright areas</userdoc>
        [DataMember(10)]
        [DefaultValue(0.2f)]
        public float Threshold { get; set; }

        /// <summary>
        /// Gets or sets the smoothstep steepness for bright pass filtering
        /// </summary>
        [DataMember(15)]
        [DefaultValue(1.0f)]
        public float Steepness { get; set; }

        /// <summary>
        /// Modulate the bloom by a certain color.
        /// </summary>
        /// <value>The color.</value>
        /// <userdoc>Modulates bright areas with the provided color. It affects the color of sub-sequent bloom, light-streak effects.</userdoc>
        [DataMember(20)]
        public Color3 Color { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();
            ToLoadAndUnload(brightPassFilter);
        }

        protected override void SetDefaultParameters()
        {
            Color = new Color3(1.0f);

            base.SetDefaultParameters();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);
            if (input == null || output == null)
            {
                return;
            }
        
            brightPassFilter.Parameters.Set(BrightFilterShaderKeys.ThresholdOffset, Threshold);
            brightPassFilter.Parameters.Set(BrightFilterShaderKeys.BrightPassSteepness, Steepness);
            brightPassFilter.Parameters.Set(BrightFilterShaderKeys.ColorModulator, Color.ToColorSpace(GraphicsDevice.ColorSpace));
            
            brightPassFilter.SetInput(input);
            brightPassFilter.SetOutput(output);
            ((RendererBase)brightPassFilter).Draw(context);
        }
    }
}
