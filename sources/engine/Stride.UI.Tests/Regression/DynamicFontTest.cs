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
    /// Class for dynamic sized text rendering tests.
    /// </summary>
    public class DynamicFontTest : UITestGameBase
    {
        private ContentDecorator decorator;
        private TextBlock textBlock;

        public DynamicFontTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            textBlock = new TextBlock
                {
                    Font = Content.Load<SpriteFont>("HanSans13"), 
                    Text = "Simple Text - 簡単な文章。", 
                    TextColor = Color.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    SynchronousCharacterGeneration = true
                };

            decorator = new ContentDecorator
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                BackgroundImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("DumbWhite")),
                Content = textBlock
            };

            UIComponent.Page = new Engine.UIPage { RootElement = decorator };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            const float ChangeFactor = 1.1f;
            const float ChangeFactorInverse = 1 / ChangeFactor;

            // change the size of the virtual resolution
            if (Input.IsKeyReleased(Keys.NumPad0))
                UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width / 2f, GraphicsDevice.Presenter.BackBuffer.Height / 2f, 400);
            if (Input.IsKeyReleased(Keys.NumPad1))
                UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 400);
            if (Input.IsKeyReleased(Keys.NumPad2))
                UIComponent.Resolution = new Vector3(2 * GraphicsDevice.Presenter.BackBuffer.Width, 2 * GraphicsDevice.Presenter.BackBuffer.Height, 400);
            if (Input.IsKeyReleased(Keys.Right))
                UIComponent.Resolution = new Vector3((ChangeFactor * UIComponent.Resolution.X), UIComponent.Resolution.Y, UIComponent.Resolution.Z);
            if (Input.IsKeyReleased(Keys.Left))
                UIComponent.Resolution = new Vector3((ChangeFactorInverse * UIComponent.Resolution.X), UIComponent.Resolution.Y, UIComponent.Resolution.Z);
            if (Input.IsKeyReleased(Keys.Up))
                UIComponent.Resolution = new Vector3(UIComponent.Resolution.X, (ChangeFactor * UIComponent.Resolution.Y), UIComponent.Resolution.Z);
            if (Input.IsKeyReleased(Keys.Down))
                UIComponent.Resolution = new Vector3(UIComponent.Resolution.X, (ChangeFactorInverse * UIComponent.Resolution.Y), UIComponent.Resolution.Z);

            if (Input.IsKeyReleased(Keys.D1))
                decorator.LocalMatrix = Matrix.Scaling(1);
            if (Input.IsKeyReleased(Keys.D2))
                decorator.LocalMatrix = Matrix.Scaling(1.5f);
            if (Input.IsKeyReleased(Keys.D3))
                decorator.LocalMatrix = Matrix.Scaling(2);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest0).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest4).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest5).TakeScreenshot();
        }

        private void DrawTest0()
        {
            decorator.LocalMatrix = Matrix.Scaling(1);
            textBlock.TextSize = textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);
        }

        private void DrawTest1()
        {
            decorator.LocalMatrix = Matrix.Scaling(1);
            textBlock.TextSize = 2*textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);
        }

        private void DrawTest2()
        {
            decorator.LocalMatrix = Matrix.Scaling(1);
            textBlock.TextSize = textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width / 2f, GraphicsDevice.Presenter.BackBuffer.Height / 2f, 500);
        }

        private void DrawTest3()
        {
            decorator.LocalMatrix = Matrix.Scaling(2);
            textBlock.TextSize = textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 500);
        }

        private void DrawTest4()
        {
            decorator.LocalMatrix = Matrix.Scaling(1);
            textBlock.TextSize = textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width / 2f, GraphicsDevice.Presenter.BackBuffer.Height, 500);
        }

        private void DrawTest5()
        {
            decorator.LocalMatrix = Matrix.Scaling(1);
            textBlock.TextSize = textBlock.Font.Size;
            UIComponent.Resolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height / 2f, 500);
        }

        [Fact]
        public void RunDynamicFontTest()
        {
            RunGameTest(new DynamicFontTest());
        }
    }
}
