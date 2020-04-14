// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Input.Tests
{
    /// <summary>
    /// Interactive test that displays the state of various input devices on the screen
    /// </summary>
    public class AdvancedInputTest : InputTestBase
    {
        // keyboard
        private string keyPressed;
        private string keyDown;
        private string keyReleased;

        // mouse
        private Vector2 mousePosition;
        private string mouseButtonPressed;
        private string mouseButtonDown;
        private string mouseButtonReleased;
        private string mouseWheelDelta;

        // pointers
        private readonly Queue<Tuple<Vector2, TimeSpan, int>> pointerPressed = new Queue<Tuple<Vector2, TimeSpan, int>>();
        private readonly Queue<Tuple<Vector2, TimeSpan, int>> pointerMoved = new Queue<Tuple<Vector2, TimeSpan, int>>();
        private readonly Queue<Tuple<Vector2, TimeSpan, int>> pointerReleased = new Queue<Tuple<Vector2, TimeSpan, int>>();

        private readonly TimeSpan displayPointerDuration;

        // Gestures
        private string dragEvent;
        private string flickEvent;
        private string longPressEvent;
        private string compositeEvent;
        private string tapEvent;

        private Tuple<GestureEvent, TimeSpan> lastFlickEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);
        private Tuple<GestureEvent, TimeSpan> lastLongPressEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);
        private Tuple<GestureEvent, TimeSpan> lastTapEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);

        private readonly TimeSpan displayGestureDuration;
        private VirtualButtonBinding b1;
        private VirtualButtonBinding b2;
        private VirtualButtonBinding b3;

        private VirtualButtonConfig virtualButtonConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="Input"/> class.
        /// </summary>
        public AdvancedInputTest()
        {
            DefaultTextColor = Color.Black;

            displayPointerDuration = TimeSpan.FromSeconds(1.5f);
            displayGestureDuration = TimeSpan.FromSeconds(1f);
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // activate the gesture recognitions
            Input.Gestures.Add(new GestureConfigDrag());
            Input.Gestures.Add(new GestureConfigFlick());
            Input.Gestures.Add(new GestureConfigLongPress());
            Input.Gestures.Add(new GestureConfigComposite());
            Input.Gestures.Add(new GestureConfigTap());

            // add a task to the task scheduler that will be executed asynchronously 
            Script.AddTask(UpdateInputStates);

            // Create a new VirtualButtonConfigSet if none exists. 
            Input.VirtualButtonConfigSet = Input.VirtualButtonConfigSet ?? new VirtualButtonConfigSet();

            //Bind "M" key, GamePad "Start" button and left mouse button to a virtual button "MyButton".
            b1 = new VirtualButtonBinding("M Key", VirtualButton.Keyboard.M);
            b2 = new VirtualButtonBinding("GamePad Start", VirtualButton.GamePad.Start);
            b3 = new VirtualButtonBinding("Mouse Left Button", VirtualButton.Mouse.Left);

            virtualButtonConfig = new VirtualButtonConfig();

            virtualButtonConfig.Add(b1);
            virtualButtonConfig.Add(b2);
            virtualButtonConfig.Add(b3);

            Input.VirtualButtonConfigSet.Add(virtualButtonConfig);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // clear the screen
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.White);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            BeginSpriteBatch();

            // render the keyboard key states
            WriteLine("Keyboard:");
            WriteLine("Key pressed: " + keyPressed, 1);
            WriteLine("Key down: " + keyDown, 1);
            WriteLine("Key released: " + keyReleased, 1);

            // render the mouse key states
            WriteLine("Mouse :");
            WriteLine("Mouse position: " + mousePosition, 1);
            WriteLine("Mouse button pressed: " + mouseButtonPressed, 1);
            WriteLine("Mouse button down: " + mouseButtonDown, 1);
            WriteLine("Mouse button released: " + mouseButtonReleased, 1);
            WriteLine("Mouse wheel delta: " + mouseWheelDelta, 1);

            // render the pointer states
            foreach (var tuple in pointerPressed)
                DrawPointers(tuple, 1.5f, Color.Blue);
            foreach (var tuple in pointerMoved)
                DrawPointers(tuple, 1f, Color.Green);
            foreach (var tuple in pointerReleased)
                DrawPointers(tuple, 2f, Color.Red);

            // render the gesture states
            WriteLine("Gestures :");
            WriteLine("Drag: " + dragEvent, 1);
            WriteLine("Flick: " + flickEvent, 1);
            WriteLine("LongPress: " + longPressEvent, 1);
            WriteLine("Composite: " + compositeEvent, 1);
            WriteLine("Tap: " + tapEvent, 1);
            
            WriteLine("Virtual Buttons :");
            foreach (var btn in virtualButtonConfig)
            {
                WriteLine($"VirtualButton ({btn.Name}): {btn.GetValue(Input)}", 1);
            }

            WriteLine("Game Pads :");
            foreach (var gp in Input.GamePads)
            {
                WriteLine($"{gp.Name} ({gp.GetType()}) [{gp.Index}]", 1);
            }

            WriteLine("Game Controllers :");
            foreach (var gc in Input.GameControllers)
            {
                WriteLine($"{gc.Name} ({gc.GetType()})", 1);
            }

            DrawCursor();

            EndSpriteBatch();
        }

        private void DrawPointers(Tuple<Vector2, TimeSpan, int> tuple, float baseScale, Color baseColor)
        {
            var position = tuple.Item1;
            var duration = DrawTime.Total - tuple.Item2;

            var scale = (float)(0.2f * (1f - duration.TotalSeconds / displayPointerDuration.TotalSeconds));
            var pointerScreenPosition = new Vector2(position.X * ScreenSize.X, position.Y * ScreenSize.Y);

            SpriteBatch.Draw(RoundTexture, pointerScreenPosition, baseColor, 0, RoundTextureSize / 2, scale * baseScale);
        }

        private async Task UpdateInputStates()
        {
            while (true)
            {
                await Script.NextFrame();

                var currentTime = DrawTime.Total;

                keyPressed = "";
                keyDown = "";
                keyReleased = "";
                mouseButtonPressed = "";
                mouseButtonDown = "";
                mouseButtonReleased = "";
                mouseWheelDelta = "";
                dragEvent = "";
                flickEvent = "";
                longPressEvent = "";
                compositeEvent = "";
                tapEvent = "";

                // Keyboard
                if (Input.HasKeyboard)
                {
                    keyPressed = string.Join(", ", Input.KeyEvents.Where(keyEvent => keyEvent.IsDown));
                    keyDown = string.Join(", ", Input.DownKeys);
                    keyReleased = string.Join(", ", Input.KeyEvents.Where(keyEvent => !keyEvent.IsDown));
                }

                // Mouse
                if (Input.HasMouse)
                {
                    mousePosition = Input.MousePosition;
                    for (int i = 0; i <= (int)MouseButton.Extended2; i++)
                    {
                        var button = (MouseButton)i;
                        if (Input.IsMouseButtonPressed(button))
                        {
                            if (mouseButtonPressed.Length > 0)
                                mouseButtonPressed += ", ";
                            mouseButtonPressed += button;
                        }
                        if (Input.IsMouseButtonDown(button))
                        {
                            if (mouseButtonDown.Length > 0)
                                mouseButtonDown += ", ";
                            mouseButtonDown += button;
                        }
                        if (Input.IsMouseButtonReleased(button))
                        {
                            if (mouseButtonReleased.Length > 0)
                                mouseButtonReleased += ", ";
                            mouseButtonReleased += button;
                        }
                    }
                }
                mouseWheelDelta = Input.MouseWheelDelta.ToString();

                // Pointers
                if (Input.HasPointer)
                {
                    foreach (var pointerEvent in Input.PointerEvents)
                    {
                        switch (pointerEvent.EventType)
                        {
                            case PointerEventType.Pressed:
                                pointerPressed.Enqueue(Tuple.Create(pointerEvent.Position, currentTime, pointerEvent.PointerId));
                                break;
                            case PointerEventType.Moved:
                                pointerMoved.Enqueue(Tuple.Create(pointerEvent.Position, currentTime, pointerEvent.PointerId));
                                break;
                            case PointerEventType.Released:
                                pointerReleased.Enqueue(Tuple.Create(pointerEvent.Position, currentTime, pointerEvent.PointerId));
                                break;
                            case PointerEventType.Canceled:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    // remove too old pointer events
                    RemoveOldPointerEventInfo(pointerPressed);
                    RemoveOldPointerEventInfo(pointerMoved);
                    RemoveOldPointerEventInfo(pointerReleased);
                }

                // Gestures
                foreach (var gestureEvent in Input.GestureEvents)
                {
                    switch (gestureEvent.Type)
                    {
                        case GestureType.Drag:
                            var dragGestureEvent = (GestureEventDrag)gestureEvent;
                            dragEvent = "Position = " + dragGestureEvent.TotalTranslation;
                            break;
                        case GestureType.Flick:
                            lastFlickEvent = Tuple.Create(gestureEvent, currentTime);
                            break;
                        case GestureType.LongPress:
                            lastLongPressEvent = Tuple.Create(gestureEvent, currentTime);
                            break;
                        case GestureType.Composite:
                            var compositeGestureEvent = (GestureEventComposite)gestureEvent;
                            compositeEvent = "Rotation = " + compositeGestureEvent.TotalRotation + " - Scale = " + compositeGestureEvent.TotalScale + " - Position = " + compositeGestureEvent.TotalTranslation;
                            break;
                        case GestureType.Tap:
                            lastTapEvent = Tuple.Create(gestureEvent, currentTime);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (currentTime - lastFlickEvent.Item2 < displayGestureDuration && lastFlickEvent.Item1 != null)
                {
                    var flickGestureEvent = (GestureEventFlick)lastFlickEvent.Item1;
                    flickEvent = " Start Position = " + flickGestureEvent.StartPosition + " - Speed = " + flickGestureEvent.AverageSpeed;
                }
                if (currentTime - lastLongPressEvent.Item2 < displayGestureDuration && lastLongPressEvent.Item1 != null)
                {
                    var longPressGestureEvent = (GestureEventLongPress)lastLongPressEvent.Item1;
                    longPressEvent = " Position = " + longPressGestureEvent.Position;
                }
                if (currentTime - lastTapEvent.Item2 < displayGestureDuration && lastTapEvent.Item1 != null)
                {
                    var tapGestureEvent = (GestureEventTap)lastTapEvent.Item1;
                    tapEvent = " Position = " + tapGestureEvent.TapPosition + " - number of taps = " + tapGestureEvent.NumberOfTaps;
                }
            }
        }

        // utility function to remove old pointer event from the queues
        private void RemoveOldPointerEventInfo(Queue<Tuple<Vector2, TimeSpan, int>> tuples)
        {
            while (tuples.Count > 0 && UpdateTime.Total - tuples.Peek().Item2 > displayPointerDuration)
                tuples.Dequeue();
        }

        // Override the Update function to quit the game when the user press escape
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.Escape))
                Exit();
        }

        [Fact]
        public void RunAdvancedInputTest()
        {
            RunGameTest(new AdvancedInputTest());
        }

        internal static void Main(string[] args)
        {
            using (var game = new AdvancedInputTest())
                game.Run();
        }
    }
}
