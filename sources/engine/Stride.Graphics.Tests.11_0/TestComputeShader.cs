// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestComputeShader : GraphicTestGameBase
    {
        const int ReductionRatio = 4;

        private SpriteBatch spriteBatch;

        private Texture displayedTexture;

        private Texture outputTexture;
        private Texture inputTexture;

        private Int2 screenSize = new Int2(1200, 900);

        private ComputeEffectShader computeShaderEffect;
        private RenderContext drawEffectContext;

        public TestComputeShader()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = screenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = screenSize.Y;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            inputTexture = Content.Load<Texture>("uv");
            var groupCounts = new Int3(inputTexture.Width / ReductionRatio, inputTexture.Height / ReductionRatio, 1);
            outputTexture = Texture.New2D(GraphicsDevice, groupCounts.X, groupCounts.Y, 1, PixelFormat.R8G8B8A8_UNorm, TextureFlags.UnorderedAccess | TextureFlags.ShaderResource);
            displayedTexture = outputTexture;

            drawEffectContext = RenderContext.GetShared(Services);
            computeShaderEffect = new ComputeEffectShader(drawEffectContext) { ShaderSourceName = "ComputeShaderTestEffect", ThreadGroupCounts = groupCounts };
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.I))
                displayedTexture = inputTexture;

            if (Input.IsKeyPressed(Keys.O))
                displayedTexture = outputTexture;
        }

        protected override void Draw(GameTime gameTime)
        {
            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

            computeShaderEffect.Parameters.Set(ComputeShaderTestParams.NbOfIterations, ReductionRatio);
            computeShaderEffect.Parameters.Set(ComputeShaderTestKeys.input, inputTexture);
            computeShaderEffect.Parameters.Set(ComputeShaderTestKeys.output, outputTexture);
            computeShaderEffect.Draw(renderDrawContext);

            if (displayedTexture == null || spriteBatch == null)
                return;

            GraphicsContext.DrawTexture(displayedTexture);

            base.Draw(gameTime);
        }

        [SkippableFact(Skip="This test is unmaintained and currently doesn't pass")]
        public void RunTest()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);

            RunGameTest(new TestComputeShader());
        }
    }
}
