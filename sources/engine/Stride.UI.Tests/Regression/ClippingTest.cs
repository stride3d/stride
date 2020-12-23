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
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class ClippingTest : UITestGameBase
    {
        private ContentDecorator element1;
        private ContentDecorator element2;
        private ContentDecorator element3;
        private ContentDecorator element4;

        public ClippingTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var uiGroup = Content.Load<SpriteSheet>("UIImages");

            element4 = new ContentDecorator
            {
                Name = "4",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 200, Height = 100,
                LocalMatrix = Matrix.Translation(-50, -50, 0),
                BackgroundImage = SpriteFromSheet.Create(uiGroup, "uvNotRotated")
            };

            element3 = new ContentDecorator
            {
                Name = "3",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 300, Height = 150,
                Content = element4,
                LocalMatrix = Matrix.Translation(-200, -100, 0),
                BackgroundImage = SpriteFromSheet.Create(uiGroup, "uvRotated90")
            };
            
            element2 = new ContentDecorator
            {
                Name = "2",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 600, Height = 300,
                Content = element3,
                BackgroundImage = SpriteFromSheet.Create(uiGroup, "BorderButton")
            };
            element2.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(400, 200, 0));
            element2.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);

            element1 = new ContentDecorator
            {
                Name = "1",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 600, Height = 300,
                BackgroundImage = SpriteFromSheet.Create(uiGroup, "GameScreen")
            };

            var canvas = new Canvas();
            canvas.Children.Add(element1);
            canvas.Children.Add(element2);

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.D1))
                element1.ClipToBounds = !element1.ClipToBounds;
            if (Input.IsKeyReleased(Keys.D2))
                element2.ClipToBounds = !element2.ClipToBounds;
            if (Input.IsKeyReleased(Keys.D3))
                element3.ClipToBounds = !element3.ClipToBounds;
            if (Input.IsKeyReleased(Keys.D4))
                element4.ClipToBounds = !element4.ClipToBounds;

            if (Input.IsKeyDown(Keys.Left))
                element3.LocalMatrix = Matrix.Translation(element3.LocalMatrix.TranslationVector - Vector3.UnitX);
            if (Input.IsKeyDown(Keys.Right))
                element3.LocalMatrix = Matrix.Translation(element3.LocalMatrix.TranslationVector + Vector3.UnitX);

            if (Input.IsKeyDown(Keys.Up))
                element3.LocalMatrix = Matrix.Translation(element3.LocalMatrix.TranslationVector - Vector3.UnitY);
            if (Input.IsKeyDown(Keys.Down))
                element3.LocalMatrix = Matrix.Translation(element3.LocalMatrix.TranslationVector + Vector3.UnitY);

            if (Input.IsKeyDown(Keys.NumPad7))
                MoveElementToLeftTopCorner();
            if (Input.IsKeyDown(Keys.NumPad9))
                MoveElementToRightTopCorner();
            if (Input.IsKeyDown(Keys.NumPad5))
                MoveElementToIntersection();
            if (Input.IsKeyDown(Keys.NumPad3))
                MoveElementToRightBottomCorner();
        }

        private void MoveElementToLeftTopCorner()
        {
            element3.LocalMatrix = Matrix.Translation(new Vector3(-333,-141,0));
        }

        private void MoveElementToRightTopCorner()
        {
            element3.LocalMatrix = Matrix.Translation(new Vector3(275, -150, 0));
        }

        private void MoveElementToRightBottomCorner()
        {
            element3.LocalMatrix = Matrix.Translation(new Vector3(175, 110, 0));
        }

        private void MoveElementToIntersection()
        {
            element3.LocalMatrix = Matrix.Translation(new Vector3(-60, 70, 0));
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
            FrameGameSystem.Draw(DrawTest6).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest7).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest8).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest9).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest10).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest11).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest12).TakeScreenshot();
        }

        private void DrawTest0()
        {
            MoveElementToLeftTopCorner();
            element1.ClipToBounds = true;
            element2.ClipToBounds = false;
            element3.ClipToBounds = false;
        }
        private void DrawTest1()
        {
            MoveElementToLeftTopCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = true;
            element3.ClipToBounds = false;
        }
        private void DrawTest2()
        {
            MoveElementToLeftTopCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = false;
            element3.ClipToBounds = true;
        }


        private void DrawTest3()
        {
            MoveElementToRightTopCorner();
            element1.ClipToBounds = true;
            element2.ClipToBounds = false;
            element3.ClipToBounds = false;
        }
        private void DrawTest4()
        {
            MoveElementToRightTopCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = true;
            element3.ClipToBounds = false;
        }
        private void DrawTest5()
        {
            MoveElementToRightTopCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = false;
            element3.ClipToBounds = true;
        }


        private void DrawTest6()
        {
            MoveElementToRightBottomCorner();
            element1.ClipToBounds = true;
            element2.ClipToBounds = false;
            element3.ClipToBounds = false;
        }
        private void DrawTest7()
        {
            MoveElementToRightBottomCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = true;
            element3.ClipToBounds = false;
        }
        private void DrawTest8()
        {
            MoveElementToRightBottomCorner();
            element1.ClipToBounds = false;
            element2.ClipToBounds = false;
            element3.ClipToBounds = true;
        }


        private void DrawTest9()
        {
            MoveElementToIntersection();
            element1.ClipToBounds = true;
            element2.ClipToBounds = false;
            element3.ClipToBounds = false;
        }
        private void DrawTest10()
        {
            MoveElementToIntersection();
            element1.ClipToBounds = false;
            element2.ClipToBounds = true;
            element3.ClipToBounds = false;
        }
        private void DrawTest11()
        {
            MoveElementToIntersection();
            element1.ClipToBounds = false;
            element2.ClipToBounds = false;
            element3.ClipToBounds = true;
        }
        private void DrawTest12()
        {
            MoveElementToIntersection();
            element1.ClipToBounds = false;
            element2.ClipToBounds = true;
            element3.ClipToBounds = true;
        }

        [Fact]
        public void RunClippingTest()
        {
            RunGameTest(new ClippingTest());
        }
    }
}
