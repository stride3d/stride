// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests to test batching ordering for transparency.
    /// </summary>
    public class ClickTests : UITestGameBase
    {
        private List<Button> elements;

        public ClickTests()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var textblock = new TextBlock { Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(textblock);
            var element1 = new Button { Name = "1", Width = 800, Height = 480, Content = textblock };
            ApplyButtonDefaultStyle(element1);
            element1.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(100, 60, 0));
            element1.DependencyProperties.Set(Panel.ZIndexPropertyKey, -1);

            textblock = new TextBlock { Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(textblock);
            var element2 = new Button { Name = "2", Width = 400, Height = 240, Content = textblock };
            ApplyButtonDefaultStyle(element2);
            element2.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(300, 180, 0));
            element2.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);

            textblock = new TextBlock { Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(textblock);
            var element3 = new Button { Name = "3", Width = 400, Height = 240, Content = textblock };
            ApplyButtonDefaultStyle(element3);
            element3.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(150, 270, 0));
            element3.DependencyProperties.Set(Panel.ZIndexPropertyKey, 2);

            textblock = new TextBlock { Font = Content.Load<SpriteFont>("CourierNew12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(textblock);
            var element4 = new Button { Name = "4", Width = 400, Height = 240, Content = textblock };
            ApplyButtonDefaultStyle(element4);
            element4.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(450, 90, 0));
            element4.DependencyProperties.Set(Panel.ZIndexPropertyKey, 0);

            var canvas = new Canvas();
            canvas.Children.Add(element1);
            canvas.Children.Add(element2);
            canvas.Children.Add(element3);
            canvas.Children.Add(new Canvas { Children = { element4 } });

            elements = new List<Button> { element1, element2, element3, element4 };

            UIComponent.Page = new Engine.UIPage { RootElement = canvas };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            const float depthIncrement = 1f;
            const float rotationIncrement = 0.1f;

            var localMatrix = elements[1].LocalMatrix;

            if (Input.IsKeyPressed(Keys.Up))
                localMatrix.M43 -= depthIncrement;
            if (Input.IsKeyPressed(Keys.Down))
                localMatrix.M43 += depthIncrement;
            if (Input.IsKeyPressed(Keys.NumPad4))
                localMatrix = localMatrix * Matrix.RotationY(-rotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad6))
                localMatrix = localMatrix * Matrix.RotationY(+rotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad2))
                localMatrix = localMatrix * Matrix.RotationX(+rotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad8))
                localMatrix = localMatrix * Matrix.RotationX(-rotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad1))
                localMatrix = localMatrix * Matrix.RotationZ(-rotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad9))
                localMatrix = localMatrix * Matrix.RotationZ(+rotationIncrement);

            if (Input.KeyEvents.Any())
            {
                elements[1].LocalMatrix = localMatrix;

                UpdateTextBlockText();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (gameTime.FrameCount <= 2)
                UpdateTextBlockText();
        }

        private void UpdateTextBlockText()
        {
            foreach (var element in elements)
                ((TextBlock)element.Content).Text = "Element " + element.Name + "\nActual Depth: " + element.LocalMatrix.M43 + "\nDepth Bias: " + element.DepthBias;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(0, UpdateTextBlockText).TakeScreenshot(0);
            FrameGameSystem.Draw(1, Draw1).TakeScreenshot(1);
            FrameGameSystem.Draw(2, Draw2).TakeScreenshot(2);
            FrameGameSystem.Draw(3, () => SetElement2Matrix(Matrix.Translation(0, 0, -132))).Draw(4, Draw3).TakeScreenshot(4);
            FrameGameSystem.Draw(5, () => SetElement2Matrix(Matrix.Translation(0, 0, 204))).Draw(6, Draw4).TakeScreenshot(6);
            FrameGameSystem.Draw(7, () => SetElement2Matrix(Matrix.RotationYawPitchRoll(-0.1f, -0.2f, 0.3f))).Draw(8, Draw5).TakeScreenshot(8);
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
        }

        private void SetElement2Matrix(Matrix matrix)
        {
            elements[1].LocalMatrix = matrix;
        }

        private void Draw1()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.4f, 0.6f));
            Input.Update(new GameTime());
        }

        private void Draw2()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.4f, 0.6f));
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.6f, 0.4f));
            Input.Update(new GameTime());
        }

        private void Draw3()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.6f, 0.4f));
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.4f, 0.6f));
            Input.Update(new GameTime());
        }

        private void Draw4()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.4f, 0.6f));
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.4f, 0.4f));
            Input.Update(new GameTime());
        }

        private void Draw5()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.4f, 0.6f));
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.27625f, 0.5667f));
            Input.Update(new GameTime());
        }

        private void Draw6()
        {
            AddPointerEvent(PointerEventType.Released, new Vector2(0.348f, 0.231f));
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.441f, 0.418f));
            Input.Update(new GameTime());
        }

        [Fact]
        public void RunClickTests()
        {
            RunGameTest(new ClickTests());
        }
    } 
}
