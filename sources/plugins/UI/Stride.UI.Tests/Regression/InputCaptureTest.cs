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
            GameDragReleasedOverUI,
            DragFromButtonProducesNoGesture,
            LockedMouseIsNotCapturedByHover,
            ClickOnButtonThatMoved,
            ClickOnWorldSpacePanel,
            HitTestablePanelBackgroundCapturesClicks,
            ImageWithoutHitTestLetsClicksThrough,
        }

        private static readonly Vector2 OnButton = new Vector2(0.1f, 0.08f);
        private static readonly Vector2 OutsideUI = new Vector2(0.9f, 0.9f);
        private static readonly Vector2 OnMovedButton = new Vector2(0.5f, 0.38f);
        private static readonly Vector2 OnPanel = new Vector2(0.6f, 0.1f);
        private static readonly Vector2 OnImage = new Vector2(0.75f, 0.58f);

        private readonly Scenario scenario;
        private Button button;
        private PointerSimulated touch;
        private Button worldButton;

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

            var canvas = new Canvas { Children = { button } };
            UIComponent.Page = new Engine.UIPage { RootElement = canvas };

            switch (scenario)
            {
                case Scenario.ClickOnWorldSpacePanel:
                    worldButton = new Button { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                    ApplyButtonDefaultStyle(worldButton);
                    var worldPanel = new Engine.Entity("World panel")
                    {
                        new Engine.UIComponent
                        {
                            IsFullScreen = false,
                            IsBillboard = false,
                            Resolution = new Vector3(400, 300, 100),
                            Size = new Vector3(400, 300, 100),
                            Page = new Engine.UIPage { RootElement = worldButton },
                        },
                    };
                    Scene.Entities.Add(worldPanel);
                    break;
                case Scenario.HitTestablePanelBackgroundCapturesClicks:
                    var panel = new Grid { CanBeHitByUser = true, Width = 200, Height = 100 };
                    panel.SetCanvasAbsolutePosition(new Vector3(500, 30, 0));
                    canvas.Children.Add(panel);
                    break;
                case Scenario.ImageWithoutHitTestLetsClicksThrough:
                    var image = new ImageElement { Width = 100, Height = 100 };
                    image.SetCanvasAbsolutePosition(new Vector3(700, 300, 0));
                    canvas.Children.Add(image);
                    break;
            }

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
                case Scenario.GameDragReleasedOverUI:
                    FrameGameSystem.Draw(2, PressOutsideUI);
                    FrameGameSystem.Draw(3, DragOntoButton);
                    FrameGameSystem.Draw(4, ReleaseOverButton);
                    break;
                case Scenario.DragFromButtonProducesNoGesture:
                    FrameGameSystem.Draw(2, PressOnButtonWithDragGesture);
                    for (int frame = 3; frame <= 10; frame++)
                    {
                        var step = frame - 2;
                        FrameGameSystem.Draw(frame, () => DragAwayFromButton(step));
                    }
                    break;
                case Scenario.LockedMouseIsNotCapturedByHover:
                    FrameGameSystem.Draw(2, HoverButtonWithLockedMouse);
                    break;
                case Scenario.ClickOnButtonThatMoved:
                    FrameGameSystem.Draw(2, MoveButton);
                    FrameGameSystem.Draw(3, ClickOnMovedButton);
                    break;
                case Scenario.ClickOnWorldSpacePanel:
                    FrameGameSystem.Draw(2, MoveCamera);
                    FrameGameSystem.Draw(3, ClickOnWorldPanel);
                    break;
                case Scenario.HitTestablePanelBackgroundCapturesClicks:
                    FrameGameSystem.Draw(2, ClickOnPanelBackground);
                    break;
                case Scenario.ImageWithoutHitTestLetsClicksThrough:
                    FrameGameSystem.Draw(2, ClickOnImage);
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

        private void ReleaseOverButton()
        {
            MouseSimulated.SimulateMouseUp(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(Input.IsMouseButtonReleased(MouseButton.Left));
            Assert.Contains(Input.PointerEvents, e => e.EventType == PointerEventType.Released);
        }

        private void PressOnButtonWithDragGesture()
        {
            Input.Gestures.Add(new GestureConfigDrag());
            MouseSimulated.SetPosition(OnButton);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.Empty(Input.GestureEvents);
        }

        private void DragAwayFromButton(int step)
        {
            MouseSimulated.SetPosition(OnButton + new Vector2(step * 0.1f, 0f));
            Input.Update(new GameTime());

            Assert.Empty(Input.GestureEvents);
        }

        private void HoverButtonWithLockedMouse()
        {
            MouseSimulated.SetPosition(OnButton);
            Input.LockMousePosition();
            Input.Update(new GameTime());

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

        private void MoveButton()
        {
            button.SetCanvasRelativePosition(new Vector3(0.425f, 0.35f, 0f));
        }

        private void ClickOnMovedButton()
        {
            MouseSimulated.SetPosition(OnMovedButton);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(button.IsPressed);
            Assert.False(Input.IsMouseButtonPressed(MouseButton.Left));
        }

        private void MoveCamera()
        {
            Camera.Transform.Position = new Vector3(300, 0, 1000);
        }

        private void ClickOnWorldPanel()
        {
            var panelCenter = Vector3.TransformCoordinate(Vector3.Zero, CameraComponent.ViewProjectionMatrix);
            MouseSimulated.SetPosition(new Vector2((panelCenter.X + 1) / 2, (1 - panelCenter.Y) / 2));
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(worldButton.IsPressed);
            Assert.False(Input.IsMouseButtonPressed(MouseButton.Left));
        }

        private void ClickOnPanelBackground()
        {
            MouseSimulated.SetPosition(OnPanel);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.True(Input.Mouse.CaptureState.IsCaptured);
            Assert.False(Input.IsMouseButtonPressed(MouseButton.Left));
        }

        private void ClickOnImage()
        {
            MouseSimulated.SetPosition(OnImage);
            MouseSimulated.SimulateMouseDown(MouseButton.Left);
            Input.Update(new GameTime());

            Assert.False(Input.Mouse.CaptureState.IsCaptured);
            Assert.True(Input.IsMouseButtonPressed(MouseButton.Left));
        }

        [Theory]
        [InlineData(Scenario.ClickOnButton)]
        [InlineData(Scenario.ClickOutsideUI)]
        [InlineData(Scenario.HoverDuringGameDrag)]
        [InlineData(Scenario.TouchOnUIAndOutside)]
        [InlineData(Scenario.GameDragReleasedOverUI)]
        [InlineData(Scenario.DragFromButtonProducesNoGesture)]
        [InlineData(Scenario.LockedMouseIsNotCapturedByHover)]
        [InlineData(Scenario.ClickOnButtonThatMoved)]
        [InlineData(Scenario.ClickOnWorldSpacePanel)]
        [InlineData(Scenario.HitTestablePanelBackgroundCapturesClicks)]
        [InlineData(Scenario.ImageWithoutHitTestLetsClicksThrough)]
        public void RunInputCaptureTest(Scenario testScenario)
        {
            RunGameTest(new InputCaptureTest(testScenario));
        }
    }
}
