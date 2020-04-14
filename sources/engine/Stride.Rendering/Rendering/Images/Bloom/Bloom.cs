// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Images
{
    [DataContract("Bloom")]
    public class Bloom : ImageEffect
    {
        private GaussianBlur blur;

        private ImageMultiScaler multiScaler;

        private Vector2 distortion;

        private bool stableConvolution;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bloom"/> class.
        /// </summary>
        public Bloom()
            : base(null, true)
        {
            Radius = 10;
            Amount = 0.3f;
            DownScale = 2;
            SigmaRatio = 3.5f;
            Distortion = new Vector2(1);
            Afterimage = new Afterimage { Enabled = false };
            EnableSetRenderTargets = false;
            stableConvolution = true;
        }

        /// <summary>
        /// Radius of the bloom.
        /// </summary>
        /// <userdoc>The range of the bloom effect around bright regions. Note that high values can affect performance</userdoc>
        [DataMember(10)]
        [DefaultValue(10)]
        [DataMemberRange(1.0, 100.0, 1.0, 10.0, 1)]
        public float Radius { get; set; }

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        /// <value>The amount.</value>
        /// <userdoc>The strength of the bloom effect</userdoc>
        [DataMember(20)]
        [DefaultValue(0.3f)]
        public float Amount { get; set; }

        /// <summary>
        /// Gets or sets the sigma ratio.
        /// </summary>
        /// <value>The ratio</value>
        /// <userdoc>The standard deviation used for the blur of the effect. This impact the fall-off of the bloom.</userdoc>
        [Display("Sigma ratio")]
        [DataMember(30)]
        [DefaultValue(3.5f)]
        public float SigmaRatio { get; set; }

        /// <summary>
        /// Vertical or horizontal distortion to apply.
        /// (1, 2) means the bloom will be stretched twice longer horizontally than vertically.
        /// </summary>
        /// <userdoc>Apply vertical of horizontal distortion on the effect</userdoc>
        [DataMember(40)]
        public Vector2 Distortion
        {
            get => distortion;
            set
            {
                distortion = value;
                if (distortion.X < 1f) distortion.X = 1f;
                if (distortion.Y < 1f) distortion.Y = 1f;
            }
        }

        /// <summary>
        /// Gets the afterimage effect/>
        /// </summary>
        /// <userdoc>Simulate persistence effects of the light points (trails) on the next frames</userdoc>
        [DataMember(50)]
        public Afterimage Afterimage { get; private set; }

        /// <summary>
        /// Use the "stable bloom" rendering path.
        /// </summary>
        /// <userdoc>Reverse FXAA and bloom and use a richer convolution kernel during blurring, reducing temporal shimmering</userdoc>
        [DataMember(60)]
        [DefaultValue(true)]
        [Display("Expanded filtering")]
        public bool StableConvolution
        {
            get => stableConvolution;
            set
            {
                var old = stableConvolution;
                stableConvolution = value;
                if (value != old)
                {   
                    multiScaler?.Dispose();
                    multiScaler = null;
                    if (Context != null)
                        multiScaler = ToLoadAndUnload(new ImageMultiScaler(stableConvolution));
                }
            }
        }

        [DataMemberIgnore]
        public bool ShowOnlyBloom { get; set; }

        [DataMemberIgnore]
        public bool ShowOnlyMip { get; set; }

        [DataMemberIgnore]
        public int MipIndex { get; set; }

        [DataMemberIgnore]
        public int DownScale { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            multiScaler = ToLoadAndUnload(new ImageMultiScaler(StableConvolution));
            blur = ToLoadAndUnload(new GaussianBlur());
            Afterimage = ToLoadAndUnload(Afterimage);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0) ?? input;

            if (input == null)
            {
                return;
            }

            // If afterimage is active, add some persistence to the brightness
            if (Afterimage.Enabled)
            {
                var persistenceBrightness = NewScopedRenderTarget2D(input.Description);
                Afterimage.SetInput(0, input);
                Afterimage.SetOutput(persistenceBrightness);
                Afterimage.Draw(context);
                input = persistenceBrightness;
            }

            // A distortion can be applied to the bloom effect to simulate anamorphic lenses
            if (Distortion.X > 1f || Distortion.Y > 1f)
            {
                int distortedWidth = (int)Math.Max(1, input.Description.Width / Distortion.X);
                int distortedHeight = (int)Math.Max(1, input.Description.Height / Distortion.Y);
                var anamorphicInput = NewScopedRenderTarget2D(distortedWidth, distortedHeight, input.Format);
                Scaler.SetInput(input);
                Scaler.SetOutput(anamorphicInput);
                Scaler.Draw(context, "Anamorphic distortion");
                input = anamorphicInput;
            }

            // ----------------------------------------
            // Downscale
            // ----------------------------------------
            var nextSize = input.Size.Down2(DownScale);
            var blurTexture = NewScopedRenderTarget2D(nextSize.Width, nextSize.Height, input.Format);
            if (DownScale > 0)
            {
                multiScaler.SetInput(input);
                multiScaler.SetOutput(blurTexture);
                multiScaler.Draw(context);

                blur.SetInput(blurTexture);
            }
            else
            {
                blur.SetInput(input);
            }

            // Max blur size no more than 1/4 of input size
            var inputMaxBlurRadiusInPixels = 0.25 * Math.Max(input.Width, input.Height) * Math.Pow(2, -DownScale);
            blur.Radius = Math.Max(1, (int)MathUtil.Lerp(1, inputMaxBlurRadiusInPixels, Math.Max(0, Radius / 100.0f)));
            blur.SigmaRatio = Math.Max(1.0f, SigmaRatio);
            blur.SetOutput(blurTexture);
            blur.Draw(context);

            // Copy the input texture to the output
            if (ShowOnlyMip || ShowOnlyBloom)
            {
                context.CommandList.Clear(output, Color.Black);
            }

            // Switch to additive
            Scaler.BlendState = BlendStates.Additive;

            Scaler.Color = new Color4(Amount);
            Scaler.SetInput(blurTexture);
            Scaler.SetOutput(output);
            Scaler.Draw(context);
            Scaler.Reset();

            Scaler.BlendState = BlendStates.Default;
        }
    }
}
