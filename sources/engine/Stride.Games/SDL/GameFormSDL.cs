// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using SDL2;
using Stride.Core.Mathematics;
using Stride.Graphics.SDL;

namespace Stride.Games
{
    /// <summary>
    /// Default Rendering Form on SDL based applications.
    /// </summary>
    public class GameFormSDL : Window
    {
#region Initialization
        /// <summary>
        /// Initializes a new instance of the <see cref="GameForm"/> class.
        /// </summary>
        public GameFormSDL() : this(GameContext.ProductName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameForm"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public GameFormSDL(string text) : base(text)
        {
            Size = new Size2(800, 600);
            ResizeBeginActions += GameForm_ResizeBeginActions;
            ResizeEndActions += GameForm_ResizeEndActions;
            ActivateActions += GameForm_ActivateActions;
            DeActivateActions += GameForm_DeActivateActions;
            previousWindowState = FormWindowState.Normal;
            MinimizedActions += GameForm_MinimizedActions;
            MaximizedActions += GameForm_MaximizedActions;
            RestoredActions += GameForm_RestoredActions;
            KeyDownActions += GameFormSDL_KeyDownActions;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when [app activated].
        /// </summary>
        public event EventHandler<EventArgs> AppActivated;

        /// <summary>
        /// Occurs when [app deactivated].
        /// </summary>
        public event EventHandler<EventArgs> AppDeactivated;

        /// <summary>
        /// Occurs when [pause rendering].
        /// </summary>
        public event EventHandler<EventArgs> PauseRendering;

        /// <summary>
        /// Occurs when [resume rendering].
        /// </summary>
        public event EventHandler<EventArgs> ResumeRendering;

        /// <summary>
        /// Occurs when [user resized].
        /// </summary>
        public event EventHandler<EventArgs> UserResized;

        /// <summary>
        /// Occurs when alt-enter key combination has been pressed.
        /// </summary>
        public event EventHandler<EventArgs> FullscreenToggle;

        #endregion

        #region Implementation
        // TODO: The code below is taken from GameForm.cs of the Windows Desktop implementation. This needs reviewing
        private Size2 cachedSize;
        private FormWindowState previousWindowState;
        //private DisplayMonitor monitor;
        private bool isUserResizing;

        private void GameForm_MinimizedActions(SDL.SDL_WindowEvent e)
        {
            previousWindowState = FormWindowState.Minimized;
            PauseRendering?.Invoke(this, EventArgs.Empty);
        }

        private void GameForm_MaximizedActions(SDL.SDL_WindowEvent e)
        {
            if (previousWindowState == FormWindowState.Minimized)
                ResumeRendering?.Invoke(this, EventArgs.Empty);

            previousWindowState = FormWindowState.Maximized;

            UserResized?.Invoke(this, EventArgs.Empty);
            //UpdateScreen();
            cachedSize = Size;
        }

        private void GameForm_RestoredActions(SDL.SDL_WindowEvent e)
        {
            if (previousWindowState == FormWindowState.Minimized)
            {
                ResumeRendering?.Invoke(this, EventArgs.Empty);
            }
            previousWindowState = FormWindowState.Normal;
        }

        private void GameForm_ActivateActions(SDL.SDL_WindowEvent e)
        {
            AppActivated?.Invoke(this, EventArgs.Empty);
        }

        private void GameForm_DeActivateActions(SDL.SDL_WindowEvent e)
        {
            AppDeactivated?.Invoke(this, EventArgs.Empty);
        }

        private void GameForm_ResizeBeginActions(SDL.SDL_WindowEvent e)
        {
            isUserResizing = true;
            cachedSize = Size;
            PauseRendering?.Invoke(this, EventArgs.Empty);
        }

        private void GameForm_ResizeEndActions(SDL.SDL_WindowEvent e)
        {
            if (isUserResizing && cachedSize.Equals(Size))
            {
                UserResized?.Invoke(this, EventArgs.Empty);
                // UpdateScreen();
            }

            isUserResizing = false;
            ResumeRendering?.Invoke(this, EventArgs.Empty);
        }

        private void GameFormSDL_KeyDownActions(SDL.SDL_KeyboardEvent e)
        {
            var altReturn = (e.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN) && ((e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0);
            var altEnter = (e.keysym.sym == SDL.SDL_Keycode.SDLK_KP_ENTER) && ((e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0);
            if (altReturn || altEnter)
            {
                FullscreenToggle?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
#endif
