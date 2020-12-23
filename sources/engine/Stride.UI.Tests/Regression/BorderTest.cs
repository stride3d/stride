// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
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
    /// Regression tests for <see cref="Border"/>
    /// </summary>
    public class BorderTest : UITestGameBase
    {
        private Border border;

        public BorderTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            border = new Border { Width = 200, Height = 150, Content = new Button { NotPressedImage = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), DepthAlignment = DepthAlignment.Back}};
            border.SetCanvasPinOrigin(new Vector3(0.5f));
            
            border.BackgroundColor = Color.Red;

            ResetBorderElement();

            UIComponent.Page = new Engine.UIPage { RootElement = new Canvas { Children = { border } } };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            const float DepthIncrement = 10f;
            const float RotationIncrement = 0.1f;

            var localMatrix = border.LocalMatrix;

            if (Input.IsKeyPressed(Keys.Up))
                localMatrix.M43 -= DepthIncrement;
            if (Input.IsKeyPressed(Keys.Down))
                localMatrix.M43 += DepthIncrement;
            if (Input.IsKeyPressed(Keys.NumPad4))
                localMatrix = localMatrix * Matrix.RotationY(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad6))
                localMatrix = localMatrix * Matrix.RotationY(+RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad2))
                localMatrix = localMatrix * Matrix.RotationX(+RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad8))
                localMatrix = localMatrix * Matrix.RotationX(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad1))
                localMatrix = localMatrix * Matrix.RotationZ(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad9))
                localMatrix = localMatrix * Matrix.RotationZ(+RotationIncrement);

            if (Input.IsKeyPressed(Keys.L))
                border.BorderThickness += new Thickness(1, 0, 0, 0, 0, 0);
            if (Input.IsKeyPressed(Keys.R))
                border.BorderThickness += new Thickness(0, 0, 0, 1, 0, 0);
            if (Input.IsKeyPressed(Keys.T))
                border.BorderThickness += new Thickness(0, 1, 0, 0, 0, 0);
            if (Input.IsKeyPressed(Keys.B))
                border.BorderThickness += new Thickness(0, 0, 0, 0, 1, 0);
            if (Input.IsKeyPressed(Keys.F))
                border.BorderThickness += new Thickness(0, 0, 0, 0, 0, 1);
            if (Input.IsKeyPressed(Keys.S))
                border.BorderThickness += new Thickness(0, 0, 1, 0, 0, 0);

            if (Input.KeyEvents.Any())
                border.LocalMatrix = localMatrix;

            if (Input.IsKeyPressed(Keys.D1))
                ResetBorderElement();
            if (Input.IsKeyPressed(Keys.D2))
                TurnBorderElement();
            if (Input.IsKeyPressed(Keys.D3))
                FlattenBorderElement();
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(TurnBorderElement).TakeScreenshot();
            FrameGameSystem.Draw(FlattenBorderElement).TakeScreenshot();
        }

        private void ResetBorderElement()
        {
            border.Depth = 100;
            border.LocalMatrix = Matrix.Identity;
            border.BorderThickness = new Thickness(3, 5, 1, 4, 6, 2);
            border.SetCanvasRelativePosition(new Vector3(0.5f));
        }

        private void FlattenBorderElement()
        {
            border.LocalMatrix = Matrix.Identity;
            border.SetCanvasRelativePosition(new Vector3(0.5f, 0.5f, 0f));
            border.Depth = 0;

            var borderSize = border.BorderThickness;
            borderSize.Front = 0;
            borderSize.Back = 0;
            border.BorderThickness = borderSize;
        }

        private void TurnBorderElement()
        {
            border.LocalMatrix = Matrix.RotationYawPitchRoll(-0.2f, -0.3f, 0.4f);
        }

        [Fact]
        public void RunBorderTest()
        {
            RunGameTest(new BorderTest());
        }
    }
}
