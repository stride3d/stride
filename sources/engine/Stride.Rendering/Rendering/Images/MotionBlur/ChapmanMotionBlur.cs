using System;
using Stride.Graphics;
using Stride.Rendering.Images;
using Stride.Core;
using Stride.Core.Annotations;
using System.ComponentModel;

namespace Stride.Rendering.Rendering.Images.MotionBlur
{

    /// <summary>
    /// Simple motion blur effect.
    /// </summary>
    [DataContract("ChapmanMotionBlur")]
    public class ChapmanMotionBlur : ImageEffect, IMotionBlur
    {
        private ImageEffectShader SimpleBlur;

        public bool RequiresDepthBuffer => true;

        public bool RequiresVelocityBuffer => true;

        
        /// <summary>
        /// The shutter angle defines how long a frame is exposed to light.
        /// Higher values gives a more pronounced blur
        /// </summary>
        [DefaultValue(60)]
        [DataMemberRange(1, 360, 1, 1, 1)]
        public float ShutterAngle { get; set; } = 60;

        /// <summary>
        /// Max number of samples for the blurring.
        /// Lower values gives better performance but less accurate blurring.
        /// </summary>
        [DefaultValue(20)]
        [DataMemberRange(1, 50, 1, 1, 1)]
        public uint MaxSamples { get; set; } = 20;



        protected override void InitializeCore()
        {
            base.InitializeCore(); 
            
            SimpleBlur = ToLoadAndUnload(new ImageEffectShader("ChapmanBlurShader"));
            
        }

        protected override void DrawCore(RenderDrawContext context)
        { 
            // Inputs:
            Texture colorBuffer = GetSafeInput(0);
            Texture velocityBuffer = GetSafeInput(1);
            Texture depthBuffer = GetSafeInput(1);

            // Output:
            Texture outputBuffer = GetSafeOutput(0);

            

            SimpleBlur.SetInput(0, colorBuffer);
            SimpleBlur.SetInput(1, velocityBuffer);
            SimpleBlur.SetInput(2, depthBuffer);
            SimpleBlur.Parameters.Set(ChapmanBlurShaderKeys.u_MaxSamples, MaxSamples);
            SimpleBlur.Parameters.Set(ChapmanBlurShaderKeys.u_BlurRadius, Math.Min(60, (uint)(context.RenderContext.Time.FramePerSecond * ShutterAngle / 360)));
            SimpleBlur.SetOutput(outputBuffer); 
            SimpleBlur.Draw(context);

        }
    }
}
