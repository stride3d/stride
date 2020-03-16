// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Applies an ambient occlusion effect to a scene. Ambient occlusion is a technique which fakes occlusion for objects close to other opaque objects.
    /// It takes as input a color-buffer where the scene was rendered, with its associated depth-buffer.
    /// You also need to provide the camera configuration you used when rendering the scene.
    /// </summary>
    [DataContract("AmbientOcclusion")]
    public class AmbientOcclusion : ImageEffect
    {
        private ImageEffectShader aoRawImageEffect;
        private ImageEffectShader blurH;
        private ImageEffectShader blurV;
        private string nameGaussianBlurH;
        private string nameGaussianBlurV;
        private float[] offsetsWeights;

        private ImageEffectShader aoApplyImageEffect;

        public AmbientOcclusion()
        {
            //Enabled = false;

            NumberOfSamples = 13;
            ParamProjScale = 0.5f;
            ParamIntensity = 0.2f;
            ParamBias = 0.01f;
            ParamRadius = 1f;
            NumberOfBounces = 2;
            BlurScale = 1.85f;
            EdgeSharpness = 3f;
            TempSize = TemporaryBufferSize.SizeFull;
        }

        /// <userdoc>
        /// The number of pixels sampled to determine how occluded a point is. Higher values reduce noise, but affect performance.
        /// Use with "Blur count to find a balance between results and performance.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(13)]
        [DataMemberRange(1, 50, 1, 5, 0)]
        [Display("Samples")]
        public int NumberOfSamples { get; set; } = 13;

        /// <userdoc>
        /// Scales the sample radius. In most cases, 1 (no scaling) produces the most accurate result.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        [Display("Projection scale")]
        public float ParamProjScale { get; set; } = 0.5f;

        /// <userdoc>
        /// The strength of the darkening effect in occluded areas
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(0.2f)]
        [Display("Intensity")]
        public float ParamIntensity { get; set; } = 0.2f;

        /// <userdoc>
        /// The angle at which Xenko considers an area of geometry an occluder. At high values, only narrow joins and crevices are considered occluders.
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(0.01f)]
        [Display("Sample bias")]
        public float ParamBias { get; set; } = 0.01f;

        /// <userdoc>
        /// Use with "projection scale" to control the radius of the occlusion effect
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(1f)]
        [Display("Sample radius")]
        public float ParamRadius { get; set; } = 1f;

        /// <userdoc>
        /// The number of times the ambient occlusion image is blurred. Higher numbers reduce noise, but can produce artifacts.
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(2)]
        [DataMemberRange(0, 3, 1, 1, 0)]
        [Display("Blur count")]
        public int NumberOfBounces { get; set; } = 2;

        /// <userdoc>
        /// The blur radius in pixels
        /// </userdoc>
        [DataMember(74)]
        [DefaultValue(1.85f)]
        [Display("Blur radius")]
        public float BlurScale { get; set; } = 1.85f;

        /// <userdoc>
        /// How much the blur respects the depth differences of occluded areas. Lower numbers create more blur, but might blur unwanted areas (ie beyond occluded areas).
        /// </userdoc>
        [DataMember(78)]
        [DefaultValue(3f)]
        [Display("Edge sharpness")]
        public float EdgeSharpness { get; set; } = 3f;

        /// <userdoc>
        /// The resolution the ambient occlusion is calculated at. The result is upscaled to the game resolution.
        /// Larger sizes produce better results but use more memory and affect performance.
        /// </userdoc>
        [DataMember(100)]
        [DefaultValue(TemporaryBufferSize.SizeFull)]
        [Display("Buffer size")]
        public TemporaryBufferSize TempSize { get; set; } = TemporaryBufferSize.SizeFull;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            aoApplyImageEffect = ToLoadAndUnload(new ImageEffectShader("ApplyAmbientOcclusionShader"));

            aoRawImageEffect = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionRawAOEffect"));
            aoRawImageEffect.Initialize(Context);

            blurH = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionBlurEffect"));
            blurV = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionBlurEffect", true));
            blurH.Initialize(Context);
            blurV.Initialize(Context);

            // Setup Horizontal parameters
            blurH.Parameters.Set(AmbientOcclusionBlurKeys.VerticalBlur, false);
            blurV.Parameters.Set(AmbientOcclusionBlurKeys.VerticalBlur, true);
        }

        protected override void Destroy()
        {
            base.Destroy();
        }

        /// <summary>
        /// Provides a color buffer and a depth buffer to apply the depth-of-field to.
        /// </summary>
        /// <param name="colorBuffer">A color buffer to process.</param>
        /// <param name="depthBuffer">The depth buffer corresponding to the color buffer provided.</param>
        public void SetColorDepthInput(Texture colorBuffer, Texture depthBuffer)
        {
            SetInput(0, colorBuffer);
            SetInput(1, depthBuffer);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var originalColorBuffer = GetSafeInput(0);
            var originalDepthBuffer = GetSafeInput(1);

            var outputTexture = GetSafeOutput(0);

            var renderView = context.RenderContext.RenderView;

            //---------------------------------
            // Ambient Occlusion
            //---------------------------------

            var tempWidth = (originalColorBuffer.Width * (int)TempSize) / (int)TemporaryBufferSize.SizeFull;
            var tempHeight = (originalColorBuffer.Height * (int)TempSize) / (int)TemporaryBufferSize.SizeFull;
            var aoTexture1 = NewScopedRenderTarget2D(tempWidth, tempHeight, PixelFormat.R8_UNorm, 1);
            var aoTexture2 = NewScopedRenderTarget2D(tempWidth, tempHeight, PixelFormat.R8_UNorm, 1);

            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOKeys.Count, NumberOfSamples > 0 ? NumberOfSamples : 9);

            // Set Near/Far pre-calculated factors to speed up the linear depth reconstruction
            aoRawImageEffect.Parameters.Set(CameraKeys.ZProjection, CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane));

            Vector4 screenSize = new Vector4(originalColorBuffer.Width, originalColorBuffer.Height, 0, 0);
            screenSize.Z = screenSize.X / screenSize.Y;
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ScreenInfo, screenSize);

            // Projection infor used to reconstruct the View space position from linear depth
            var p00 = renderView.Projection.M11;
            var p11 = renderView.Projection.M22;
            var p02 = renderView.Projection.M13;
            var p12 = renderView.Projection.M23;
            Vector4 projInfo = new Vector4(-2.0f / (screenSize.X * p00), -2.0f / (screenSize.Y * p11), (1.0f - p02) / p00, (1.0f + p12) / p11);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ProjInfo, projInfo);

            //**********************************
            // User parameters
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ParamProjScale, ParamProjScale);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ParamIntensity, ParamIntensity);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ParamBias, ParamBias);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ParamRadius, ParamRadius);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionRawAOShaderKeys.ParamRadiusSquared, ParamRadius * ParamRadius);

            aoRawImageEffect.SetInput(0, originalDepthBuffer);
            aoRawImageEffect.SetOutput(aoTexture1);
            aoRawImageEffect.Draw(context, "AmbientOcclusionRawAO");

            for (int bounces = 0; bounces < NumberOfBounces; bounces++)
            {
                if (offsetsWeights == null)
                {
                    offsetsWeights = new[]
                    {
                        //  0.356642f, 0.239400f, 0.072410f, 0.009869f,
                        //  0.398943f, 0.241971f, 0.053991f, 0.004432f, 0.000134f, // stddev = 1.0
                            0.153170f, 0.144893f, 0.122649f, 0.092902f, 0.062970f, // stddev = 2.0
                        //  0.111220f, 0.107798f, 0.098151f, 0.083953f, 0.067458f, 0.050920f, 0.036108f, // stddev = 3.0
                    };

                    nameGaussianBlurH = string.Format("AmbientOcclusionBlurH{0}x{0}", offsetsWeights.Length);
                    nameGaussianBlurV = string.Format("AmbientOcclusionBlurV{0}x{0}", offsetsWeights.Length);
                }

                // Set Near/Far pre-calculated factors to speed up the linear depth reconstruction
                var zProj = CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane);
                blurH.Parameters.Set(CameraKeys.ZProjection, ref zProj);
                blurV.Parameters.Set(CameraKeys.ZProjection, ref zProj);

                // Update permutation parameters
                blurH.Parameters.Set(AmbientOcclusionBlurKeys.Count, offsetsWeights.Length);
                blurH.Parameters.Set(AmbientOcclusionBlurKeys.BlurScale, BlurScale);
                blurH.Parameters.Set(AmbientOcclusionBlurKeys.EdgeSharpness, EdgeSharpness);
                blurH.EffectInstance.UpdateEffect(context.GraphicsDevice);

                blurV.Parameters.Set(AmbientOcclusionBlurKeys.Count, offsetsWeights.Length);
                blurV.Parameters.Set(AmbientOcclusionBlurKeys.BlurScale, BlurScale);
                blurV.Parameters.Set(AmbientOcclusionBlurKeys.EdgeSharpness, EdgeSharpness);
                blurV.EffectInstance.UpdateEffect(context.GraphicsDevice);

                // Update parameters
                blurH.Parameters.Set(AmbientOcclusionBlurShaderKeys.Weights, offsetsWeights);
                blurV.Parameters.Set(AmbientOcclusionBlurShaderKeys.Weights, offsetsWeights);

                // Horizontal pass
                blurH.SetInput(0, aoTexture1);
                blurH.SetInput(1, originalDepthBuffer);
                blurH.SetOutput(aoTexture2);
                blurH.Draw(context, nameGaussianBlurH);

                // Vertical pass
                blurV.SetInput(0, aoTexture2);
                blurV.SetInput(1, originalDepthBuffer);
                blurV.SetOutput(aoTexture1);
                blurV.Draw(context, nameGaussianBlurV);
            }

            aoApplyImageEffect.SetInput(0, originalColorBuffer);
            aoApplyImageEffect.SetInput(1, aoTexture1);
            aoApplyImageEffect.SetOutput(outputTexture);
            aoApplyImageEffect.Draw(context, "AmbientOcclusionApply");
        }

        public enum TemporaryBufferSize
        {
            [Display("Full size")]
            SizeFull = 12,

            [Display("5/6 size")]
            Size1012 = 10,

            [Display("3/4 size")]
            Size0912 = 9,

            [Display("2/3 size")]
            Size0812 = 8,

            [Display("1/2 size")]
            Size0612 = 6,
        }
    }
}
