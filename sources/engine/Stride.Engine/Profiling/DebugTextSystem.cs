// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Profiling
{
    public class DebugTextSystem : GameSystemBase
    {
        internal struct DebugOverlayMessage
        {
            public string Message;
            public Int2 Position;
            public Color4 TextColor;
            public TimeSpan RemeaningTime;
        }
        
        private FastTextRenderer fastTextRenderer;
        private readonly List<DebugOverlayMessage> overlayMessages = new List<DebugOverlayMessage>();

        public DebugTextSystem(IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Visible = Platform.IsRunningDebugAssembly;

            DrawOrder = 0xffffff;
            UpdateOrder = -100100; //before script
        }

        /// <summary>
        /// Print a custom overlay message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="timeOnScreen"></param>
        public void Print(string message, Int2 position, Color4? color = null, TimeSpan? timeOnScreen = null)
        {
            var msg = new DebugOverlayMessage 
            { 
                Message = message,
                Position = position,
                TextColor = color ?? TextColor,
                RemeaningTime = timeOnScreen ?? DefaultOnScreenTime,
            };

            overlayMessages.Add(msg);

            //drop one old message if the tail size has been reached
            if (overlayMessages.Count > TailSize)
            {
                overlayMessages.RemoveAt(0);
            }
        }

        /// <summary>
        /// Sets or gets the color to use when drawing the profiling system fonts.
        /// </summary>
        public Color4 TextColor { get; set; } = Color.LightGreen;

        /// <summary>
        /// Sets or gets the time that messages will stay on screen by default.
        /// </summary>
        public TimeSpan DefaultOnScreenTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Sets or gets the size of the messages queue, older messages will be discarded if the size is greater.
        /// </summary>
        public int TailSize { get; set; } = 100;

        public override void Update(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            if (overlayMessages.Count == 0)
            {
                return;
            }

            if (fastTextRenderer == null)
            {
                fastTextRenderer = new FastTextRenderer(Game.GraphicsContext)
                {
                    DebugSpriteFont = Content.Load<Texture>("StrideDebugSpriteFont"),
                    TextColor = TextColor,
                };
            }

            // TODO GRAPHICS REFACTOR where to get command list from?
            Game.GraphicsContext.CommandList.SetRenderTargetAndViewport(null, Game.GraphicsDevice.Presenter.BackBuffer);

            var currentColor = TextColor;
            fastTextRenderer.Begin(Game.GraphicsContext);

            // the loop is done backwards so when removing elements from the list you don't change the index of elements that weren't processed already
            for (int index = overlayMessages.Count - 1; index > 0; index--)
            {
                var msg = overlayMessages[index];
                if (msg.TextColor != currentColor)
                {
                    currentColor = msg.TextColor;
                    fastTextRenderer.End(Game.GraphicsContext);
                    fastTextRenderer.TextColor = currentColor;
                    fastTextRenderer.Begin(Game.GraphicsContext);
                }

                msg.RemeaningTime -= gameTime.Elapsed;

                if (msg.RemeaningTime < TimeSpan.Zero)
                {
                    overlayMessages.RemoveAt(index);
                }

                fastTextRenderer.DrawString(Game.GraphicsContext, msg.Message, msg.Position.X, msg.Position.Y);
            }
            fastTextRenderer.End(Game.GraphicsContext);
        }
    }
}
