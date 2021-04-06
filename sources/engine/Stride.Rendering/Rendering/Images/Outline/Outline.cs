// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Images {
    /// <summary>
    /// A fog filter.
    /// </summary>
    [DataContract("Outline")]
    public class Outline : ImageEffect 
    {
        private readonly ImageEffectShader outlineFilter;
        private Texture depthTexture;
        private float zMin, zMax;

        /// <summary>
        /// Initializes a new instance of the <see cref="Outline"/> class.
        /// </summary>
        public Outline()
            : this("OutlineEffect") 
        {
        }

        [DataMember(10)]
        public float NormalWeight { get; set; } = 2f;

        [DataMember(20)]
        public float DepthWeight { get; set; } = 0.2f;

        [DataMember(30)]
        public float NormalNearCutoff { get; set; } = 0.1f;

        /// <summary>
        /// Initializes a new instance of the <see cref="Outline"/> class.
        /// </summary>
        /// <param name="shaderName">Name of the outline shader.</param>
        public Outline(string shaderName) : base(shaderName) 
        {
            if (shaderName == null) throw new ArgumentNullException("outlineFilterName");
            outlineFilter = new ImageEffectShader(shaderName);
        }

        protected override void InitializeCore() 
        {
            base.InitializeCore();
            ToLoadAndUnload(outlineFilter);
        }

        /// <summary>
        /// Provides a color buffer and a depth buffer to apply the fog to.
        /// </summary>
        /// <param name="colorBuffer">A color buffer to process.</param>
        /// <param name="depthBuffer">The depth buffer corresponding to the color buffer provided.</param>
        public void SetColorDepthInput(Texture colorBuffer, Texture depthBuffer, float zMin, float zMax) 
        {
            SetInput(0, colorBuffer);
            depthTexture = depthBuffer;
            this.zMin = zMin;
            this.zMax = zMax;
        }

        protected override void SetDefaultParameters() 
        {
            NormalWeight = 2f;
            DepthWeight = 0.2f;
            NormalNearCutoff = 0.1f;
            base.SetDefaultParameters();
        }

        protected override void DrawCore(RenderDrawContext context) 
        {
            Texture color = GetInput(0);
            Texture output = GetOutput(0);
            if (color == null || output == null || depthTexture == null) 
            {
                return;
            }

            outlineFilter.Parameters.Set(OutlineEffectKeys.ScreenDiffs, new Vector2(0.5f / color.Width, 0.5f / color.Height));
            outlineFilter.Parameters.Set(OutlineEffectKeys.DepthTexture, depthTexture);
            outlineFilter.Parameters.Set(OutlineEffectKeys.zFar, zMax);
            outlineFilter.Parameters.Set(OutlineEffectKeys.zNear, zMin);

            outlineFilter.Parameters.Set(OutlineEffectKeys.NormalWeight, NormalWeight);
            outlineFilter.Parameters.Set(OutlineEffectKeys.DepthWeight, DepthWeight);
            outlineFilter.Parameters.Set(OutlineEffectKeys.NormalNearCutoff, NormalNearCutoff);

            outlineFilter.SetInput(0, color);
            outlineFilter.SetOutput(output);
            ((RendererBase)outlineFilter).Draw(context);
        }
    }
}
