// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="Canvas"/> and on the <see cref="UniformGrid"/>
    /// </summary>
    public class CanvasGridTest : UITestGameBase
    {
        public CanvasGridTest()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            const float relativeSize = 1 / 6f;

            var canvas = new Canvas();
            canvas.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);

            // left/top
            var image1 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image1.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image1.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image1.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(0, 0, 0));
            image1.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0, 0, 0));
            canvas.Children.Add(image1);

            // right/top
            var image2 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image2.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image2.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image2.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(1, 0, 0));
            image2.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(1, 0, 0));
            canvas.Children.Add(image2);

            // left/bottom
            var image3 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image3.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image3.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image3.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(0, 1, 0));
            image3.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0, 1, 0));
            canvas.Children.Add(image3);

            // 1/3 right/bottom
            var image4 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image4.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image4.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image4.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(1, 1, 0));
            image4.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(1, 1, 0));
            canvas.Children.Add(image4);

            // 1/3 left/top middle centered
            var image5 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image5.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image5.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image5.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(1/3f, 1/3f, 0));
            image5.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0.5f, 0.5f, 0));
            canvas.Children.Add(image5);

            // 1/3 right/top right aligned 
            var image6 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image6.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image6.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image6.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(2 / 3f, 1 / 3f, 0));
            image6.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0, 0.5f, 0));
            canvas.Children.Add(image6);

            // 1/3 left/bottom bottom aligned
            var image7 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image7.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image7.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image7.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(1/3f, 2/3f, 0));
            image7.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0.5f, 0, 0));
            canvas.Children.Add(image7);

            // 1/3 right/bottom top aligned
            var image8 = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch };
            image8.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
            image8.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, relativeSize * Vector3.One);
            image8.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, new Vector3(2/3f, 2/3f, 0));
            image8.DependencyProperties.Set(Canvas.PinOriginPropertyKey, new Vector3(0.5f, 1, 0));
            canvas.Children.Add(image8);
            
            var grid = new UniformGrid { Rows = 3, Columns = 3 };
            for (int c = 0; c < 3; c++)
                for (int r = 0; r < 3; r++)
                    CreateAndInsertButton(grid, c, r);

            var baseGrid = new UniformGrid();
            baseGrid.Children.Add(grid);
            baseGrid.Children.Add(canvas);

            UIComponent.Page = new Engine.UIPage { RootElement = baseGrid };
        }

        private void CreateAndInsertButton(UniformGrid grid, int c, int r)
        {
            var button = new Button();
            ApplyButtonDefaultStyle(button);
            button.DependencyProperties.Set(GridBase.RowPropertyKey, r);
            button.DependencyProperties.Set(GridBase.ColumnPropertyKey, c);
            grid.Children.Add(button);
        }

        [Fact]
        public void RunCanvasGridTest()
        {
            RunGameTest(new CanvasGridTest());
        }
    }
}
