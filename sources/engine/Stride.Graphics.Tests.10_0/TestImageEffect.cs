// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;
using Stride.Core.Diagnostics;
using Stride.Rendering;
using Stride.Rendering.Images;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestImageEffect : GraphicTestGameBase
    {
        private RenderContext drawEffectContext;

        private Texture hdrTexture;
        private Texture hdrRenderTexture;

        private PostProcessingEffects postProcessingEffects;

        public TestImageEffect()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = 760;
            GraphicsDeviceManager.PreferredBackBufferHeight = 1016;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            hdrTexture = Content.Load<Texture>("HdrTexture");
            hdrRenderTexture = Texture.New2D(GraphicsDevice, hdrTexture.Width, hdrTexture.Height, 1, hdrTexture.Description.Format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            drawEffectContext = RenderContext.GetShared(Services);
            postProcessingEffects = new PostProcessingEffects(drawEffectContext);
            postProcessingEffects.BrightFilter.Threshold = 20.0f;
            postProcessingEffects.Bloom.DownScale = 2;
            postProcessingEffects.Bloom.Enabled = true;
            postProcessingEffects.Bloom.ShowOnlyBloom = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!ScreenShotAutomationEnabled)
                AdjustEffectParameters();

            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);
            DrawCustomEffect(renderDrawContext);

            base.Draw(gameTime);
        }

        private void AdjustEffectParameters()
        {
            if (Input.IsKeyDown(Keys.Left))
            {
                postProcessingEffects.BrightFilter.Threshold -= 2.0f;
                Log.Info($"BrightFilter Threshold: {postProcessingEffects.BrightFilter.Threshold}");
            }
            else if (Input.IsKeyDown(Keys.Right))
            {
                postProcessingEffects.BrightFilter.Threshold += 2.0f;
                Log.Info($"BrightFilter Threshold: {postProcessingEffects.BrightFilter.Threshold}");
            }

            postProcessingEffects.Bloom.Enabled = !Input.IsKeyDown(Keys.Space);
            postProcessingEffects.Bloom.ShowOnlyBloom = !Input.IsKeyDown(Keys.B);
            if (Input.IsKeyDown(Keys.Down))
            {
                postProcessingEffects.Bloom.Amount += -0.01f;
                Log.Info($"Bloom Amount: {postProcessingEffects.Bloom.Amount}");
            }
            else if (Input.IsKeyDown(Keys.Up))
            {
                postProcessingEffects.Bloom.Amount += +0.01f;
                Log.Info($"Bloom Amount: {postProcessingEffects.Bloom.Amount}");
            }
        }
        private void DrawCustomEffect(RenderDrawContext context)
        {
            GraphicsContext.CommandList.CopyRegion(hdrTexture, 0, null, hdrRenderTexture, 0);

            postProcessingEffects.SetInput(hdrRenderTexture);
            postProcessingEffects.SetInput(1, null); // No depth
            postProcessingEffects.SetOutput(GraphicsContext.CommandList.RenderTarget);
            postProcessingEffects.Draw(context);
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunImageEffect()
        {
            RunGameTest(new TestImageEffect());
        }
    }
}
