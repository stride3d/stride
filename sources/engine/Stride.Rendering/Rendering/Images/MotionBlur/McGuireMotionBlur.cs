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
    /// Motion blur effect using GUERTIN2014 Motion blur.
    /// </summary>
    [DataContract("McGuireMotionBlur")]
    public class McGuireMotionBlur : ImageEffect, IMotionBlur
    {
        private ImageEffectShader tileMaxPassShader;
        private ImageEffectShader neighborMaxPassShader;
        private ImageEffectShader blurPassShader;
        private ImageEffectShader resize;

        public bool NeedRangeDecompress => false;

        public bool RequiresDepthBuffer => true;

        public bool RequiresVelocityBuffer => true;

        //[DefaultValue(20)]
        //public float ShutterSpeed { get; set; } = 20;

        //[DefaultValue(4)]
        //public uint MaxSamples { get; set; } = 4;

        [DefaultValue(4)]
        public int TileSize { get; set; } = 4;

        [DefaultValue(1)]
        public float MinimumThreshold { get; set; } = 1;

        [DefaultValue(1)]
        public float MaximumJitter { get; set; } = 1;

        [DefaultValue(1)]
        public int JitterLevel { get; set; } = 1;

        [DefaultValue(40)]
        public int WeightBias { get; set; } = 40;

        [DefaultValue(40)]
        public int SamplingRate { get; set; } = 40;

        

        protected override void InitializeCore()
        { 
            base.InitializeCore();

            tileMaxPassShader = ToLoadAndUnload(new ImageEffectShader("TileMaxShader"));
            resize = ToLoadAndUnload(new ImageEffectShader("ResizeTexture"));
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
            blurPassShader.SetOutput(outputBuffer);
            blurPassShader.Draw(context);

            // return output (maybe not needed since we draw on it right bf

            //resize.SetInput(maxVelocity);
            //resize.SetOutput(outputBuffer);
            //resize.Draw(context);

            //SetOutput(tileMax);
        }
    }
}
