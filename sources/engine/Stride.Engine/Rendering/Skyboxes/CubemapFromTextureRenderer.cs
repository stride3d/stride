using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Skyboxes
{
    public class CubemapFromTextureRenderer : CubemapRendererBase
    {
        private DynamicEffectInstance skyboxEffect;

        private SpriteBatch spriteBatch;

        private Texture inputTexture;

        public CubemapFromTextureRenderer(IServiceRegistry services, RenderDrawContext renderDrawContext, Texture input, int outputSize, PixelFormat outputFormat) 
            : base(renderDrawContext.GraphicsDevice, outputSize, outputFormat, false)
        {
            inputTexture = input;

            DrawContext = renderDrawContext;

            skyboxEffect = new DynamicEffectInstance("SkyboxShaderTexture");
            skyboxEffect.Initialize(services);

            spriteBatch = new SpriteBatch(renderDrawContext.GraphicsDevice) { VirtualResolution = new Vector3(1) };
        }

        protected override void DrawImpl()
        {
            skyboxEffect.UpdateEffect(DrawContext.GraphicsDevice);
            spriteBatch.Begin(DrawContext.GraphicsContext, depthStencilState: DepthStencilStates.None, effect: skyboxEffect);
            spriteBatch.Parameters.Set(SkyboxShaderTextureKeys.Texture, inputTexture);
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.Intensity, 1);
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.ViewInverse, Matrix.Invert(Camera.ViewMatrix));
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.ProjectionInverse, Matrix.Invert(Camera.ProjectionMatrix));
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.SkyMatrix, Matrix.Identity);
            spriteBatch.Draw(inputTexture, new RectangleF(0, 0, 1, 1), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();
        }

        public static Texture GenerateCubemap(IServiceRegistry services, RenderDrawContext renderDrawContext, Texture input, int outputSize)
        {
            var pixelFormat = input.Format.IsHDR() ? PixelFormat.R16G16B16A16_Float : input.Format.IsSRgb() ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm;
            return GenerateCubemap(new CubemapFromTextureRenderer(services, renderDrawContext, input, outputSize, pixelFormat), Vector3.Zero);
        }
    }
}
