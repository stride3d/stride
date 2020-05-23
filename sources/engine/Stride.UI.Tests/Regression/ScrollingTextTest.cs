// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
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
    /// Class for rendering tests on the <see cref="Button"/> 
    /// </summary>
    public class ScrollingTextTest : UITestGameBase
    {
        private ScrollingText textScroller;

        private ContentDecorator decorator;

        private const string InitialText = "This is a scrolling text test. ";
        private const string TextWithBlanks = "  This is another test with a lot of blanks                             .";

        public ScrollingTextTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();


            textScroller = new ScrollingText
            {
                Name = "Text Scroller", 
                DesiredCharacterNumber = 25, 
                Text = InitialText,
                TextColor = Color.Black,
                Font = Content.Load<SpriteFont>("CourierNew12"), 
                IsEnabled = ForceInteractiveMode,
                SynchronousCharacterGeneration = true
            };
            
            decorator = new ContentDecorator
            {
                Name = "ContentDecorator", 
                Content = textScroller, 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                BackgroundImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("DumbWhite"))
            };

            UIComponent.Page = new Engine.UIPage { RootElement = decorator };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.W))
                textScroller.RepeatText = !textScroller.RepeatText;

            if (Input.IsKeyDown(Keys.Right))
                textScroller.ScrollingSpeed /= 1.1f;

            if (Input.IsKeyDown(Keys.Left))
                textScroller.ScrollingSpeed *= 1.1f;

            if (Input.IsKeyReleased(Keys.C))
                textScroller.ClearText();

            if (Input.IsKeyReleased(Keys.T))
                textScroller.Text = TextWithBlanks;

            if (Input.IsKeyReleased(Keys.A))
                textScroller.AppendText(" Additional Text");

            if (Input.IsKeyReleased(Keys.B))
                IncreaseButtonSize();

            if (Input.IsKeyReleased(Keys.V))
                DecreaseButtonSize();
        }

        private void DecreaseButtonSize()
        {
            decorator.Width = 50;
            decorator.Height = 20;
        }

        private void IncreaseButtonSize()
        {
            decorator.Width = 500;
            decorator.Height = 100;
        }
        
        private void UpdateScrollingText(TimeSpan elapsedTime)
        {
            textScroller.IsEnabled = true;
            ((IUIElementUpdate)textScroller).Update(new GameTime(new TimeSpan(), elapsedTime));
            textScroller.IsEnabled = false;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
            FrameGameSystem.Draw(Draw4).TakeScreenshot();
            FrameGameSystem.Draw(Draw5).TakeScreenshot();
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
        }

        private void Draw1()
        {
            UpdateScrollingText(new TimeSpan(0, 0, 0, 5, 500));
        }

        private void Draw2()
        {
            //test text wrapping
            UpdateScrollingText(new TimeSpan(0, 0, 0, 5, 500));
        }

        private void Draw3()
        {
            // test higher speed (result should be same as Draw1)
            textScroller.Text = string.Empty; // force full reset
            textScroller.Text = InitialText;
            textScroller.ScrollingSpeed = 2 * textScroller.ScrollingSpeed;
            UpdateScrollingText(new TimeSpan(0, 0, 0, 2, 750));
            textScroller.ScrollingSpeed = textScroller.ScrollingSpeed / 2;
        }

        private void Draw4()
        {
            DecreaseButtonSize();
        }

        private void Draw5()
        {
            IncreaseButtonSize();
        }

        private void Draw6()
        {
            //test no text wrapping
            textScroller.RepeatText = false;
            UpdateScrollingText(new TimeSpan(0, 0, 0, 11));
        }

        [Fact]
        public void RunScrollingTextTest()
        {
            RunGameTest(new ScrollingTextTest());
        }
    }
}
