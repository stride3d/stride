// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Luminance effect.
    /// </summary>
    public class LuminanceEffect : ImageEffect
    {
        public static readonly ObjectParameterKey<LuminanceResult> LuminanceResult = ParameterKeys.NewObject<LuminanceResult>();

        private PixelFormat luminanceFormat = PixelFormat.R16_Float;
        private GaussianBlur blur;

        private ImageMultiScaler multiScaler;
        private ImageReadback<Half> readback;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuminanceEffect" /> class.
        /// </summary>
        public LuminanceEffect()
        {
            LuminanceFormat = PixelFormat.R16_Float;
            DownscaleCount = 6;
            UpscaleCount = 4;
            EnableAverageLuminanceReadback = true;
            readback = new ImageReadback<Half>();
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            LuminanceLogEffect = ToLoadAndUnload(new LuminanceLogEffect());

            // Create 1x1 texture
            AverageLuminanceTexture = Texture.New2D(GraphicsDevice, 1, 1, 1, luminanceFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            // Use a multiscaler
            multiScaler = ToLoadAndUnload(new ImageMultiScaler());

            // Readback is always going to be done on the 1x1 texture
            readback = ToLoadAndUnload(readback);

            // Blur used before upscaling 
            blur = ToLoadAndUnload(new GaussianBlur());
            blur.Radius = 4;
        }

        /// <summary>
        /// Luminance texture format.
        /// </summary>
        public PixelFormat LuminanceFormat
        {
            get => luminanceFormat;
            set
            {
                if (value.IsCompressed() || value.IsPacked() || value.IsTypeless() || value == PixelFormat.None)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Unsupported format [{0}] (must be not none, compressed, packed or typeless)".ToFormat(value));
                }
                luminanceFormat = value;
            }
        }

        /// <summary>
        /// Luminance log effect.
        /// </summary>
        public ImageEffectShader LuminanceLogEffect { get; set; }

        /// <summary>
        /// Gets or sets down scale count used to downscale the input intermediate texture used for local luminance (if no 
        /// output is given). By default 1/64 of the input texture size.
        /// </summary>
        /// <value>Down scale count.</value>
        public int DownscaleCount { get; set; }

        /// <summary>
        /// Gets or sets the upscale count used to upscale the downscaled input local luminance texture. By default x16 of the 
        /// input texture size.
        /// </summary>
        /// <value>The upscale count.</value>
        public int UpscaleCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable calculation of <see cref="AverageLuminance"/> (default is true).
        /// </summary>
        /// <value><c>true</c> if to enable calculation of <see cref="AverageLuminance"/>; otherwise, <c>false</c>.</value>
        public bool EnableAverageLuminanceReadback { get; set; }

        /// <summary>
        /// Gets the average luminance calculated on the GPU. See remarks.
        /// </summary>
        /// <value>The average luminance.</value>
        /// <remarks>
        /// The average luminance is calculated on the GPU and readback with a few frames of delay, depending on the number of 
        /// frames in advance between command scheduling and actual execution on GPU.
        /// </remarks>
        public float AverageLuminance { get; private set; }

        /// <summary>
        /// Gets the average luminance 1x1 texture available after drawing this effect.
        /// </summary>
        /// <value>The average luminance texture.</value>
        public Texture AverageLuminanceTexture { get; private set; }

        /// <summary>
        /// Indicated if the local luminance should be rendered to the output texture.
        /// </summary>
        public bool EnableLocalLuminanceCalculation { get; set; }

        public override void Reset()
        {
            readback.Reset();

            base.Reset();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetSafeInput(0);

            // Render the luminance to a power-of-two target, so we preserve energy on downscaling
            var startWidth = Math.Max(1, Math.Min(MathUtil.NextPowerOfTwo(input.Size.Width), MathUtil.NextPowerOfTwo(input.Size.Height)) / 2);
            var startSize = new Size3(startWidth, startWidth, 1);
            var blurTextureSize = startSize.Down2(UpscaleCount);

            // If we don't need a blur pass, or no local luminance output at all, don't allocate a blur target
            Texture blurTexture = null;
            if (EnableLocalLuminanceCalculation && blurTextureSize.Width != 1 && blurTextureSize.Height != 1)
            {
                blurTexture = NewScopedRenderTarget2D(blurTextureSize.Width, blurTextureSize.Height, luminanceFormat, 1);
            }

            var luminanceMap = NewScopedRenderTarget2D(startSize.Width, startSize.Height, luminanceFormat, 1);

            // Calculate the first luminance map
            LuminanceLogEffect.SetInput(input);
            LuminanceLogEffect.SetOutput(luminanceMap);
            LuminanceLogEffect.Draw(context);

            // Downscales luminance up to BlurTexture (optional) and 1x1
            multiScaler.SetInput(luminanceMap);
            if (blurTexture == null)
            {
                multiScaler.SetOutput(AverageLuminanceTexture);
                multiScaler.Draw(context);

                if (EnableLocalLuminanceCalculation)
                {
                    var output = GetSafeOutput(0);

                    // TODO: Workaround to that the output filled with 1x1
                    Scaler.SetInput(AverageLuminanceTexture);
                    Scaler.SetOutput(output);
                    Scaler.Draw(context);
                }
            }
            else
            {
                multiScaler.SetOutput(blurTexture, AverageLuminanceTexture);
                multiScaler.Draw(context);

                // Blur x2 the intermediate output texture 
                blur.SetInput(blurTexture);
                blur.SetOutput(blurTexture);
                blur.Draw(context);
                blur.Draw(context);

                var output = GetSafeOutput(0);

                // Upscale from intermediate to output
                multiScaler.SetInput(blurTexture);
                multiScaler.SetOutput(output);
                multiScaler.Draw(context);
            }

            // Calculate average luminance only if needed
            if (EnableAverageLuminanceReadback)
            {
                readback.SetInput(AverageLuminanceTexture);
                readback.Draw(context);
                var rawLogValue = readback.Result[0];
                AverageLuminance = (float)Math.Pow(2.0, rawLogValue);

                // In case AvergaeLuminance go crazy because of halp float/infinity precision, some code to save the values here:
                //if (float.IsInfinity(AverageLuminance))
                //{
                //    using (var stream = new FileStream("luminance_input.dds", FileMode.Create, FileAccess.Write))
                //    {
                //        input.Save(stream, ImageFileType.Dds);
                //    }
                //    using (var stream = new FileStream("luminance.dds", FileMode.Create, FileAccess.Write))
                //    {
                //        luminanceMap.Save(stream, ImageFileType.Dds);
                //    }
                //}
            }
        }
    }
}
