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
    public class ImageRegionTest : UITestGameBase
    {
        private StackPanel stackPanel;

        private int currentElement;

        public ImageRegionTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var image1 = new ImageElement
            {
                Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("BorderButtonCentered")) { Region = new Rectangle(256, 128, 512, 256), Borders = new Vector4(0.125f, 0.25f, 0.125f, 0.25f) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image2 = new ImageElement
            {
                Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) { Region = new Rectangle(0, 0, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image3 = new ImageElement
            {
                Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) { Region = new Rectangle(512, 0, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image4 = new ImageElement
            {
                Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) { Region = new Rectangle(0, 512, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image5 = new ImageElement
            {
                Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) { Region = new Rectangle(512, 512, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(image1);
            stackPanel.Children.Add(image2);
            stackPanel.Children.Add(image3);
            stackPanel.Children.Add(image4);
            stackPanel.Children.Add(image5);

            UIComponent.Page = new Engine.UIPage { RootElement = new ScrollViewer { Content = stackPanel } };
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
        }

        private void Draw0()
        {
            stackPanel.ScrolllToElement(0);
        }

        private void Draw1()
        {
            stackPanel.ScrolllToElement(1);
        }

        private void Draw2()
        {
            stackPanel.ScrolllToElement(2);
        }

        private void Draw3()
        {
            stackPanel.ScrolllToElement(3);
        }

        private void Draw4()
        {
            stackPanel.ScrolllToElement(4);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.Left))
            {
                currentElement = (stackPanel.Children.Count + currentElement - 1) % stackPanel.Children.Count;
                stackPanel.ScrolllToElement(currentElement);
            }
            if (Input.IsKeyReleased(Keys.Right))
            {
                currentElement = (stackPanel.Children.Count + currentElement + 1) % stackPanel.Children.Count;
                stackPanel.ScrolllToElement(currentElement);
            }
        }

        [Fact]
        public void RunImageRegionTest()
        {
            RunGameTest(new ImageRegionTest());
        }
    }
}
