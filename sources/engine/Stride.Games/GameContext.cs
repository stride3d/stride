// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Reflection;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// Contains context used to render the game (Control for WinForm, a DrawingSurface for WP8...etc.).
    /// </summary>
    public abstract class GameContext
    {
        /// <summary>
        /// Context type of this instance.
        /// </summary>
        public AppContextType ContextType { get; protected set; }

        /// <summary>
        /// Indicating whether the user will call the main loop. E.g. Stride is used as a library.
        /// </summary>
        public bool IsUserManagingRun { get; protected set; }

        /// <summary>
        /// Gets the main loop callback to be called when <see cref="IsUserManagingRun"/> is true.
        /// </summary>
        /// <value>The run loop.</value>
        public Action RunCallback { get; internal set; }

        /// <summary>
        /// Gets the exit callback to be called when <see cref="IsUserManagingRun"/> is true when exiting the game.
        /// </summary>
        /// <value>The run loop.</value>
        public Action ExitCallback { get; internal set; }

        // TODO: remove these requested values.

        /// <summary>
        /// The requested width.
        /// </summary>
        internal int RequestedWidth;

        /// <summary>
        /// The requested height.
        /// </summary>
        internal int RequestedHeight;

        /// <summary>
        /// The requested back buffer format.
        /// </summary>
        internal PixelFormat RequestedBackBufferFormat;

        /// <summary>
        /// The requested depth stencil format.
        /// </summary>
        internal PixelFormat RequestedDepthStencilFormat;

        /// <summary>
        /// THe requested graphics profiles.
        /// </summary>
        internal GraphicsProfile[] RequestedGraphicsProfile;

        /// <summary>
        /// The device creation flags that will be used to create the <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <value>The device creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags;

        /// <summary>
        /// Indicate whether the game must initialize the default database when it starts running.
        /// </summary>
        public bool InitializeDatabase = true;

        /// <summary>
        /// Product name of game.
        /// TODO: Provide proper access title through code and game studio
        /// </summary>
        internal static string ProductName
        {
            get
            {
#if STRIDE_PLATFORM_UWP
                return "Stride Game";
#else
                var assembly = Assembly.GetEntryAssembly();
                var productAttribute = assembly?.GetCustomAttribute<AssemblyProductAttribute>();
                return productAttribute?.Product ?? "Stride Game";
#endif
            }
        }

        /// <summary>
        /// Product location of game.
        /// TODO: Only used for retrieving game's icon. See ProductName for future refactoring
        /// </summary>
        public static string ProductLocation
        {
            get
            {
#if STRIDE_PLATFORM_UWP
                return string.Empty;
#else
                var assembly = Assembly.GetEntryAssembly();
                return assembly?.Location;
#endif
            }
        }

        // This code is for backward compatibility only where the generated games
        // would not explicitly create the context, but would just use a Winform
#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)        /// <summary>
        /// Performs an implicit conversion from <see cref="Control"/> to <see cref="GameContextWinforms"/>.
        /// </summary>
        /// <param name="control">Winform control</param>
        /// <returns>The result of the conversion.</returns>
        [Obsolete("Use new GameContextWinforms(control) instead.")]
        public static implicit operator GameContext(System.Windows.Forms.Control control)
        {
            return new GameContextWinforms(control);
        }
#endif

#if (STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX) && STRIDE_GRAPHICS_API_OPENGL && STRIDE_UI_OPENTK
        /// <summary>
        /// Performs an implicit conversion from <see cref="OpenTK.GameWindow"/> to <see cref="GameContextOpenTK"/>.
        /// </summary>
        /// <param name="gameWindow">OpenTK GameWindow</param>
        /// <returns>The result of the conversion.</returns>
        [Obsolete ("Use new GameContextOpenTK(gameWindow) instead.")]
        public static implicit operator GameContext(OpenTK.GameWindow gameWindow)
        {
            return new GameContextOpenTK(gameWindow);
        }
#endif
    }

    /// <summary>
    /// Generic version of <see cref="GameContext"/>. The later is used to describe a generic game Context.
    /// This version enables us to constraint the game context to a specifc toolkit and ensures a better cohesion
    /// between the various toolkit specific classes, such as InputManager, GameWindow.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    public abstract class GameContext<TK> : GameContext
    {
        /// <summary>
        /// Underlying control associated with context.
        /// </summary>
        public TK Control { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameContext" /> class.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="requestedWidth">Width of the requested.</param>
        /// <param name="requestedHeight">Height of the requested.</param>
        protected GameContext(TK control, int requestedWidth = 0, int requestedHeight = 0, bool isUserManagingRun = false)
        {
            Control = control;
            RequestedWidth = requestedWidth;
            RequestedHeight = requestedHeight;
            IsUserManagingRun = isUserManagingRun;
        }
    }
}
