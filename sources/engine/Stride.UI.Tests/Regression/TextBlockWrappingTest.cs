// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="TextBlock"/> 
    /// </summary>
    public class TextBlockWrappingTest : UITestGameBase
    {
        private TextBlock textBlock;

        public TextBlockWrappingTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);
            
            textBlock = new TextBlock
            {
                WrapText = true,
                TextSize = 15,
                TextColor = Color.Black,
                Font = Content.Load<SpriteFont>("HanSans13"),
                Text = @"This is a very long sentence that will hopefully be wrapped up. 
Does it work with kanjis too? let's see that in the following line. Here we goes.
漢字も大丈夫そうですね！良かった！でも、文章の切る所は本当に合ってますか？どうかな。。
やった！",
                SynchronousCharacterGeneration = true,
            };
            var decorator = new ContentDecorator
            {
                Width = 200,
                Content = textBlock, 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                BackgroundImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("DumbWhite"))
            };

            UIComponent.Page = new Engine.UIPage { RootElement = decorator };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.W))
                textBlock.WrapText = !textBlock.WrapText;

            if (Input.IsKeyPressed(Keys.R))
            {
                textBlock.VerticalAlignment = VerticalAlignment.Stretch;
                textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

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
        }

        private void Draw0()
        {
            textBlock.WrapText = false;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw1()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw2()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw3()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Right;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw4()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw5()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw6()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private void Draw7()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
        }

        private void Draw8()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }

        private void Draw9()
        {
            textBlock.WrapText = true;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Right;
        }

        [Fact]
        public void RunTextBlockWrappingTest()
        {
            RunGameTest(new TextBlockWrappingTest());
        }
    }
}
