// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect.LambertianPrefiltering;
using Stride.Rendering.Images.SphericalHarmonics;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestLambertPrefilteringSH : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;

        private RenderContext drawEffectContext;

        private Texture inputCubemap;

        private Texture displayedCubemap;

        private LambertianPrefilteringSH lamberFilter;
        private LambertianPrefilteringSHNoCompute lamberFilterNoCompute;
        private SphericalHarmonicsRendererEffect renderSHEffect;

        private Texture outputCubemap;

        private bool shouldPrefilter = true;

        private bool useComputeShader;

        private Int2 screenSize = new Int2(768, 1024);

        private EffectInstance cubemapSpriteEffect;

        public TestLambertPrefilteringSH()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = screenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = screenSize.Y;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            cubemapSpriteEffect = new EffectInstance(EffectSystem.LoadEffect("CubemapSprite").WaitForResult());

            drawEffectContext = RenderContext.GetShared(Services);
            lamberFilter = new LambertianPrefilteringSH(drawEffectContext);
            lamberFilterNoCompute = new LambertianPrefilteringSHNoCompute(drawEffectContext);
            renderSHEffect = new SphericalHarmonicsRendererEffect();
            renderSHEffect.Initialize(drawEffectContext);

            spriteBatch = new SpriteBatch(GraphicsDevice);
            inputCubemap = Content.Load<Texture>("CubeMap");
            outputCubemap = Texture.NewCube(GraphicsDevice, 256, 1, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource).DisposeBy(this);
            displayedCubemap = outputCubemap;
        }

        private void PrefilterCubeMap(RenderDrawContext context)
        {
            if (!shouldPrefilter)
                return;

            if (useComputeShader)
            { 
                lamberFilter.HarmonicOrder = 5;
                lamberFilter.RadianceMap = inputCubemap;
                lamberFilter.Draw(context);
                renderSHEffect.InputSH = lamberFilter.PrefilteredLambertianSH;
            }
            else
            {
                lamberFilterNoCompute.HarmonicOrder = 5;
                lamberFilterNoCompute.RadianceMap = inputCubemap;
                lamberFilterNoCompute.Draw(context);
                renderSHEffect.InputSH = lamberFilterNoCompute.PrefilteredLambertianSH;
            }

            renderSHEffect.SetOutput(outputCubemap);
            renderSHEffect.Draw(context);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        private void RenderCubeMap(RenderDrawContext context)
        {
            if (displayedCubemap == null || spriteBatch == null)
                return;

            var size = new Vector2(screenSize.X / 3f, screenSize.Y / 4f);

            context.CommandList.SetRenderTargetAndViewport(null, GraphicsDevice.Presenter.BackBuffer);
            context.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Green);

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 1);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(0, size.Y, size.X, size.Y), Color.White);
            spriteBatch.End();

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 2);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(size.X, 0f, size.X, size.Y), Color.White);
            spriteBatch.End();

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 4);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(size.X, size.Y, size.X, size.Y), Color.White);
            spriteBatch.End();

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 3);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(size.X, 2f * size.Y, size.X, size.Y), Color.White);
            spriteBatch.End();

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 5);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(size.X, 3f * size.Y, size.X, size.Y), null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically);
            spriteBatch.End();

            cubemapSpriteEffect.Parameters.Set(CubemapSpriteKeys.ViewIndex, 0);
            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, cubemapSpriteEffect);
            spriteBatch.Draw(displayedCubemap, new RectangleF(2f * size.X, size.Y, size.X, size.Y), Color.White);
            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.Space))
                shouldPrefilter = !shouldPrefilter;

            if (Input.IsKeyPressed(Keys.C))
                useComputeShader = !useComputeShader;

            if (Input.IsKeyPressed(Keys.I))
                displayedCubemap = inputCubemap;

            if (Input.IsKeyPressed(Keys.O))
                displayedCubemap = outputCubemap;

            if (Input.IsKeyPressed(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "LambertianPrefilteredImageCross.png");
        }

        protected override void Draw(GameTime gameTime)
        {
            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);
            PrefilterCubeMap(renderDrawContext);
            RenderCubeMap(renderDrawContext);

            base.Draw(gameTime);
        }

        [SkippableFact]
        public void RunTestPass2()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);

            RunGameTest(new TestLambertPrefilteringSH());
        }
    }
}
