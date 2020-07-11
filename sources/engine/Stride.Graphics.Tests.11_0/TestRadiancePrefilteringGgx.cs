// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect.GGXPrefiltering;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestRadiancePrefilteringGgx : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;

        private RenderContext drawEffectContext;

        private Texture inputCubemap;
        private Texture outputCubemap;
        private Texture outputCubemapNoCompute;
        private Texture displayedCubemap;
        private Texture[] displayedViews = new Texture[6];

        private RadiancePrefilteringGGX radianceFilter;
        private RadiancePrefilteringGGXNoCompute radianceFilterNoCompute;

        private Int2 screenSize = new Int2(768, 1024);

        private int outputSize = 256;

        private int displayedLevel = 0;
        private int mipmapCount = 6;
        private int samplingCounts = 1024;

        private bool skipHighestLevel;

        private EffectInstance spriteEffect;

        private bool filterAtEachFrame = true;
        private bool hasBeenFiltered;
        private bool useComputeShader;
        private bool showOutput = true;

        public TestRadiancePrefilteringGgx() : this(false)
        {
            
        }

        protected TestRadiancePrefilteringGgx(bool filterAtEachFrame)
        {
            this.filterAtEachFrame = filterAtEachFrame;
            GraphicsDeviceManager.PreferredBackBufferWidth = screenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = screenSize.Y;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DisplayNextMipmapLevel).TakeScreenshot();
            FrameGameSystem.Draw(DisplayNextMipmapLevel).TakeScreenshot();
            FrameGameSystem.Draw(DisplayNextMipmapLevel).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            drawEffectContext = RenderContext.GetShared(Services);
            radianceFilter = new RadiancePrefilteringGGX(drawEffectContext);
            radianceFilterNoCompute = new RadiancePrefilteringGGXNoCompute(drawEffectContext);
            skipHighestLevel = radianceFilter.DoNotFilterHighestLevel;

            spriteBatch = new SpriteBatch(GraphicsDevice);
            inputCubemap = Content.Load<Texture>("CubeMap");
            outputCubemap = Texture.New2D(GraphicsDevice, outputSize, outputSize, MathUtil.Log2(outputSize), PixelFormat.R16G16B16A16_Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 6).DisposeBy(this);
            outputCubemapNoCompute = Texture.New2D(GraphicsDevice, outputSize, outputSize, MathUtil.Log2(outputSize), PixelFormat.R16G16B16A16_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 6).DisposeBy(this);
            CreateViews();

            //RenderSystem.Pipeline.Renderers.Add(new DelegateRenderer(Services) { Render = PrefilterCubeMap });
            //RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services) { ClearColor = Color.Zero });
            //RenderSystem.Pipeline.Renderers.Add(new DelegateRenderer(Services) { Render = RenderCubeMap });
        }

        private void PrefilterCubeMap(RenderDrawContext context)
        {
            if (!filterAtEachFrame && hasBeenFiltered)
                return;

            if (useComputeShader)
            {
                radianceFilter.DoNotFilterHighestLevel = skipHighestLevel;
                radianceFilter.MipmapGenerationCount = mipmapCount;
                radianceFilter.SamplingsCount = samplingCounts;
                radianceFilter.RadianceMap = inputCubemap;
                radianceFilter.PrefilteredRadiance = outputCubemap;
                radianceFilter.Draw(context);
            }
            else
            {
                radianceFilterNoCompute.DoNotFilterHighestLevel = skipHighestLevel;
                radianceFilterNoCompute.MipmapGenerationCount = mipmapCount;
                radianceFilterNoCompute.SamplingsCount = samplingCounts;
                radianceFilterNoCompute.RadianceMap = inputCubemap;
                radianceFilterNoCompute.PrefilteredRadiance = outputCubemapNoCompute;
                radianceFilterNoCompute.Draw(context);
            }

            hasBeenFiltered = true;
        }

        private void RenderCubeMap(RenderDrawContext context)
        {
            if (displayedViews == null || spriteBatch == null)
                return;

            spriteEffect = new EffectInstance(EffectSystem.LoadEffect("SpriteEffect").WaitForResult());

            var size = new Vector2(screenSize.X / 3f, screenSize.Y / 4f);

            context.CommandList.SetRenderTargetAndViewport(null, GraphicsDevice.Presenter.BackBuffer);
            context.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Green);

            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, spriteEffect);
            spriteBatch.Draw(displayedViews[1], new RectangleF(0, size.Y, size.X, size.Y), Color.White);
            spriteBatch.Draw(displayedViews[2], new RectangleF(size.X, 0f, size.X, size.Y), Color.White);
            spriteBatch.Draw(displayedViews[4], new RectangleF(size.X, size.Y, size.X, size.Y), Color.White);
            spriteBatch.Draw(displayedViews[3], new RectangleF(size.X, 2f * size.Y, size.X, size.Y), Color.White);
            spriteBatch.Draw(displayedViews[5], new RectangleF(size.X, 3f * size.Y, size.X, size.Y), null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically);
            spriteBatch.Draw(displayedViews[0], new RectangleF(2f * size.X, size.Y, size.X, size.Y), Color.White);
            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.NumPad0))
                skipHighestLevel = !skipHighestLevel;

            if (Input.IsKeyPressed(Keys.NumPad4))
                mipmapCount = Math.Max(1, mipmapCount - 1);

            if (Input.IsKeyPressed(Keys.NumPad6))
                mipmapCount = Math.Min(10, mipmapCount + 1);

            if (Input.IsKeyPressed(Keys.NumPad1))
                samplingCounts = Math.Max(1, samplingCounts / 2);

            if (Input.IsKeyPressed(Keys.NumPad3))
                samplingCounts = Math.Min(1024, samplingCounts * 2);

            if (Input.IsKeyPressed(Keys.Left))
                DisplayPreviousMipmapLevel();

            if (Input.IsKeyPressed(Keys.Right))
                DisplayNextMipmapLevel();

            if (Input.IsKeyPressed(Keys.I))
            {
                showOutput = false;
                CreateViews();
            }

            if (Input.IsKeyPressed(Keys.O))
            {
                showOutput = true;
                CreateViews();
            }

            if (Input.IsKeyPressed(Keys.C))
            {
                useComputeShader = !useComputeShader;
                CreateViews();
            }

            if (Input.IsKeyPressed(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "RadiancePrefilteredGGXCross_level{0}.png".ToFormat(displayedLevel));
        }

        protected override void Draw(GameTime gameTime)
        {
            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

            PrefilterCubeMap(renderDrawContext);
            RenderCubeMap(renderDrawContext);

            base.Draw(gameTime);
        }

        private void DisplayPreviousMipmapLevel()
        {
            displayedLevel = Math.Max(0, displayedLevel - 1);
            CreateViewsFor(displayedCubemap);
        }

        private void DisplayNextMipmapLevel()
        {
            displayedLevel = Math.Min(mipmapCount - 1, displayedLevel + 1);
            CreateViewsFor(displayedCubemap);
        }

        private void CreateViews()
        {
            if (showOutput)
            {
                CreateViewsFor(useComputeShader ? outputCubemap : outputCubemapNoCompute);
            }
            else
            {
                CreateViewsFor(inputCubemap);
            }
        }

        private void CreateViewsFor(Texture texture)
        {
            displayedCubemap = texture;
            for (int i = 0; i < displayedViews.Length; i++)
            {
                if (displayedViews[i] != null)
                {
                    displayedViews[i].Dispose();
                    displayedViews[i] = null;
                }
            }
            for (int i = 0; i < texture.ArraySize; i++)
            {
                displayedViews[i] = texture.ToTextureView(ViewType.Single, i, displayedLevel);
            }
        }

        [SkippableFact]
        public void RunTest()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);
            IgnoreGraphicPlatform(GraphicsPlatform.Vulkan);

            RunGameTest(new TestRadiancePrefilteringGgx());
        }
    }
}
