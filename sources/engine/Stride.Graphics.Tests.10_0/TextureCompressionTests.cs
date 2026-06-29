// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Exercises the asset-pipeline texture compression paths by drawing each compressed variant fullscreen.
    /// On Windows/Linux/macOS (D3D11/12/Vulkan, profile ≥10.0): ColorNone→BC1, ColorExplicit→BC2,
    /// ColorInterpolated→BC3, Grayscale→BC4, NormalMap→BC5.
    /// On Android/iOS (Vulkan/Metal/GLES, profile ≥10.0): every variant collapses to ASTC_6x6.
    /// Gold images are intentionally per-platform.
    /// </summary>
    // TODO add HDR (BC6H) and HighQuality (BC7) variants when TextureHelper enables those paths.
    // TODO additional ASTC block sizes (4x4/5x5/8x8) coverage when TextureHelper varies block size by hint.
    public class TextureCompressionTests : GameTestBase
    {
        private string textureUrl;
        private Texture texture;

        public TextureCompressionTests() : this(null)
        {
        }

        private TextureCompressionTests(string textureUrl)
        {
            this.textureUrl = textureUrl;
            GraphicsDeviceManager.PreferredBackBufferWidth = 256;
            GraphicsDeviceManager.PreferredBackBufferHeight = 256;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            texture = Content.Load<Texture>(textureUrl);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.Draw(DrawTexture).TakeScreenshot();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            DrawTexture();
        }

        private void DrawTexture()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            GraphicsContext.DrawTexture(texture, GraphicsDevice.SamplerStates.PointClamp);
        }

        [Fact]
        public void ColorNone() => RunGameTest(new TextureCompressionTests("TextureCompressionTests/ColorNone") { TestName = nameof(ColorNone) });

        [Fact]
        public void ColorExplicit() => RunGameTest(new TextureCompressionTests("TextureCompressionTests/ColorExplicit") { TestName = nameof(ColorExplicit) });

        [Fact]
        public void ColorInterpolated() => RunGameTest(new TextureCompressionTests("TextureCompressionTests/ColorInterpolated") { TestName = nameof(ColorInterpolated) });

        [Fact]
        public void Grayscale() => RunGameTest(new TextureCompressionTests("TextureCompressionTests/Grayscale") { TestName = nameof(Grayscale) });

        [Fact]
        public void NormalMap() => RunGameTest(new TextureCompressionTests("TextureCompressionTests/NormalMap") { TestName = nameof(NormalMap) });
    }
}
