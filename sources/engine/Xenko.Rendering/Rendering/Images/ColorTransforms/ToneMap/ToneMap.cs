// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// A tonemap effect.
    /// </summary>
    [DataContract("ToneMap")]
    public class ToneMap : ColorTransform
    {
        private float previousLuminance;

        private readonly Stopwatch timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMap"/> class.
        /// </summary>
        public ToneMap() : this("ToneMapEffect")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMap" /> class.
        /// </summary>
        /// <param name="toneMapEffect">The tone map shader effect (default is <c>ToneMapEffect)</c>.</param>
        /// <exception cref="System.ArgumentNullException">toneMapEffect</exception>
        public ToneMap(string toneMapEffect) : base(toneMapEffect)
        {
            timer = new Stopwatch();
            AutoKeyValue = true;
            Operator = new ToneMapHejl2Operator();
            AdaptationRate = 1.0f;
            TemporalAdaptation = true;
            AutoExposure = true;
        }

        /// <summary>
        /// Gets or sets the operator used for tonemap.
        /// </summary>
        /// <value>The operator.</value>
        /// <userdoc>The method used to perform the HDR to LDR tone mapping</userdoc>
        [DataMember(10)]
        [NotNull]
        public ToneMapOperator Operator
        {
            get => Parameters.Get(ToneMapKeys.Operator);
            set
            {
                Parameters.Set(ToneMapKeys.Operator, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tonemap key is automatically calculated based on common perceptive behavior.
        /// </summary>
        /// <value><c>true</c> if [automatic key value]; otherwise, <c>false</c>.</value>
        [DataMember(15)]
        [DefaultValue(true)]
        public bool AutoKeyValue
        {
            get => Parameters.Get(ToneMapKeys.AutoKey);
            set
            {
                Parameters.Set(ToneMapKeys.AutoKey, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets or sets the key value.
        /// </summary>
        /// <value>The key value.</value>
        [DataMember(20)]
        [DefaultValue(0.18f)]
        public float KeyValue { get; set; } = 0.18f;

        /// <summary>
        /// Gets or sets a value indicating whether the tonemap is calculating the exposure based on the average luminance of the image else <see cref="Exposure"/> is used.
        /// </summary>
        /// <value><c>true</c> if the tonemap is calculating the exposure based on the average luminance of the image; otherwise, <c>false</c>.</value>
        [DataMember(30)]
        [DefaultValue(true)]
        public bool AutoExposure
        {
            get => Parameters.Get(ToneMapKeys.AutoExposure);
            set
            {
                Parameters.Set(ToneMapKeys.AutoExposure, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets or sets the manual exposure value if <see cref="AutoExposure"/> is <c>false</c>.
        /// </summary>
        /// <value>The exposure value.</value>
        [DataMember(32)]
        [DefaultValue(0.0f)]
        public float Exposure { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to update the luminance progressively based on the current time.
        /// </summary>
        /// <value><c>true</c> the luminance is updated progressively based on the current time; otherwise, <c>false</c>.</value>
        [DataMember(35)]
        [DefaultValue(true)]
        [Display("Temporal adaptation?")]
        public bool TemporalAdaptation { get; set; }

        /// <summary>
        /// Gets or sets the adaptation rate.
        /// </summary>
        /// <value>The adaptation rate.</value>
        [DataMember(40)]
        [DefaultValue(1.0f)]
        public float AdaptationRate { get; set; }

        /// <summary>
        /// Indicates if the luminance in the neighborhood of a pixel is used in addition to the overall luminance of the input.
        /// </summary>
        [DataMember(45)]
        [DefaultValue(false)]
        public bool UseLocalLuminance
        {
            get => Parameters.Get(ToneMapKeys.UseLocalLuminance);
            set
            {
                Parameters.Set(ToneMapKeys.UseLocalLuminance, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets or sets the luminance local factor. 0.0: No local influence, only global influence, 1.0: No global influence, Only local influence.
        /// </summary>
        /// <value>The luminance local factor.</value>
        [DataMember(50)]
        [DefaultValue(0.0f)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public float LuminanceLocalFactor
        {
            get => Parameters.Get(ToneMapShaderKeys.LuminanceLocalFactor);
            set => Parameters.Set(ToneMapShaderKeys.LuminanceLocalFactor, value);
        }

        /// <summary>
        /// Gets or sets the contrast.
        /// </summary>
        /// <value>The contrast.</value>
        [DataMember(60)]
        [DefaultValue(0f)]
        public float Contrast
        {
            get => Parameters.Get(ToneMapShaderKeys.Contrast);
            set => Parameters.Set(ToneMapShaderKeys.Contrast, value);
        }

        /// <summary>
        /// Gets or sets the brightness.
        /// </summary>
        /// <value>The brightness.</value>
        [DataMember(70)]
        [DefaultValue(0f)]
        public float Brightness
        {
            get => Parameters.Get(ToneMapShaderKeys.Brightness);
            set => Parameters.Set(ToneMapShaderKeys.Brightness, value);
        }

        public override void PrepareParameters(ColorTransformContext context, ParameterCollection parentCollection, string keyRoot)
        {
            base.PrepareParameters(context, parentCollection, keyRoot);

            Operator.PrepareParameters(context, Parameters, ".ToneMapOperator");
        }

        public override void UpdateParameters(ColorTransformContext context)
        {
            if (Operator == null)
            {
                throw new InvalidOperationException("Operator cannot be null on this instance");
            }

            // Update the luminance
            var elapsedTime = timer.Elapsed;
            timer.Restart();

            var luminanceResult = context.SharedParameters.Get(LuminanceEffect.LuminanceResult);

            // Get the average luminance
            float adaptedLum = luminanceResult.AverageLuminance;
            if (TemporalAdaptation)
            {
                // Get adapted luminance
                // From "Perceptual effects in real-time tone mapping" by Grzegorz Krawczyk, Karol Myszkowski, Hans-Peter Seidel, p. 3, Equation 5
                adaptedLum = (float)(previousLuminance + (luminanceResult.AverageLuminance - previousLuminance) * (1.0 - Math.Exp(-elapsedTime.TotalSeconds * AdaptationRate)));
                previousLuminance = adaptedLum;
            }

            var keyValue = KeyValue;
            if (AutoKeyValue)
            {
                // From "Perceptual effects in real-time tone mapping" by Grzegorz Krawczyk, Karol Myszkowski, Hans-Peter Seidel, p. 4, Equation 11
                keyValue = 1.03f - (2.0f / (2.0f + (float)Math.Log10(adaptedLum + 1)));
            }

            // Setup parameters
            Parameters.Set(ToneMapShaderKeys.LuminanceTexture, luminanceResult.LocalTexture);
            Parameters.Set(ToneMapShaderKeys.LuminanceAverageGlobal, (float)Math.Log(adaptedLum, 2));
            Parameters.Set(ToneMapShaderKeys.Exposure, (float)Math.Pow(2.0, Exposure));
            Parameters.Set(ToneMapShaderKeys.KeyValue, keyValue);

            // Update operator parameters
            Operator.UpdateParameters(context);

            // Copy parameters to parent
            base.UpdateParameters(context);
        }
    }
}
