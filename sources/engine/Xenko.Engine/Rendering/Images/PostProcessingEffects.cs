// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Compositing;
using Xenko.Rendering.Materials;
using Xenko.Rendering.SubsurfaceScattering;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// A default bundle of <see cref="ImageEffect"/>.
    /// </summary>
    [DataContract("PostProcessingEffects")]
    [Display("Post-processing effects")]
    public sealed class PostProcessingEffects : ImageEffect, IImageEffectRenderer, IPostProcessingEffects
    {
        private LuminanceEffect luminanceEffect;
        private ColorTransformGroup colorTransformsGroup;

        private ImageEffectShader rangeCompress;
        private ImageEffectShader rangeDecompress;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostProcessingEffects" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public PostProcessingEffects(IServiceRegistry services)
            : this(RenderContext.GetShared(services))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostProcessingEffects"/> class.
        /// </summary>
        public PostProcessingEffects()
        {
            AmbientOcclusion = new AmbientOcclusion();
            LocalReflections = new LocalReflections();
            DepthOfField = new DepthOfField();
            luminanceEffect = new LuminanceEffect();
            BrightFilter = new BrightFilter();
            Bloom = new Bloom();
            LightStreak = new LightStreak();
            LensFlare = new LensFlare();
            Antialiasing = new FXAAEffect();
            rangeCompress = new ImageEffectShader("RangeCompressorShader");
            rangeDecompress = new ImageEffectShader("RangeDecompressorShader");
            colorTransformsGroup = new ColorTransformGroup();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostProcessingEffects"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public PostProcessingEffects(RenderContext context)
            : this()
        {
            Initialize(context);
        }

        /// <inheritdoc/>
        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets the ambient occlusion effect.
        /// </summary>
        /// <userdoc>
        /// Darkens areas where light is occluded by opaque objects, such as corners and crevices
        /// </userdoc>
        [DataMember(8)]
        [Category]
        public AmbientOcclusion AmbientOcclusion { get; private set; }

        /// <summary>
        /// Gets the local reflections effect.
        /// </summary>
        /// <value>The local reflection technique.</value>
        /// <userdoc>Reflect the scene in glossy materials</userdoc>
        [DataMember(9)]
        [Category]
        public LocalReflections LocalReflections { get; private set; }

        /// <summary>
        /// Gets the depth of field effect.
        /// </summary>
        /// <value>The depth of field.</value>
        /// <userdoc>Accentuate regions of the image by blurring objects in the foreground or background</userdoc>
        [DataMember(10)]
        [Category]
        public DepthOfField DepthOfField { get; private set; }

        /// <summary>
        /// Gets the bright pass-filter.
        /// </summary>
        /// <value>The bright filter.</value>
        /// <userdoc>The bright filter isn't an effect by itself; 
        /// it extracts the brightest areas of the image and gives it to effects that use it (eg bloom, light streaks, lens flares).</userdoc>
        [DataMember(20)]
        [Category]
        public BrightFilter BrightFilter { get; private set; }

        /// <summary>
        /// Gets the bloom effect.
        /// </summary>
        /// <value>The bloom.</value>
        /// <userdoc>Bleed bright areas into surrounding areas</userdoc>
        [DataMember(30)]
        [Category]
        public Bloom Bloom { get; private set; }

        /// <summary>
        /// Gets the light streak effect.
        /// </summary>
        /// <value>The light streak.</value>
        /// <userdoc>Bleed bright points along streaks</userdoc>
        [DataMember(40)]
        [Category]
        public LightStreak LightStreak { get; private set; }

        /// <summary>
        /// Gets the lens flare effect.
        /// </summary>
        /// <value>The lens flare.</value>
        /// <userdoc>Simulate artifacts produced by the internal reflection or scattering of light within camera lenses</userdoc>
        [DataMember(50)]
        [Category]
        public LensFlare LensFlare { get; private set; }

        /// <summary>
        /// Gets the final color transforms.
        /// </summary>
        /// <value>The color transforms.</value>
        /// <userdoc>Perform a transformation onto the image colors</userdoc>
        [DataMember(70)]
        [Category]
        public ColorTransformGroup ColorTransforms => colorTransformsGroup;

        /// <summary>
        /// Gets the antialiasing effect.
        /// </summary>
        /// <value>The antialiasing.</value>
        /// <userdoc>Perform anti-aliasing filtering, smoothing the jagged edges of models</userdoc>
        [DataMember(80)]
        [Display("Type", "Antialiasing")]
        public IScreenSpaceAntiAliasingEffect Antialiasing { get; set; } // TODO: Unload previous anti aliasing

        /// <summary>
        /// Disables all post processing effects.
        /// </summary>
        public void DisableAll()
        {
            AmbientOcclusion.Enabled = false;
            LocalReflections.Enabled = false;
            DepthOfField.Enabled = false;
            Bloom.Enabled = false;
            LightStreak.Enabled = false;
            LensFlare.Enabled = false;
            Antialiasing.Enabled = false;
            rangeCompress.Enabled = false;
            rangeDecompress.Enabled = false;
            colorTransformsGroup.Enabled = false;
        }

        public override void Reset()
        {
            // TODO: Check how to reset other effects too
            // Reset the luminance effect
            luminanceEffect.Reset();

            base.Reset();
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            AmbientOcclusion = ToLoadAndUnload(AmbientOcclusion);
            LocalReflections = ToLoadAndUnload(LocalReflections);
            DepthOfField = ToLoadAndUnload(DepthOfField);
            luminanceEffect = ToLoadAndUnload(luminanceEffect);
            BrightFilter = ToLoadAndUnload(BrightFilter);
            Bloom = ToLoadAndUnload(Bloom);
            LightStreak = ToLoadAndUnload(LightStreak);
            LensFlare = ToLoadAndUnload(LensFlare);
            //this can be null if no SSAA is selected in the editor
            if (Antialiasing != null) Antialiasing = ToLoadAndUnload(Antialiasing);

            rangeCompress = ToLoadAndUnload(rangeCompress);
            rangeDecompress = ToLoadAndUnload(rangeDecompress);

            colorTransformsGroup = ToLoadAndUnload(colorTransformsGroup);
        }

        public void Collect(RenderContext context)
        {
        }

        public void Draw(RenderDrawContext drawContext, RenderOutputValidator outputValidator, Texture[] inputs, Texture inputDepthStencil, Texture outputTarget)
        {
            var colorIndex = outputValidator.Find<ColorTargetSemantic>();
            if (colorIndex < 0)
                return;
            
            SetInput(0, inputs[colorIndex]);
            SetInput(1, inputDepthStencil);

            var normalsIndex = outputValidator.Find<NormalTargetSemantic>();
            if (normalsIndex >= 0)
            {
                SetInput(2, inputs[normalsIndex]);
            }

            var specularRoughnessIndex = outputValidator.Find<SpecularColorRoughnessTargetSemantic>();
            if (specularRoughnessIndex >= 0)
            {
                SetInput(3, inputs[specularRoughnessIndex]);
            }

            var reflectionIndex0 = outputValidator.Find<OctahedronNormalSpecularColorTargetSemantic>();
            var reflectionIndex1 = outputValidator.Find<EnvironmentLightRoughnessTargetSemantic>();
            if (reflectionIndex0 >= 0 && reflectionIndex1 >= 0)
            {
                SetInput(4, inputs[reflectionIndex0]);
                SetInput(5, inputs[reflectionIndex1]);
            }

            var velocityIndex = outputValidator.Find<VelocityTargetSemantic>();
            if (velocityIndex != -1)
            {
                SetInput(6, inputs[velocityIndex]);
            }

            SetOutput(outputTarget);
            Draw(drawContext);
        }

        public bool RequiresVelocityBuffer => Antialiasing?.RequiresVelocityBuffer ?? false;

        public bool RequiresNormalBuffer => LocalReflections.Enabled;

        public bool RequiresSpecularRoughnessBuffer => LocalReflections.Enabled;

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);
            if (input == null || output == null)
            {
                return;
            }

            var inputDepthTexture = GetInput(1); // Depth

            // Update the parameters for this post effect
            if (!Enabled)
            {
                if (input != output)
                {
                    Scaler.SetInput(input);
                    Scaler.SetOutput(output);
                    Scaler.Draw(context);
                }
                return;
            }

            // If input == output, than copy the input to a temporary texture
            if (input == output)
            {
                var newInput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                context.CommandList.Copy(input, newInput);
                input = newInput;
            }
            
            var currentInput = input;

            var fxaa = Antialiasing as FXAAEffect;
            bool aaFirst = Bloom != null && Bloom.StableConvolution;
            bool needAA = Antialiasing != null && Antialiasing.Enabled;

            // do AA here, first. (hybrid method from Karis2013)
            if (aaFirst && needAA)
            {
                // do AA:
                if (fxaa != null)
                    fxaa.InputLuminanceInAlpha = true;

                Antialiasing.SetInput(1, inputDepthTexture);

                bool requiresVelocityBuffer = Antialiasing.RequiresVelocityBuffer;
                if (requiresVelocityBuffer)
                {
                    Antialiasing.SetInput(2, GetInput(6));
                }

                var aaSurface = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                if (Antialiasing.NeedRangeDecompress)
                {
                    // explanation:
                    // The Karis method (Unreal Engine 4.1x), uses a hybrid pipeline to execute AA.
                    // The AA is usually done at the end of the pipeline, but we don't benefit from
                    // AA for the posteffects, which is a shame.
                    // The Karis method, executes AA at the beginning, but for AA to be correct, it must work post tonemapping,
                    // and even more in fact, in gamma space too. Plus, it waits for the alpha=luma to be a "perceptive luma" so also gamma space.
                    // in our case, working in gamma space created monstruous outlining artefacts around eggageratedely strong constrasted objects (way in hdr range).
                    // so AA works in linear space, but still with gamma luma, as a light tradeoff to supress artefacts.

                    // create a 16 bits target for FXAA:

                    // render range compression & perceptual luma to alpha channel:
                    rangeCompress.SetInput(currentInput);
                    rangeCompress.SetOutput(aaSurface);
                    rangeCompress.Draw(context);

                    Antialiasing.SetInput(0, aaSurface);
                    Antialiasing.SetOutput(currentInput);
                    Antialiasing.Draw(context);

                    // reverse tone LDR to HDR:
                    rangeDecompress.SetInput(currentInput);
                    rangeDecompress.SetOutput(aaSurface);
                    rangeDecompress.Draw(context);
                }
                else
                {
                    Antialiasing.SetInput(0, currentInput);
                    Antialiasing.SetOutput(aaSurface);
                    Antialiasing.Draw(context);
                }

                currentInput = aaSurface;
            }

            if (AmbientOcclusion.Enabled && inputDepthTexture != null)
            {
                // Ambient Occlusion
                var aoOutput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                AmbientOcclusion.SetColorDepthInput(currentInput, inputDepthTexture);
                AmbientOcclusion.SetOutput(aoOutput);
                AmbientOcclusion.Draw(context);
                currentInput = aoOutput;
            }

            if (LocalReflections.Enabled && inputDepthTexture != null)
            {
                var normalsBuffer = GetInput(2);
                var specularRoughnessBuffer = GetInput(3);

                if (normalsBuffer != null && specularRoughnessBuffer != null)
                {
                    // Local reflections
                    var rlrOutput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                    LocalReflections.SetInputSurfaces(currentInput, inputDepthTexture, normalsBuffer, specularRoughnessBuffer);
                    LocalReflections.SetOutput(rlrOutput);
                    LocalReflections.Draw(context);
                    currentInput = rlrOutput;
                }
            }

            if (DepthOfField.Enabled && inputDepthTexture != null)
            {
                // DoF
                var dofOutput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                DepthOfField.SetColorDepthInput(currentInput, inputDepthTexture);
                DepthOfField.SetOutput(dofOutput);
                DepthOfField.Draw(context);
                currentInput = dofOutput;
            }

            // Luminance pass (only if tone mapping is enabled)
            // TODO: This is not super pluggable to have this kind of dependencies. Check how to improve this
            var toneMap = colorTransformsGroup.Transforms.Get<ToneMap>();
            if (colorTransformsGroup.Enabled && toneMap != null && toneMap.Enabled)
            {
                Texture luminanceTexture = null;
                if (toneMap.UseLocalLuminance)
                {
                    const int localLuminanceDownScale = 3;

                    // The luminance chain uses power-of-two intermediate targets, so it expects to output to one as well
                    var lumWidth = Math.Min(MathUtil.NextPowerOfTwo(currentInput.Size.Width), MathUtil.NextPowerOfTwo(currentInput.Size.Height));
                    lumWidth = Math.Max(1, lumWidth / 2);

                    var lumSize = new Size3(lumWidth, lumWidth, 1).Down2(localLuminanceDownScale);
                    luminanceTexture = NewScopedRenderTarget2D(lumSize.Width, lumSize.Height, PixelFormat.R16_Float, 1);

                    luminanceEffect.SetOutput(luminanceTexture);
                }

                luminanceEffect.EnableLocalLuminanceCalculation = toneMap.UseLocalLuminance;
                luminanceEffect.SetInput(currentInput);
                luminanceEffect.Draw(context);

                // Set this parameter that will be used by the tone mapping
                colorTransformsGroup.Parameters.Set(LuminanceEffect.LuminanceResult, new LuminanceResult(luminanceEffect.AverageLuminance, luminanceTexture));
            }

            if (BrightFilter.Enabled && (Bloom.Enabled || LightStreak.Enabled || LensFlare.Enabled))
            {
                Texture brightTexture = NewScopedRenderTarget2D(currentInput.Width, currentInput.Height, currentInput.Format, 1);
                // Bright filter pass

                BrightFilter.SetInput(currentInput);
                BrightFilter.SetOutput(brightTexture);
                BrightFilter.Draw(context);

                // Bloom pass
                if (Bloom.Enabled)
                {
                    Bloom.SetInput(brightTexture);
                    Bloom.SetOutput(currentInput);
                    Bloom.Draw(context);
                }

                // Light streak pass
                if (LightStreak.Enabled)
                {
                    LightStreak.SetInput(brightTexture);
                    LightStreak.SetOutput(currentInput);
                    LightStreak.Draw(context);
                }

                // Lens flare pass
                if (LensFlare.Enabled)
                {
                    LensFlare.SetInput(brightTexture);
                    LensFlare.SetOutput(currentInput);
                    LensFlare.Draw(context);
                }
            }

            bool aaLast = needAA && !aaFirst;
            var toneOutput = aaLast ? NewScopedRenderTarget2D(input.Width, input.Height, input.Format) : output;

            // When FXAA is enabled we need to detect whether the ColorTransformGroup should output the Luminance into the alpha or not
            var luminanceToChannelTransform = colorTransformsGroup.PostTransforms.Get<LuminanceToChannelTransform>();
            if (fxaa != null)
            {
                if (luminanceToChannelTransform == null)
                {
                    luminanceToChannelTransform = new LuminanceToChannelTransform { ColorChannel = ColorChannel.A };
                    colorTransformsGroup.PostTransforms.Add(luminanceToChannelTransform);
                }

                // Only enabled when FXAA is enabled and InputLuminanceInAlpha is true
                luminanceToChannelTransform.Enabled = fxaa.Enabled && fxaa.InputLuminanceInAlpha;
            }
            else if (luminanceToChannelTransform != null)
            {
                luminanceToChannelTransform.Enabled = false;
            }
            
            // Color transform group pass (tonemap, color grading)
            var lastEffect = colorTransformsGroup.Enabled ? (ImageEffect)colorTransformsGroup : Scaler;
            lastEffect.SetInput(currentInput);
            lastEffect.SetOutput(toneOutput);
            lastEffect.Draw(context);

            // do AA here, last, if not already done.
            if (aaLast)
            {
                Antialiasing.SetInput(toneOutput);
                Antialiasing.SetOutput(output);
                Antialiasing.Draw(context);
            }
        }
    }
}
