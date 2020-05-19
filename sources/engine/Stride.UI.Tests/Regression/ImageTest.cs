// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class ImageTest : UITestGameBase
    {
        private ImageElement imageElement;

        public ImageTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            imageElement = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv"))};
            UIComponent.Page = new Engine.UIPage { RootElement = imageElement };
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeImageColor(Color.Brown)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeImageColor(Color.Blue)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeImageColor(Color.Red)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeImageColor(Color.Lime)).TakeScreenshot();
        }

        private void ChangeImageColor(Color color)
        {
            imageElement.Color = color;
        }

        [Fact]
        public void RunImageTest()
        {
            RunGameTest(new ImageTest());
        }
    }
}
