// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.Rendering.Sprites;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ContentDecorator"/> 
    /// </summary>
    public class ContentDecoratorTest : UITestGameBase
    {
        private TextBlock textBlock;

        public ContentDecoratorTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            
            textBlock = new TextBlock
            {
                TextColor = Color.Black,
                Font = Content.Load<SpriteFont>("HanSans13"),
                Text = @"Simple sample text surrounded by decorator.",
                SynchronousCharacterGeneration = true
            };

            var decorator = new ContentDecorator
            {
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

            if (Input.IsKeyPressed(Keys.Left))
                textBlock.TextSize = 3 * textBlock.ActualTextSize / 4;
            if (Input.IsKeyPressed(Keys.Right))
                textBlock.TextSize = 4 * textBlock.ActualTextSize / 3;
            if (Input.IsKeyPressed(Keys.Delete))
                textBlock.TextSize = float.NaN;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest0).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
        }

        private void DrawTest0()
        {
            textBlock.TextSize = 12;
        }

        private void DrawTest1()
        {
            textBlock.TextSize = 18;
        }

        private void DrawTest2()
        {
            textBlock.TextSize = 24;
        }

        [Fact]
        public void RunContentDecoratorTest()
        {
            RunGameTest(new ContentDecoratorTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        internal static void Main()
        {
            using (var game = new ContentDecoratorTest())
                game.Run();
        }
    }
}
