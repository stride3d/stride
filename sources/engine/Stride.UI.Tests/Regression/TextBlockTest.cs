// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="TextBlock"/> 
    /// </summary>
    public class TextBlockTest : UITestGameBase
    {
        private TextBlock textBlock;

        public TextBlockTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);

            textBlock = new TextBlock
            {
                TextColor = Color.Black,
                Font = Content.Load<SpriteFont>("HanSans13"),
                Text = @"Text Block test
にほんご ニホンゴ 人
Several line of texts with different width.
Next is empty.

This is the last line.",
                SynchronousCharacterGeneration = true,
                BackgroundColor = Color.LightSkyBlue
            };

            UIComponent.Page = new Engine.UIPage { RootElement = textBlock };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.Down))
                UIComponent.Resolution = 3 * UIComponent.Resolution / 4;
            if (Input.IsKeyPressed(Keys.Up))
                UIComponent.Resolution = 4 * UIComponent.Resolution / 3;
            if (Input.IsKeyPressed(Keys.R))
                UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);

            if (Input.IsKeyPressed(Keys.Left))
                textBlock.TextSize = 3 * textBlock.ActualTextSize / 4;
            if (Input.IsKeyPressed(Keys.Right))
                textBlock.TextSize = 4 * textBlock.ActualTextSize / 3;
            if (Input.IsKeyPressed(Keys.Delete))
                textBlock.TextSize = float.NaN;

            if (Input.IsKeyReleased(Keys.NumPad1))
                textBlock.VerticalAlignment = VerticalAlignment.Top;
            if (Input.IsKeyReleased(Keys.NumPad2))
                textBlock.VerticalAlignment = VerticalAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad3))
                textBlock.VerticalAlignment = VerticalAlignment.Bottom;

            if (Input.IsKeyReleased(Keys.NumPad4))
                textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            if (Input.IsKeyReleased(Keys.NumPad5))
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad6))
                textBlock.HorizontalAlignment = HorizontalAlignment.Right;

            if (Input.IsKeyReleased(Keys.NumPad7))
                textBlock.TextAlignment = TextAlignment.Left;
            if (Input.IsKeyReleased(Keys.NumPad8))
                textBlock.TextAlignment = TextAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad9))
                textBlock.TextAlignment = TextAlignment.Right;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(Draw0).TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
            FrameGameSystem.Draw(Draw4).TakeScreenshot();
            FrameGameSystem.Draw(Draw5).TakeScreenshot();
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
            FrameGameSystem.Draw(Draw7).TakeScreenshot();
            FrameGameSystem.Draw(Draw8).TakeScreenshot();
            FrameGameSystem.Draw(Draw9).TakeScreenshot();
            FrameGameSystem.Draw(Draw10).TakeScreenshot();
            FrameGameSystem.Draw(Draw11).TakeScreenshot();
            FrameGameSystem.Draw(Draw12).TakeScreenshot();
            FrameGameSystem.Draw(Draw13).TakeScreenshot();
            FrameGameSystem.Draw(Draw14).TakeScreenshot();
        }

        private void Draw0()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw1()
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw2()
        {
            textBlock.TextAlignment = TextAlignment.Right;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw3()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw4()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw5()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        private void Draw6()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
        }
        private void Draw7()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Right;
        }
        private void Draw8()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        private void Draw9()
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        private void Draw10()
        {
            textBlock.TextAlignment = TextAlignment.Right;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        private void Draw11()
        {
            textBlock.TextSize = textBlock.Font.Size * 2;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        private void Draw12()
        {
            textBlock.TextSize = textBlock.Font.Size / 2;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        private void Draw13()
        {
            textBlock.TextSize = textBlock.Font.Size;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width / 2, GraphicsDevice.Presenter.BackBuffer.Height / 2, 500);
        }
        private void Draw14()
        {
            textBlock.TextSize = textBlock.Font.Size;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width * 2, GraphicsDevice.Presenter.BackBuffer.Height * 2, 500);
        }

        [Fact]
        public void RunTextBlockTest()
        {
            RunGameTest(new TextBlockTest());
        }
    }
}
