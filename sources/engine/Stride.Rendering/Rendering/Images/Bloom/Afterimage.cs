// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Afterimage simulates the persistence of the bright areas on the retina. 
    /// </summary>
    [DataContract("Afterimage")]
    public class Afterimage : ImageEffect
    {
        private readonly ImageEffectShader bloomAfterimageShader;
        private readonly ImageEffectShader bloomAfterimageCombineShader;

        private Texture persistenceTexture;

        /// <summary>
        /// Initializes a new instance of the <see cref="Afterimage"/> class.
        /// </summary>
        public Afterimage()
        {
            bloomAfterimageShader = new ImageEffectShader("BloomAfterimageShader");
            bloomAfterimageCombineShader = new ImageEffectShader("BloomAfterimageCombineShader");
            FadeOutSpeed = 0.9f;
            Sensitivity = 0.1f;
        }

        /// <summary>
        /// How fast the persistent image fades out. 
        /// </summary>
        /// <userdoc>The factor specifying how much the persistence decreases at each frame (1 means infinite persistence, while 0 means no persistence at all)</userdoc>
        [DataMember(10)]
        [DefaultValue(0.9f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f, 3)]
        public float FadeOutSpeed { get; set; }

        /// <summary>
        /// How sensitive we are to the bright light.
        /// </summary>
        /// <userdoc>The sensitiveness of the retina to bright light. This affects the time needed to produce persistence effect.</userdoc>
        [DataMember(20)]
        [DefaultValue(0.1f)]
        public float Sensitivity { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            ToLoadAndUnload(bloomAfterimageShader);
            ToLoadAndUnload(bloomAfterimageCombineShader);
        }

        protected override void Destroy()
        {
            if (persistenceTexture != null) Context.Allocator.ReleaseReference(persistenceTexture);
            base.Destroy();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);

            if (FadeOutSpeed == 0f)
            {
                // Nothing to do
                if (input != output)
                {
                    context.CommandList.Copy(input, output);
                }
                return;
            }

            if (input == output)
            {
                var newInput = NewScopedRenderTarget2D(input.Description);
                context.CommandList.Copy(input, newInput);
                input = newInput;
            }

            // Check we have a render target to hold the persistence over a few frames
            if (persistenceTexture == null || persistenceTexture.Description != output.Description)
            {
                // We need to re-allocate the texture
                if (persistenceTexture != null)
                {
                    Context.Allocator.ReleaseReference(persistenceTexture);
                }

                persistenceTexture = Context.Allocator.GetTemporaryTexture2D(output.Description);
                // Initializes to black
                context.CommandList.Clear(persistenceTexture, Color.Black);
            }

            var accumulationPersistence = NewScopedRenderTarget2D(persistenceTexture.Description);

            // For persistence, we combine the current brightness with the one of the previous frames.
            bloomAfterimageShader.Parameters.Set(BloomAfterimageShaderKeys.FadeOutSpeed, FadeOutSpeed);
            bloomAfterimageShader.Parameters.Set(BloomAfterimageShaderKeys.Sensitivity, Sensitivity / 100f);
            bloomAfterimageShader.SetInput(0, input);
            bloomAfterimageShader.SetInput(1, persistenceTexture);
            bloomAfterimageShader.SetOutput(accumulationPersistence);
            bloomAfterimageShader.Draw(context, "Afterimage persistence accumulation");

            // Keep the final brightness buffer for the following frames
            context.CommandList.Copy(accumulationPersistence, persistenceTexture);

            // Merge persistence and current bloom into the final result
            bloomAfterimageCombineShader.SetInput(0, input);
            bloomAfterimageCombineShader.SetInput(1, persistenceTexture);
            bloomAfterimageCombineShader.SetOutput(output);
            bloomAfterimageCombineShader.Draw(context, "Afterimage persistence combine");
        }
    }
}
