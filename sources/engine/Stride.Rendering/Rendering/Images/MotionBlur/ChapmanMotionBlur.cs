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

        public bool NeedRangeDecompress => false;

        public bool RequiresDepthBuffer => true;

        public bool RequiresVelocityBuffer => true;

        [DataAlias("Target FPS")]
        [DefaultValue(60)]
        public int TargetFPS { get; set; } = 60;

        
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
            SimpleBlur.Parameters.Set(ChapmanBlurShaderKeys.u_BlurRadius, Math.Min(15, (uint)(context.RenderContext.Time.FramePerSecond / TargetFPS)));
            SimpleBlur.SetOutput(outputBuffer); 
            SimpleBlur.Draw(context);

        }
    }
}
