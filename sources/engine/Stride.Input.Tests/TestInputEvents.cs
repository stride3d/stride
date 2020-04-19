// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Input.Tests
{
    /// <summary>
    /// Simple interactive test that logs input events to the screen
    /// </summary>
    public class TestInputEvents : InputTestBase
    {
        private const int MaximumLogEntries = 30;
        private List<EventLog> eventLog = new List<EventLog>();

        private Dictionary<Type, Color> eventColors = new Dictionary<Type, Color>
        {
            [typeof(KeyEvent)] = Color.AliceBlue,
            [typeof(PointerEvent)] = Color.Orange,
            [typeof(GamePadAxisEvent)] = Color.Green,
            [typeof(GamePadButtonEvent)] = Color.Green,
            [typeof(GameControllerAxisEvent)] = Color.Bisque,
            [typeof(GameControllerButtonEvent)] = Color.Bisque,
            [typeof(GameControllerDirectionEvent)] = Color.Bisque,
        };

        public TestInputEvents()
        {
            DefaultTextColor = Color.White;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            Input.DeviceAdded += InputOnDeviceChanged;
            Input.DeviceRemoved += InputOnDeviceChanged;
        }

        private void InputOnDeviceChanged(object sender, DeviceChangedEventArgs deviceChangedEventArgs)
        {
            var device = deviceChangedEventArgs.Device;
            LogEvent($"{device} ({device.Name}, {device.Id}) {deviceChangedEventArgs.Type} from {deviceChangedEventArgs.Source}", Color.Magenta);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // clear the screen
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            BeginSpriteBatch();

            foreach (var evt in Input.Events)
            {
                LogEvent(evt.ToString(), GetLogColor(evt));
            }

#if STRIDE_PLATFORM_WINDOWS
            WriteLine($"Raw input: {Input.UseRawInput} (Ctrl+R to toggle)");
#endif
            WriteLine($"Locked mouse position: {Input.IsMousePositionLocked} (Ctrl+E to toggle)");

            if ((Input.IsKeyDown(Keys.LeftCtrl) || Input.IsKeyDown(Keys.RightCtrl)))
            {
#if STRIDE_PLATFORM_WINDOWS
                // Toggle raw input
                if (Input.IsKeyPressed(Keys.R))
                {
                    Input.UseRawInput = !Input.UseRawInput;
                }
#endif
                // Toggle mouse lock
                if (Input.IsKeyPressed(Keys.E))
                {
                    if (Input.IsMousePositionLocked)
                        Input.UnlockMousePosition();
                    else
                        Input.LockMousePosition(Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift));   
                }
            }

            WriteLine("Input Events:");
            foreach (var evt in eventLog)
            {
                WriteLine(evt.Message, evt.Color, 1);
            }


            EndSpriteBatch();
        }

        private void LogEvent(string message, Color color)
        {
            eventLog.Add(new EventLog
            {
                Color = color,
                Message = message
            });
            while (eventLog.Count > MaximumLogEntries)
            {
                eventLog.RemoveAt(0);
            }
        }

        private Color GetLogColor(InputEvent evt)
        {
            Color color;
            if (!eventColors.TryGetValue(evt.GetType(), out color))
                return DefaultTextColor;
            return color;
        }

        [Fact]
        public void RunTestInputEvents()
        {
            RunGameTest(new TestInputEvents());
        }

        internal static void Main(string[] args)
        {
            using (var game = new TestInputEvents())
                game.Run();
        }

        private struct EventLog
        {
            public Color Color;
            public string Message;
        }
    }
}
