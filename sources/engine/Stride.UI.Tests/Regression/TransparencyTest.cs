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
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests to test batching ordering for transparency.
    /// </summary>
    public class TransparencyTest : UITestGameBase
    {
        private Button element2;

        private Button element1;

        private float zValue;

        public TransparencyTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var sprites = Content.Load<SpriteSheet>("UIImages");
            element1 = new Button { Name = "1", Width = 300, Height = 150 };
            element1.PressedImage = SpriteFromSheet.Create(sprites, "Logo");
            element1.NotPressedImage = SpriteFromSheet.Create(sprites, "BorderButton");
            element1.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(350, 400, 0));
            element1.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);

            element2 = new Button { Name = "2", Width = 600, Height = 400 };
            element2.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(200, 100, -50));
            element2.DependencyProperties.Set(Panel.ZIndexPropertyKey, 0);
            element2.PressedImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("ImageButtonPressed"));
            element2.NotPressedImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("ImageButtonNotPressed"));

            var canvas = new Canvas();
            canvas.Children.Add(element1);
            canvas.Children.Add(element2);

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (ForceInteractiveMode)
            {
                zValue = 100 * (1 + (float)Math.Sin(gameTime.Total.TotalSeconds));

                element1.LocalMatrix = Matrix.Translation(0, 0, zValue);
            }
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(Draw0).TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
        }

        private void Draw0()
        {
            element1.LocalMatrix = Matrix.Translation(0, 0, 0);
        }

        private void Draw1()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f, 0.75f));
            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }

        private void Draw2()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.5f, 0.75f));
            Input.Update(new GameTime());
            UI.Update(new GameTime());

            element1.LocalMatrix = Matrix.Translation(0, 0, -100);
        }

        private void Draw3()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f, 0.75f));
            Input.Update(new GameTime());
            UI.Update(new GameTime());
        }
        
        [Fact]
        public void RunTransparencyUnitTest()
        {
            RunGameTest(new TransparencyTest());
        }
    }
}
