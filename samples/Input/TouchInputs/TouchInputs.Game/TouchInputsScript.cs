// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;
using Stride.Input;

namespace TouchInputs
{
    public class TouchInputsScript : SyncScript
    {
        private const float TextSpaceY = 3;
        private const float TextSubSectionOffsetX = 15;
        private const string KeyboardSessionString = "Keyboard :";

        public SpriteFont Font;
        public Texture RoundTexture;

        private Vector2 roundTextureSize;

        private readonly Color fontColor = Color.WhiteSmoke;

        private float textHeight;
        private readonly Vector2 textLeftTopCorner = new Vector2(5, 5);

        // keyboard
        private string keyEvents;
        private string keyDown;

        // mouse
        private Vector2 mousePosition;
        private string mouseButtonPressed;
        private string mouseButtonDown;
        private string mouseButtonReleased;

        private readonly Color mouseColor = Color.DarkGray;

        // pointers
        private readonly Queue<Tuple<Vector2, TimeSpan>> pointerPressed = new Queue<Tuple<Vector2, TimeSpan>>();
        private readonly Queue<Tuple<Vector2, TimeSpan>> pointerMoved = new Queue<Tuple<Vector2, TimeSpan>>();
        private readonly Queue<Tuple<Vector2, TimeSpan>> pointerReleased = new Queue<Tuple<Vector2, TimeSpan>>();

        private readonly TimeSpan displayPointerDuration = TimeSpan.FromSeconds(1.5f);

        // Gestures
        private string dragEvent;
        private string flickEvent;
        private string longPressEvent;
        private string compositeEvent;
        private string tapEvent;

        private Tuple<GestureEvent, TimeSpan> lastFlickEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);
        private Tuple<GestureEvent, TimeSpan> lastLongPressEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);
        private Tuple<GestureEvent, TimeSpan> lastTapEvent = new Tuple<GestureEvent, TimeSpan>(null, TimeSpan.Zero);

        // GamePads
        private string gamePadText;

        private readonly TimeSpan displayGestureDuration = TimeSpan.FromSeconds(1f);

        public override void Start()
        {
            // initialize parameters
            textHeight = Font.MeasureString(KeyboardSessionString).Y;
            roundTextureSize = new Vector2(RoundTexture.Width, RoundTexture.Height);

            // activate the gesture recognitions
            if (!IsLiveReloading) // Live Scripting: do it only on first launch
            {
                Input.Gestures.Add(new GestureConfigDrag());
                Input.Gestures.Add(new GestureConfigFlick());
                Input.Gestures.Add(new GestureConfigLongPress());
                Input.Gestures.Add(new GestureConfigComposite());
                Input.Gestures.Add(new GestureConfigTap());
            }
        }

        public override void Update()
        {
            var currentTime = Game.DrawTime.Total;

            keyDown = "";
            keyEvents = "";
            mouseButtonPressed = "";
            mouseButtonDown = "";
            mouseButtonReleased = "";
            dragEvent = "";
            flickEvent = "";
            longPressEvent = "";
            compositeEvent = "";
            tapEvent = "";
            gamePadText = "";

            // Keyboard
            if (Input.HasKeyboard)
            {
                foreach (var keyEvent in Input.KeyEvents)
                    keyEvents += keyEvent + ", ";

                foreach (var key in Input.DownKeys)
                    keyDown += key + ", ";
            }

            // Mouse
            if (Input.HasMouse)
            {
                mousePosition = Input.MousePosition;
                for (int i = 0; i <= (int)MouseButton.Extended2; i++)
                {
                    var button = (MouseButton)i;
                    if (Input.IsMouseButtonPressed(button))
                        mouseButtonPressed += button + ", ";
                    if (Input.IsMouseButtonDown(button))
                        mouseButtonDown += button + ", ";
                    if (Input.IsMouseButtonReleased(button))
                        mouseButtonReleased += button + ", ";
                }
            }

            // Pointers
            if (Input.HasPointer)
            {
                foreach (var pointerEvent in Input.PointerEvents)
                {
                    switch (pointerEvent.EventType)
                    {
                        case PointerEventType.Pressed:
                            pointerPressed.Enqueue(Tuple.Create(pointerEvent.Position, currentTime));
                            break;
                        case PointerEventType.Moved:
                            pointerMoved.Enqueue(Tuple.Create(pointerEvent.Position, currentTime));
                            break;
                        case PointerEventType.Released:
                            pointerReleased.Enqueue(Tuple.Create(pointerEvent.Position, currentTime));
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
                        dragEvent = "Translation = " + dragGestureEvent.TotalTranslation;
                        break;
                    case GestureType.Flick:
                        lastFlickEvent = Tuple.Create(gestureEvent, currentTime);
                        break;
                    case GestureType.LongPress:
                        lastLongPressEvent = Tuple.Create(gestureEvent, currentTime);
                        break;
                    case GestureType.Composite:
                        var compositeGestureEvent = (GestureEventComposite)gestureEvent;
                        compositeEvent = "Rotation = " + compositeGestureEvent.TotalRotation + " - Scale = " + compositeGestureEvent.TotalScale + " - Translation = " + compositeGestureEvent.TotalTranslation;
                        break;
                    case GestureType.Tap:
                        lastTapEvent = Tuple.Create(gestureEvent, currentTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (Input.HasGamePad)
            {
                for (int i = 0; i < Input.GamePadCount; i++)
                {
                    var gamePadState = Input.GetGamePadByIndex(i);
                    gamePadText += "\n[" + i + "] " + gamePadState;
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

        public void Render(RenderDrawContext context, SpriteBatch spriteBatch)
        {
            var screenSize = spriteBatch.VirtualResolution.Value;

            // depth test off mode 
            spriteBatch.Begin(context.GraphicsContext, depthStencilState: DepthStencilStates.None);
            
            // render the keyboard key states
            spriteBatch.DrawString(Font, KeyboardSessionString, textLeftTopCorner, fontColor);
            spriteBatch.DrawString(Font, "Key pressed/released: " + keyEvents, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 1 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Key down: " + keyDown, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 2 * (textHeight + TextSpaceY)), fontColor);

            // render the mouse key states
            spriteBatch.DrawString(Font, "Mouse :", textLeftTopCorner + new Vector2(0, 4 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Mouse position: " + mousePosition, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 5 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Mouse button pressed: " + mouseButtonPressed, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 6 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Mouse button down: " + mouseButtonDown, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 7 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Mouse button released: " + mouseButtonReleased, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 8 * (textHeight + TextSpaceY)), fontColor);

            var mouseScreenPosition = new Vector2(mousePosition.X * screenSize.X, mousePosition.Y * screenSize.Y);
            spriteBatch.Draw(RoundTexture, mouseScreenPosition, mouseColor, 0, roundTextureSize / 2, 0.1f);

            // render the pointer states
            foreach (var tuple in pointerPressed)
                DrawPointers(spriteBatch, tuple, 1.5f, Color.Blue);
            foreach (var tuple in pointerMoved)
                DrawPointers(spriteBatch, tuple, 1f, Color.Green);
            foreach (var tuple in pointerReleased)
                DrawPointers(spriteBatch, tuple, 2f, Color.Red);

            // render the gesture states
            spriteBatch.DrawString(Font, "Gestures :", textLeftTopCorner + new Vector2(0, 10 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Drag: " + dragEvent, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 11 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Flick: " + flickEvent, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 12 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "LongPress: " + longPressEvent, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 13 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Composite: " + compositeEvent, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 14 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.DrawString(Font, "Tap: " + tapEvent, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 15 * (textHeight + TextSpaceY)), fontColor);

            spriteBatch.DrawString(Font, "GamePads: " + gamePadText, textLeftTopCorner + new Vector2(TextSubSectionOffsetX, 17 * (textHeight + TextSpaceY)), fontColor);
            spriteBatch.End();
        }
        private void DrawPointers(SpriteBatch spriteBatch, Tuple<Vector2, TimeSpan> tuple, float baseScale, Color baseColor)
        {
            var screenSize = spriteBatch.VirtualResolution.Value;
            var position = tuple.Item1;
            var duration = Game.DrawTime.Total - tuple.Item2;

            var scale = (float)(0.2f * (1f - duration.TotalSeconds / displayPointerDuration.TotalSeconds));
            var pointerScreenPosition = new Vector2(position.X * screenSize.X, position.Y * screenSize.Y);

            spriteBatch.Draw(RoundTexture, pointerScreenPosition, baseColor, 0, roundTextureSize / 2, scale * baseScale);
        }

        /// <summary>
        /// Utility function to remove old pointer event from the queues
        /// </summary>
        /// <param name="tuples">the pointers event position and triggered time.</param>
        private void RemoveOldPointerEventInfo(Queue<Tuple<Vector2, TimeSpan>> tuples)
        {
            while (tuples.Count > 0 && Game.UpdateTime.Total - tuples.Peek().Item2 > displayPointerDuration)
                tuples.Dequeue();
        }

    }
}
