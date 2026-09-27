// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Xunit;

namespace Stride.UI.Tests.Regression
{
    /// <summary>
    /// Checks that input the UI uses is captured, so that game code does not also act on it.
    /// </summary>
    public class InputCaptureTest : UITestGameBase
    {
        public enum Scenario
        {
            ClickOnButton,
            ClickOutsideUI,
            HoverDuringGameDrag,
            TouchOnUIAndOutside,
        }

        private static readonly Vector2 OnButton = new Vector2(0.1f, 0.08f);
        private static readonly Vector2 OutsideUI = new Vector2(0.9f, 0.9f);

        private readonly Scenario scenario;
        private Button button;
        private PointerSimulated touch;

        public InputCaptureTest() : this(Scenario.ClickOnButton)
        {
        }

        private InputCaptureTest(Scenario scenario)
        {
            this.scenario = scenario;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var text = new TextBlock { Text = "button", Font = Content.Load<SpriteFont>("LiberationMono12"), SynchronousCharacterGeneration = true };
            ApplyTextBlockDefaultStyle(text);
            button = new Button { Content = text };
            ApplyButtonDefaultStyle(button);
            button.SetCanvasRelativePosition(new Vector3(0.025f, 0.05f, 0f));

            UIComponent.Page = new Engine.UIPage { RootElement = new Canvas { Children = { button } } };

            touch = InputSourceSimulated.AddPointer();
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
            switch (scenario)
            {
                case Scenario.ClickOnButton:
                    FrameGameSystem.Draw(2, ClickOnButton);
                    break;
                case Scenario.ClickOutsideUI:
                    FrameGameSystem.Draw(2, ClickOutsideUI);
                    break;
                case Scenario.HoverDuringGameDrag:
                    FrameGameSystem.Draw(2, PressOutsideUI);
                    FrameGameSystem.Draw(3, DragOntoButton);
                    break;
                case Scenario.TouchOnUIAndOutside:
                    FrameGameSystem.Draw(2, TouchOnUIAndOutside);
                    break;
            }
        }

        private void ClickOnButton()
        {
            MouseSimulated.SetPosition(OnButton);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(button.IsPressed);
            Assert.False(Input.IsMouseButtonPressed(MouseButton.Left));
            Assert.False(Input.IsMouseButtonDown(MouseButton.Left));
        }

        private void ClickOutsideUI()
        {
            MouseSimulated.SetPosition(OutsideUI);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(Input.IsMouseButtonPressed(MouseButton.Left));
        }

        private void PressOutsideUI()
        {
            MouseSimulated.SetPosition(OutsideUI);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());
        }

        private void DragOntoButton()
        {
            MouseSimulated.SetPosition(OnButton);
            Input.Update(new GameTime());

            Assert.True(Input.IsMouseButtonDown(MouseButton.Left));
            Assert.False(Input.Mouse.CaptureState.IsCaptured);
        }

        private void TouchOnUIAndOutside()
        {
            MouseSimulated.SetPosition(OutsideUI);
            touch.SimulatePointer(PointerEventType.Pressed, OnButton, 0);
            touch.SimulatePointer(PointerEventType.Pressed, OutsideUI, 1);
            Input.Update(new GameTime());

            var touchEvents = Input.PointerEvents.Where(e => e.Pointer == touch).ToList();
            Assert.NotEmpty(touchEvents);
            Assert.All(touchEvents, e => Assert.Equal(1, e.PointerId));
        }

        [Theory]
        [InlineData(Scenario.ClickOnButton)]
        [InlineData(Scenario.ClickOutsideUI)]
        [InlineData(Scenario.HoverDuringGameDrag)]
        [InlineData(Scenario.TouchOnUIAndOutside)]
        public void RunInputCaptureTest(Scenario testScenario)
        {
            RunGameTest(new InputCaptureTest(testScenario));
        }
    }
}
