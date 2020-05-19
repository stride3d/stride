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
    /// Class for rendering tests on the <see cref="Slider"/> 
    /// </summary>
    public class SliderTest : UITestGameBase
    {
        private Slider slider;
        private UniformGrid grid;
        private SpriteSheet sliderImages;

        private bool isRotatedImages;

        public SliderTest()
        {
            // 5 = Texture assets set to uncompressed, build machine changed
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            sliderImages = Content.Load<SpriteSheet>("DebugSlider");

            slider = new Slider { TrackStartingOffsets = new Vector2(10, 6), TickOffset = 10 };
            SetSliderImages(isRotatedImages);

            grid = new UniformGrid { Children = { slider } };

            UIComponent.Page = new Engine.UIPage { RootElement = grid };
        }

        private void SetSliderImages(bool setRotatedImages)
        {
            var suffix = setRotatedImages ? "Rotated" : "";

            slider.TrackBackgroundImage = SpriteFromSheet.Create(sliderImages, "Background" + suffix);
            slider.TrackForegroundImage = SpriteFromSheet.Create(sliderImages, "Foreground" + suffix);
            slider.ThumbImage = SpriteFromSheet.Create(sliderImages, "Thumb" + suffix);
            slider.MouseOverThumbImage = SpriteFromSheet.Create(sliderImages, "ThumbOverred" + suffix);
            slider.TickImage = SpriteFromSheet.Create(sliderImages, "Tick" + suffix);
        }

        private void ResetSliderImages()
        {
            slider.TrackBackgroundImage = null;
            slider.TrackForegroundImage = null;
            slider.ThumbImage = null;
            slider.MouseOverThumbImage = null;
            slider.TickImage = null;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
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
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            const float RotationStep = 0.05f;

            if (Input.IsKeyReleased(Keys.T))
                slider.AreTicksDisplayed = !slider.AreTicksDisplayed;

            if (Input.IsKeyReleased(Keys.S))
                slider.ShouldSnapToTicks = !slider.ShouldSnapToTicks;

            if (Input.IsKeyReleased(Keys.R))
                slider.IsDirectionReversed = !slider.IsDirectionReversed;

            if (Input.IsKeyReleased(Keys.O))
                slider.Orientation = (Orientation)(((int)slider.Orientation + 1) % 3);

            if (Input.IsKeyReleased(Keys.Left))
                slider.Decrease();

            if (Input.IsKeyReleased(Keys.Right))
                slider.Increase();

            if (Input.IsKeyReleased(Keys.N))
                ResetSliderImages();

            if (Input.IsKeyPressed(Keys.V))
                slider.VerticalAlignment = (VerticalAlignment)(((int)slider.VerticalAlignment + 1) % 4);

            if (Input.IsKeyPressed(Keys.H))
                slider.HorizontalAlignment = (HorizontalAlignment)(((int)slider.HorizontalAlignment + 1) % 4);

            if (Input.IsKeyReleased(Keys.NumPad4))
                slider.LocalMatrix *= Matrix.RotationY(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad6))
                slider.LocalMatrix *= Matrix.RotationY(-RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad2))
                slider.LocalMatrix *= Matrix.RotationX(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad8))
                slider.LocalMatrix *= Matrix.RotationX(-RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad1))
                slider.LocalMatrix *= Matrix.RotationZ(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad9))
                slider.LocalMatrix *= Matrix.RotationZ(-RotationStep);
            if (Input.IsKeyReleased(Keys.Delete))
                slider.LocalMatrix *= Matrix.Translation(-10, 0, 0);
            if (Input.IsKeyReleased(Keys.PageDown))
                slider.LocalMatrix *= Matrix.Translation(10, 0, 0);
            if (Input.IsKeyReleased(Keys.Home))
                slider.LocalMatrix *= Matrix.Translation(0, -10, 0);
            if (Input.IsKeyReleased(Keys.End))
                slider.LocalMatrix *= Matrix.Translation(0, 10, 0);

            if (Input.IsKeyReleased(Keys.G))
                ChangeGridColumnRowNumbers();

            if (Input.IsKeyReleased(Keys.I))
            {
                isRotatedImages = !isRotatedImages;
                SetSliderImages(isRotatedImages);
            }
        }

        private void ChangeGridColumnRowNumbers()
        {
            grid.Rows = grid.Rows % 2 + 1;
            grid.Columns = grid.Columns % 2 + 1;
        }

        private void DrawTest1()
        {
            slider.Value = 0.25f;
        }

        private void DrawTest2()
        {
            slider.AreTicksDisplayed = true;
            slider.VerticalAlignment = VerticalAlignment.Stretch;
        }

        private void DrawTest3()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.75f, 0.5f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.75f, 0.5f));
            Input.Update(new GameTime());
        }

        private void DrawTest4()
        {
            slider.VerticalAlignment = VerticalAlignment.Center;
            slider.IsDirectionReversed = true;
        }

        private void DrawTest5()
        {
            slider.IsDirectionReversed = false;
            slider.ShouldSnapToTicks = true;
        }

        private void DrawTest6()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.54f, 0.5f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.54f, 0.5f));
            Input.Update(new GameTime());
        }

        private void DrawTest7()
        {
            SetSliderImages(true);
        }

        private void DrawTest8()
        {
            slider.Orientation = Orientation.Vertical;
        }

        private void DrawTest9()
        {
            SetSliderImages(false);
        }

        private void DrawTest10()
        {
            ChangeGridColumnRowNumbers();
        }

        private void DrawTest11()
        {
            slider.LocalMatrix = Matrix.Translation(20, 30, 0) * Matrix.RotationYawPitchRoll(-0.1f, -0.2f, 0.3f);
        }

        [Fact]
        public void RunSliderTest()
        {
            RunGameTest(new SliderTest());
        }
    }
}
