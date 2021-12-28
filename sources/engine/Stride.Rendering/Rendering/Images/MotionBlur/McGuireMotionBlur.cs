using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Graphics;
using Stride.Rendering.Images;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.ComponentModel;

namespace Stride.Rendering.Rendering.Images.MotionBlur
{

    /// <summary>
    /// Motion blur effect using a technique defined in MotionBlurHPG14.
    /// </summary>
    [DataContract("MotionBlurHPG14")]
    public class McGuireMotionBlur : ImageEffect, IMotionBlur
    {
        private ImageEffectShader tileMaxPassShader;
        private ImageEffectShader neighborMaxPassShader;
        private ImageEffectShader blurPassShader;


        public bool RequiresDepthBuffer => true;

        public bool RequiresVelocityBuffer => true;

        /// <summary>
        /// The size of the square tile for the maximum speed pass.
        /// The bigger the more it will leave trails
        /// </summary>
        [DefaultValue(40)]
        [DataMemberRange(1, 50, 1, 1, 1)]
        public int TileSize { get; set; } = 40;

        /// <summary>
        /// The shutter angle defines how long a frame is exposed to light.
        /// Higher values gives a more pronounced blur.
        /// </summary>
        [DefaultValue(180)]
        [DataMemberRange(1, 360, 1, 1, 1)]
        public float ShutterAngle { get; set; } = 180;

        /// <summary>
        /// The minimum thresholds controls how much pixel velocity influences the max tile velocity.
        /// </summary>
        [DefaultValue(1.5f)]
        public float MinimumThreshold { get; set; } = 1.5f;

        /// <summary>
        /// Controls the maximum jitter that will be used during the blur sampling
        /// </summary>
        [DefaultValue(0.95f)]
        public float MaximumJitter { get; set; } = 0.95f;

        [DefaultValue(27)]
        public int JitterLevel { get; set; } = 27;

        [DefaultValue(40)]
        public int WeightBias { get; set; } = 40;

        /// <summary>
        /// The number of samples for the blurring.
        /// Lower values gives better performance but less accurate blurring.
        /// </summary>
        [DefaultValue(40)]
        public int SamplingRate { get; set; } = 40;
        
        /// <summary>
        /// The minimum velocity for the motion blur to be activated
        /// </summary>
        [DefaultValue(0.5)]
        public float MinimumVelocity { get; set; } = 0.5f;



        protected override void InitializeCore()
        { 
            base.InitializeCore();

            tileMaxPassShader = ToLoadAndUnload(new ImageEffectShader("TileMaxShader"));
            neighborMaxPassShader = ToLoadAndUnload(new ImageEffectShader("NeighborMaxShader"));
            blurPassShader = ToLoadAndUnload(new ImageEffectShader("MotionReconstructionShader"));
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            // Inputs:
            Texture colorBuffer = GetSafeInput(0);
            Texture velocityBuffer = GetSafeInput(1);
            Texture depthBuffer = GetSafeInput(2);

            // Output:
            Texture outputBuffer = GetSafeOutput(0);

            // Compute radius needed
            float resolution = colorBuffer.Width / colorBuffer.Height;


            // TileMax pass 
            var tileMax = NewScopedRenderTarget2D(colorBuffer.Width/TileSize, colorBuffer.Height/TileSize, velocityBuffer.Format);


            tileMaxPassShader.Parameters.Set(TileMaxShaderKeys.u_TileSize, TileSize); 
            tileMaxPassShader.SetInput(velocityBuffer);
            tileMaxPassShader.SetOutput(tileMax);
            tileMaxPassShader.Draw(context);



            //// MaxVelocity pass 
            var maxVelocity = NewScopedRenderTarget2D(tileMax.Width, tileMax.Height, velocityBuffer.Format);

            neighborMaxPassShader.SetInput(0, tileMax);
            neighborMaxPassShader.SetOutput(maxVelocity);
            neighborMaxPassShader.Draw(context);

            // Reconstruction pass 
            //var blurTexture = NewScopedRenderTarget2D(colorBuffer.Width, colorBuffer.Height, colorBuffer.Format);

            blurPassShader.SetInput(0, colorBuffer);
            blurPassShader.SetInput(1, velocityBuffer);
            blurPassShader.SetInput(2, depthBuffer);
            blurPassShader.SetInput(3, maxVelocity);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_MinimumThreshold, MinimumThreshold);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_MaximumJitter, MaximumJitter);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_JitterLevel, JitterLevel);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_SamplingRate, SamplingRate);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_TileSize, TileSize);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_MotionBlurScale, (float)context.RenderContext.Time.FramePerSecond * ShutterAngle / 360f);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_MinimumVelocity, MinimumVelocity);
            blurPassShader.Parameters.Set(MotionReconstructionShaderKeys.u_WeightBias, WeightBias);
            blurPassShader.SetOutput(outputBuffer);
            blurPassShader.Draw(context);

        }
    }
}
