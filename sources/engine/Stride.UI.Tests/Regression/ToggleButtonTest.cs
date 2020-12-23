// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="Button"/> 
    /// </summary>
    public class ToggleButtonTest : UITestGameBase
    {
        private ToggleButton toggle;

        public ToggleButtonTest()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            toggle = new ToggleButton 
            {
                IsThreeState = true,
                Content = new TextBlock { TextColor = Color.Black, Text = "Toggle button test", Font = Content.Load<SpriteFont>("MicrosoftSansSerif15"), VerticalAlignment = VerticalAlignment.Center },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            ApplyToggleButtonBlockDefaultStyle(toggle);

            UIComponent.Page = new Engine.UIPage { RootElement = toggle };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.KeyEvents.Count > 0)
                toggle.IsThreeState = !toggle.IsThreeState;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            // Since click are evaluated before measuring/arranging/drawing, we need to render the UI at least once (see UIRenderFeature.Draw)
            FrameGameSystem.Draw(() => { }).TakeScreenshot();
            FrameGameSystem.Draw(Click).TakeScreenshot();
            FrameGameSystem.Draw(Click).TakeScreenshot();
            FrameGameSystem.Draw(Click).TakeScreenshot();
        }

        private void Click()
        {
            AddPointerEvent(PointerEventType.Pressed, new Vector2(0.5f));
            AddPointerEvent(PointerEventType.Released, new Vector2(0.5f));
            Input.Update(new GameTime());
        }

        [Fact]
        public void RunToggleButtonTest()
        {
            RunGameTest(new ToggleButtonTest());
        }
    }
}
