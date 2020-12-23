// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ScrollViewer"/> 
    /// </summary>
    public class ImageRotatedTest : UITestGameBase
    {
        private const int WindowWidth = 1024;
        private const int WindowHeight = 512;

        public ImageRotatedTest()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = WindowWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = WindowHeight;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var sprites = Content.Load<SpriteSheet>("RotatedImages");
            var img1 = new ImageElement { Source = SpriteFromSheet.Create(sprites, "NRNR"), StretchType = StretchType.Fill };
            var img2 = new ImageElement { Source = SpriteFromSheet.Create(sprites, "RNR"), StretchType = StretchType.Fill };
            var img3 = new ImageElement { Source = SpriteFromSheet.Create(sprites, "NRR"), StretchType = StretchType.Fill };
            var img4 = new ImageElement { Source = SpriteFromSheet.Create(sprites, "RR"), StretchType = StretchType.Fill };

            img1.SetGridColumnSpan(2);
            img2.SetGridColumnSpan(2);
            img2.SetGridRow(1);
            img3.SetGridRowSpan(2);
            img3.SetGridColumn(2);
            img4.SetGridRowSpan(2);
            img4.SetGridColumn(3);

            var grid = new UniformGrid
            {
                Rows = 2, 
                Columns = 4,
                Children = { img1, img2, img3, img4 }
            };

            UIComponent.Page = new Engine.UIPage { RootElement = grid };
        }

        [Fact]
        public void RunImageRotatedTest()
        {
            RunGameTest(new ImageRotatedTest());
        }
    }
}
