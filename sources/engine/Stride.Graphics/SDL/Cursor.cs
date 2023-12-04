// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using System;
using System.Diagnostics;
using Silk.NET.SDL;
using Stride.Core.Mathematics;
using Point = Stride.Core.Mathematics.Point;

namespace Stride.Graphics.SDL
{
    /// <summary>
    /// Basic operations to show, hide and load a cursor.
    /// </summary>
    public unsafe class Cursor
    {
        private static Sdl SDL = Window.SDL;

#region Initialization
        /// <summary>
        ///  Initialize a cursor with <paramref name="data"/> and <paramref name="mask"/>. For more details, see 
        /// https://wiki.libsdl.org/SDL_CreateCursor.
        /// </summary>
        /// <param name="data">Actual cursor image</param>
        /// <param name="mask">Mask for <paramref name="data"/></param>
        /// <param name="w">Width of cursor</param>
        /// <param name="h">Height of cursor</param>
        /// <param name="hot_x">Hotspot X coordinate of cursor</param>
        /// <param name="hot_y">Hotspot Y coordinate of cursor</param>
        public unsafe Cursor(byte[] data, byte[] mask, int w, int h, int hot_x, int hot_y)
        {
            Debug.Assert(
                (w | h) >= 0 &&
                (uint)w * (uint)h <= (uint)Math.Min(data.Length, mask.Length));
            fixed (byte* dataPtr = data)
            fixed (byte* maskPtr = mask)
                Handle = (IntPtr)SDL.CreateCursor(dataPtr, maskPtr, w, h, hot_x, hot_y);
        }
#endregion

#region Access

        /// <summary>
        /// Access to low level pointer to the SDL_Cursor struct.
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Position of cursor on screen.
        /// </summary>
        public static Point Position
        {
            get { return Application.MousePosition; }
            set { Application.MousePosition = value; }
        }
#endregion

#region Basic Operations
        /// <summary>
        /// Hide cursor.
        /// </summary>
        public static void Hide()
        {
            SDL.ShowCursor(0);
        }

        /// <summary>
        /// Show cursor.
        /// </summary>
        public static void Show()
        {
            SDL.ShowCursor(1);
        }

        /// <summary>
        /// Set cursor with <paramref name="cur"/>.
        /// </summary>
        /// <param name="cur">New cursor to show.</param>
        public static void SetCursor(Cursor cur)
        {
            SDL.SetCursor((Silk.NET.SDL.Cursor*)cur.Handle);
        }
#endregion
    }
}
#endif
