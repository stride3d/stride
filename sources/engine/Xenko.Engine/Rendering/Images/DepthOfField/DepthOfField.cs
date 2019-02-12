// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Applies a depth-of-field effect to a scene.
    /// It takes as input a color-buffer where the scene was rendered, with its associated depth-buffer.
    /// You also need to provide the camera configuration you used when rendering the scene.
    /// You can optionally specify which bokeh technique should be used, the number of LOD levels with 
    /// their Circle-of-Confusion strengths and their resolution.
    /// </summary>
    [DataContract("DepthOfField")]
    public sealed class DepthOfField : ImageEffect
    {
        private const float BokehSizeFactor = 0.05f; // Occupy 5% of the screen, divided by 100, as MaxBokehSize is x 100

        /// <summary>
        /// Maximum size of the bokeh (ie. when the circle of confusion is 1.0).
        /// This is resolution-independent, it is a ratio proportional to the screen width in percentage.
        /// </summary>
        /// <remarks>
        /// This property is not supposed to be modified at each frame since it generates garbage. 
        /// Instead you should set it once for your scene and play with the DOF areas / lens parameters 
        /// to make out-of-focus objects create bigger bokeh shapes.
        /// </remarks>
        /// <userdoc>Specify the maximum size of the bokehs. This value is resolution-independent (proportional to the screen width in percentage).</userdoc>
        [DataMember(10)]
        [DefaultValue(0.1f)]
        [DataMemberRange(0.01f, 1.0f, 0.01f, 0.1f, 2)]
        [Display("Size")]
        public float MaxBokehSize { get; set; }
        
        /// <summary>
        /// Areas of the depth of field: [nearStart, nearEnd, farStart, farEnd] expressed as a 
        /// distance from the camera.
        /// </summary>
        /// <userdoc>Specify the area focused by the depth of field (eg. the not blurred area). 
        /// It is expressed as distances from the camera: [nearStart, nearEnd, farStart, farEnd].</userdoc>
        [DataMember(20)]
        public Vector4 DOFAreas { get; set; } // TODO provide an alternative control with physical lens parameters

        /// <summary>
        /// Affects a preset quality setting, between 0 (lowest quality) and 1 (highest quality).
        /// This auto-configures <cref name="LevelCoCValues"/> and <cref name="LevelDownscaleFactors"/>.
        /// </summary>
        /// <userdoc>The quality of the effect where 0 represents the worst quality and 1 the best one.
        /// Note that increase the quality of the effect significantly increase the rendering time.</userdoc>
        [DataMember(30)]
        [DefaultValue(0.5f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f, 2)]
        public float QualityPreset
        {
            get
            {
                return quality;
            }

            set
            {
                if (value < 0f) value = 0f;
                if (value > 1f) value = 1f;
                quality = value;
                int presetCount = 6;
                int presetRequested = (int)(value * presetCount);
                switch (presetRequested)
                {
                    case 0:
                        // Single level, at 1/4 resolution
                        LevelCoCValues = new[] { 1f };
                        LevelDownscaleFactors = new[] { 2 };
                        break;

                    case 1:
                        // Single level, at half the resolution
                        LevelCoCValues = new[] { 1f };
                        LevelDownscaleFactors = new[] { 1 };
                        break;

                    default:
                        // Multi-levels at half the resolution
                        int lvlNumber = presetRequested;
                        float levelSpacer = 1f / lvlNumber;
                        LevelCoCValues = new float[lvlNumber];
                        LevelDownscaleFactors = new int[lvlNumber];
                        for (int i = 0; i < lvlNumber; i++)
                        {
                            LevelCoCValues[i] = (i + 1) * levelSpacer;
                            LevelDownscaleFactors[i] = 1;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Bokeh technique used to blur each level.
        /// </summary>
        /// <remarks>
        /// This influences the bokeh shape (circular, hexagonal...) as well as the performance.
        /// </remarks>
        /// <userdoc>Specify the technique used to calculate the bokehs.
        /// This influences both the shape of the bokehs and the performances of the effect.</userdoc>
        [DataMember(40)]
        [DefaultValue(BokehTechnique.HexagonalTripleRhombi)]
        public BokehTechnique Technique
        {
            get
            {
                return technique;
            }

            set
            {
                technique = value;
                configurationDirty = true;
            }
        }

        /// <summary>
        /// Auto-focus on the pixel at the center of the screen. 
        /// </summary>
        /// <userdoc>If checked, the depth of field area is automatically adjusted to the pixel at the center of the screen.</userdoc>
        [DataMember(50)]
        [DefaultValue(true)]
        public bool AutoFocus { get; set; }

        /// <summary>
        /// The number of layers with their own CoC strength. Note that you need to define 
        /// at least 1 level of blur, each level of blur should have a CoC stronger than its predecessor and the last
        /// level should always have a CoC of 1.0. Example: { 0.25f, 0.5f, 1.0f }
        /// The higher the number of levels is, the smoother the transition between 2 levels is, but at a performance cost.
        /// </summary>
        [DataMemberIgnore]
        public float[] LevelCoCValues
        {
            get
            {
                return levelCoCValues;
            }

            set
            {
                levelCoCValues = value;
                configurationDirty = true;
            }
        }

        /// <summary>
        /// For each level defined in <cref name="LevelCoCValues"/> you can define a downscale factor (a power of 2) 
        /// at which to operate. 
        /// When not specified, the levels are treated by default at half the resolution of the original image.
        /// Example: for { 1, 2 }, the first level will be processed at half the original resolution, and the second level at 1/4.
        /// The array provided must be of the same size as the <cref name="LevelCoCValues"/> array.
        /// </summary>
        [DataMemberIgnore]
        public int[] LevelDownscaleFactors
        {
            get
            {
                return levelDownscaleFactors;
            }

            set
            {
                levelDownscaleFactors = value;
                configurationDirty = true;
            }
        }

        // Tells if we need to regenerate the "pipeline" of the DoF.
        // Set to true when the technique changes, or the number/resolution of layers is modified.
        private bool configurationDirty = true;

        // Properties
        private BokehTechnique technique;
        private float[] levelCoCValues;
        private int[] levelDownscaleFactors;
        private float quality;

        // Util to scale images
        private ImageScaler textureScaler;

        private ImageEffectShader thresholdAlphaCoC;
        private ImageEffectShader thresholdAlphaCoCFront;

        // For auto-focus
        private ImageEffect pointDepthShader;
        private ImageReadback<Half> depthReadBack;
        private Texture depthCenter1x1;
        private float autoFocusDistanceTarget = 10f;
        private float autoFocusDistanceCurrent = 10f;

        // Transforms "Color and depth" -> "linear depth and CoC"
        private ImageEffectShader coclinearDepthMapEffect;

        // Used to blur the CoC map
        private CoCMapBlur cocMapBlur;

        // Used for the final pass interpolating between some CoC levels.
        private ImageEffectShader combineLevelsEffect;
        private ImageEffectShader combineLevelsFrontEffect;

        // Represents a level of CoC
        private class CoCLevelConfig
        {
            // CoC value in [0.0, 1.0] that the current level represents.
            public float CoCValue;

            // Downscale factor indicating which resolution we'll use to render this level.
            public int DownscaleFactor;

            // Each level has its own instance of blur effect.
            // Because if we rely on a single instance for all the levels, modifying the blur radius 
            // will allocate memory and we don't want to generate garbage at each frame.
            public BokehBlur BlurEffect;
        }

        // List of each CoC level with its configuration
        private List<CoCLevelConfig> cocLevels = new List<CoCLevelConfig>();
        // Cache the different CoC level values for the shader (avoid GC)
        private float[] combineShaderCocLevelValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthOfField"/> class.
        /// </summary>
        public DepthOfField()
        {
            // Some preset values
            DOFAreas = new Vector4(0.5f, 6f, 50f, 200f);
            MaxBokehSize = 0.1f; //ratio of the width (resolution independent)
            Technique = BokehTechnique.HexagonalTripleRhombi;
            QualityPreset = 0.5f;
            AutoFocus = true;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            coclinearDepthMapEffect = ToLoadAndUnload(new ImageEffectShader("CoCLinearDepthShader"));
            combineLevelsEffect = ToLoadAndUnload(new ImageEffectShader("CombineLevelsFromCoCEffect"));
            combineLevelsFrontEffect = ToLoadAndUnload(new ImageEffectShader("CombineFrontCoCEffect"));
            combineLevelsFrontEffect.BlendState = BlendStates.AlphaBlend;
            textureScaler = ToLoadAndUnload(new ImageScaler(SamplingPattern.Expanded));
            cocMapBlur = ToLoadAndUnload(new CoCMapBlur());
            thresholdAlphaCoC = ToLoadAndUnload(new ImageEffectShader("ThresholdAlphaCoC"));
            thresholdAlphaCoCFront = ToLoadAndUnload(new ImageEffectShader("ThresholdAlphaCoCFront"));
            pointDepthShader = ToLoadAndUnload(new ImageEffectShader("PointDepth"));
            depthReadBack = ToLoadAndUnload(new ImageReadback<Half>());
            depthCenter1x1 = Texture.New2D(GraphicsDevice, 1, 1, 1, PixelFormat.R16_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
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

        /// <summary>
        /// Caches the current configuration and re-generates the filter effect of each LOD level to avoid 
        /// generating garbage a each frame.
        /// </summary>
        public void SetupTechnique()
        {
            // Sanity checks
            if (levelCoCValues == null && levelDownscaleFactors != null)
            {
                throw new ArgumentOutOfRangeException("You cannot provide downscale factors without CoC values first.");
            }

            if (levelCoCValues != null && levelDownscaleFactors != null)
            {
                if (levelCoCValues.Length != levelDownscaleFactors.Length)
                {
                    throw new ArgumentOutOfRangeException("levelCoCValues and levelDownscaleFactors must be arrays of the same size!");
                }
            }

            if (levelCoCValues != null && levelCoCValues.Length == 0)
            {
                if (levelCoCValues.Length == 0)
                {
                    throw new ArgumentOutOfRangeException("DoF configuration needs at least one blur level!");
                }
                if (levelCoCValues[levelCoCValues.Length - 1] != 1f)
                {
                    throw new ArgumentOutOfRangeException("Last blurriest level must have a CoC of 1.0!");
                }
            }

            // Default values: 1 original image and 1 blur level
            int levelCount = 2;
            if (levelCoCValues != null)
            {
                levelCount = levelCoCValues.Length + 1;
            }

            CleanupEffects();
            cocLevels.Clear();
            combineShaderCocLevelValues = new float[levelCount];
            
            // Special case: level 0 is always our original image
            cocLevels.Add(new CoCLevelConfig { CoCValue = 0f, DownscaleFactor = 0 });
            combineShaderCocLevelValues[0] = 0f;

            // Add a description for each of the blur levels
            for (int i = 1; i < levelCount; i++)
            {
                var blurEffect = Technique.ToBlurInstance();
                blurEffect.Initialize(Context);

                CoCLevelConfig lvlConfig = new CoCLevelConfig
                {
                    CoCValue = (levelCoCValues != null) ? levelCoCValues[i - 1] : 1f,
                    DownscaleFactor = (levelDownscaleFactors != null) ? levelDownscaleFactors[i - 1] : 1,
                    BlurEffect = blurEffect,
                };
                cocLevels.Add(lvlConfig);

                // Cache shader parameters
                combineShaderCocLevelValues[i] = lvlConfig.CoCValue;
            }

            configurationDirty = false;
        }

        // Temporary old a set of downscaled images for the current frame.
        // Match: downscale level -> Texture
        private Dictionary<int, Texture> downscaledSources = new Dictionary<int, Texture>();

        protected override void DrawCore(RenderDrawContext context)
        {
            var originalColorBuffer = GetSafeInput(0);
            var originalDepthBuffer = GetSafeInput(1);

            var outputTexture = GetSafeOutput(0);

            if (configurationDirty) SetupTechnique();

            // Preparation phase: create different downscaled versions of the original image, later needed by the bokeh blur shaders. 
            // TODO use ImageMultiScaler instead?
            downscaledSources.Clear();

            // First we linearize the depth and compute the CoC map based on the user lens configuration.
            // Render target will contain "CoC"(16 bits) "Linear depth"(16bits).
            var cocLinearDepthTexture = GetScopedRenderTarget(originalColorBuffer.Description, 1f, PixelFormat.R16G16_Float);

            var renderView = context.RenderContext.RenderView;
            var farPlane = renderView.FarClipPlane;

            var depthAreas = DOFAreas;
            if (AutoFocus)
            {
                // TODO replace this by physical camera parameters (aperture, focus distance...)
                var diffToTarget = (autoFocusDistanceTarget - autoFocusDistanceCurrent);
                var maxAmplitude = farPlane * 0.2f;
                diffToTarget = MathUtil.Clamp(diffToTarget, -maxAmplitude, maxAmplitude);
                autoFocusDistanceCurrent = autoFocusDistanceCurrent + 0.1f * diffToTarget;
                if (autoFocusDistanceCurrent < renderView.NearClipPlane * 2.0f) autoFocusDistanceCurrent = renderView.NearClipPlane * 2.0f;
                depthAreas = new Vector4(renderView.NearClipPlane, autoFocusDistanceCurrent, autoFocusDistanceCurrent, autoFocusDistanceCurrent + farPlane * 0.5f);
            }

            coclinearDepthMapEffect.SetInput(0, originalDepthBuffer);
            coclinearDepthMapEffect.SetOutput(cocLinearDepthTexture);
            coclinearDepthMapEffect.Parameters.Set(CircleOfConfusionKeys.depthAreas, depthAreas);
            coclinearDepthMapEffect.Parameters.Set(CameraKeys.ZProjection, CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane));
            coclinearDepthMapEffect.Draw(context, "CoC_LinearDepth");

            if (AutoFocus)
            {
                // Reads the center depth of the previous frame and use it as a new target
                // TODO single pixel is really small, average some disk area instead?
                pointDepthShader.Parameters.Set(PointDepthKeys.Coordinate, new Vector2(0.5f, 0.5f));
                pointDepthShader.SetInput(cocLinearDepthTexture);
                pointDepthShader.SetOutput(depthCenter1x1);
                pointDepthShader.Draw(context, "Center Depth");

                depthReadBack.SetInput(depthCenter1x1);
                depthReadBack.Draw(context, "Center_Depth_Readback");
                var centerDepth = depthReadBack.Result[0];
                autoFocusDistanceTarget = centerDepth;
            }

            // Find the smallest downscale we should go down to.
            var maxDownscale = 0;
            foreach (var cocLevel in cocLevels)
            {
                if (cocLevel.DownscaleFactor > maxDownscale) maxDownscale = cocLevel.DownscaleFactor;
            }

            // Create a series of downscale, with anti-bleeding treatment
            for (int i = 0; i <= maxDownscale; i++)
            {
                var downSizedTexture = originalColorBuffer;
                if (i > 0)
                {
                    downSizedTexture = GetScopedRenderTarget(originalColorBuffer.Description, 1f / (float)Math.Pow(2f, i), originalColorBuffer.Description.Format);
                    textureScaler.SetInput(0, downscaledSources[i - 1]);
                    textureScaler.SetOutput(downSizedTexture);
                    textureScaler.Draw(context, "DownScale_Factor{0}", i);
                }

                downscaledSources[i] = downSizedTexture; 
            }

            // We create a blurred version of the CoC map. 
            // This is useful to avoid silhouettes appearing when the CoC changes abruptly.
            var blurredCoCTexture = NewScopedRenderTarget2D(cocLinearDepthTexture.Description);
            cocMapBlur.Radius = 6f / 720f * cocLinearDepthTexture.Description.Height; // 6 pixels at 720p
            cocMapBlur.SetInput(0, cocLinearDepthTexture);
            cocMapBlur.SetOutput(blurredCoCTexture);
            cocMapBlur.Draw(context, "CoC_BlurredMap");

            // Creates all the levels with different CoC strengths.
            // (Skips level with CoC 0 which is always the original buffer.)
            combineLevelsEffect.Parameters.Set(CombineLevelsFromCoCKeys.LevelCount, cocLevels.Count);
            combineLevelsEffect.SetInput(0, cocLinearDepthTexture);
            combineLevelsEffect.SetInput(1, blurredCoCTexture);
            combineLevelsEffect.SetInput(2, originalColorBuffer);

            combineLevelsFrontEffect.Parameters.Set(CombineLevelsFromCoCKeys.LevelCount, cocLevels.Count);
            combineLevelsFrontEffect.SetInput(0, cocLinearDepthTexture);
            combineLevelsFrontEffect.SetInput(1, blurredCoCTexture);
            combineLevelsFrontEffect.SetInput(2, originalColorBuffer);

            float previousCoC = 0f;
            for (int i = 1; i < cocLevels.Count; i++)
            {
                // We render a blurred version of the original scene into a downscaled render target.
                // Blur strength depends on the current level CoC value. 

                var levelConfig = cocLevels[i];
                var textureToBlur = downscaledSources[levelConfig.DownscaleFactor];
                float downscaleFactor = 1f / (float)(Math.Pow(2f, levelConfig.DownscaleFactor));
                var blurOutput = GetScopedRenderTarget(originalColorBuffer.Description, downscaleFactor, originalColorBuffer.Description.Format);
                var blurOutputFront = NewScopedRenderTarget2D(blurOutput.Description);
                float blurRadius = (MaxBokehSize * BokehSizeFactor) * levelConfig.CoCValue * downscaleFactor * originalColorBuffer.Width;
                if (blurRadius < 1f) blurRadius = 1f;

                //---------------------------------
                // Far out-of-focus
                //---------------------------------

                // Pre-process the layer for the current CoC
                // This removes areas which might wrongly bleed into our image when blurring. 
                var alphaTextureToBlur = NewScopedRenderTarget2D(textureToBlur.Description);
                thresholdAlphaCoC.Parameters.Set(ThresholdAlphaCoCKeys.CoCReference, previousCoC);
                thresholdAlphaCoC.Parameters.Set(ThresholdAlphaCoCKeys.CoCCurrent, levelConfig.CoCValue);
                thresholdAlphaCoC.SetInput(0, textureToBlur);
                thresholdAlphaCoC.SetInput(1, cocLinearDepthTexture);
                thresholdAlphaCoC.SetOutput(alphaTextureToBlur);
                thresholdAlphaCoC.Draw(context, "Alphaize_Far_{0}", i);
                textureToBlur = alphaTextureToBlur;

                // TODO Quality up: make the opaque areas "bleed" into the areas we just made transparent

                // Apply the bokeh blur effect
                BokehBlur levelBlur = levelConfig.BlurEffect;
                levelBlur.CoCStrength = levelConfig.CoCValue;
                levelBlur.Radius = blurRadius; // This doesn't generate garbage if the radius value doesn't change.
                levelBlur.SetInput(0, textureToBlur);
                levelBlur.SetOutput(blurOutput);
                levelBlur.Draw(context, "CoC_LoD_Layer_Far_{0}", i);
                combineLevelsEffect.SetInput(i + 2, blurOutput);

                //---------------------------------
                // Near out-of-focus
                //---------------------------------

                // Negates CoC values and makes background objects transparent
                thresholdAlphaCoCFront.Parameters.Set(ThresholdAlphaCoCFrontKeys.CoCReference, previousCoC);
                thresholdAlphaCoCFront.Parameters.Set(ThresholdAlphaCoCFrontKeys.CoCCurrent, levelConfig.CoCValue);
                thresholdAlphaCoCFront.SetInput(0, downscaledSources[levelConfig.DownscaleFactor]);
                thresholdAlphaCoCFront.SetInput(1, cocLinearDepthTexture);
                thresholdAlphaCoCFront.SetOutput(alphaTextureToBlur);
                thresholdAlphaCoCFront.Draw(context, "Alphaize_Near_{0}", i);
                textureToBlur = alphaTextureToBlur;

                // Apply the bokeh blur effect
                levelBlur.SetInput(0, textureToBlur);
                levelBlur.SetOutput(blurOutputFront);
                levelBlur.Draw(context, "CoC_LoD_Layer_Near_{0}", i);
                combineLevelsFrontEffect.SetInput(i + 2, blurOutputFront);

                previousCoC = levelConfig.CoCValue;
            }

            // Far out-of-focus: each pixel, depending on its CoC, interpolates its color from 
            // the original color buffer and blurred buffer(s). 
            combineLevelsEffect.Parameters.Set(CombineLevelsFromCoCShaderKeys.CoCLevelValues, combineShaderCocLevelValues);
            combineLevelsEffect.SetOutput(outputTexture);
            combineLevelsEffect.Draw(context, "CoCLevelCombineInterpolation");

            // Finally add front out-of-focus objects on the top of the scene

            // TODO Quality up: instead of merging all the layers for each pixel, merge only
            // the relevant layer(s) closest to the pixel CoC. 
            combineLevelsFrontEffect.SetOutput(outputTexture);
            combineLevelsFrontEffect.Draw(context, "CoCLevelCombineInterpolationFront");

            // Release any reference
            downscaledSources.Clear();
        }

        protected override void Destroy()
        {
            CleanupEffects();
            base.Destroy();
        }

        // Disposes the effect used by each LOD layer.
        private void CleanupEffects()
        {
            foreach (var cocLevelConfig in cocLevels)
            {
                var blurEffect = cocLevelConfig.BlurEffect;
                if (blurEffect != null) blurEffect.Dispose();
            }
        }

        // Gets a new temporary render target matching the description, but with scale and format overridable.
        private Texture GetScopedRenderTarget(TextureDescription desc, float scale, PixelFormat format) 
        {
            return NewScopedRenderTarget2D(
                        (int)(desc.Width * scale),
                        (int)(desc.Height * scale),
                        format,
                        TextureFlags.ShaderResource | TextureFlags.RenderTarget);
        }
    }
}
