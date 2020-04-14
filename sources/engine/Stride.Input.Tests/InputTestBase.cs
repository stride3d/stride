// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;

namespace Stride.Input.Tests
{
    public class InputTestBase : GameTestBase
    {
        private const float TextSpaceY = 3;
        private const float TextSubSectionOffsetX = 15;

        protected Vector2 TextLeftTopCorner = new Vector2(5, 5);
        protected Color MouseColor = Color.Gray;
        protected Color DefaultTextColor = Color.Black;
        protected int LineOffset;

        private float textHeight;

        protected InputTestBase()
        { 
            // create and set the Graphic Device to the service register of the parent Game class
            GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
            GraphicsDeviceManager.PreferredBackBufferHeight = 720;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.None;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_1 };
        }

        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont SpriteFont { get; private set; }
        protected Vector2 ScreenSize { get; private set; }
        protected Texture RoundTexture { get; private set; }
        protected Vector2 RoundTextureSize { get; private set; }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Load the fonts
            SpriteFont = Content.Load<SpriteFont>("Arial");

            // load the round texture 
            RoundTexture = Content.Load<Texture>("round");

            // create the SpriteBatch used to render them
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // initialize parameters
            textHeight = SpriteFont.MeasureString("Measure Text Height (dummy string)").Y;
            ScreenSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
            RoundTextureSize = new Vector2(RoundTexture.Width, RoundTexture.Height);
        }

        protected void BeginSpriteBatch()
        {
            SpriteBatch.Begin(GraphicsContext);
            LineOffset = 0;
        }

        protected void EndSpriteBatch()
        {
            SpriteBatch.End();
        }

        protected void DrawCursor()
        {
            var mousePosition = Input.MousePosition;
            var mouseScreenPosition = new Vector2(mousePosition.X * ScreenSize.X, mousePosition.Y * ScreenSize.Y);
            SpriteBatch.Draw(RoundTexture, mouseScreenPosition, MouseColor, 0, RoundTextureSize / 2, 0.1f);
        }

        protected void WriteLine(string str, int indent = 0)
        {
            WriteLine(str, DefaultTextColor, indent);
        }

        protected void WriteLine(int line, string str, int indent = 0)
        {
            WriteLine(line, str, DefaultTextColor, indent);
        }

        protected void WriteLine(string str, Color color, int indent = 0)
        {
            WriteLine(LineOffset++, str, color, indent);
        }

        protected void WriteLine(int line, string str, Color color, int indent = 0)
        {
            SpriteBatch.DrawString(SpriteFont, str, TextLeftTopCorner + new Vector2(TextSubSectionOffsetX * indent, line * (textHeight + TextSpaceY)), color);
        }
    }
}
