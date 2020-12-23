// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using System;
using Stride.Graphics.SDL;

namespace Stride.Games
{
        // Using is here otherwise it would conflict with the current namespace that also defines SDL.
    using SDL2;

    /// <summary>
    /// RenderLoop provides a rendering loop infrastructure. See remarks for usage. 
    /// </summary>
    /// <remarks>
    /// Use static <see cref="Run(Window,RenderCallback)"/>  
    /// method to directly use a renderloop with a render callback or use your own loop:
    /// <code>
    /// control.Show();
    /// using (var loop = new RenderLoop(control))
    /// {
    ///     while (loop.NextFrame())
    ///     {
    ///        // Perform draw operations here.
    ///     }
    /// }
    /// </code>
    /// Note that the main control can be changed at anytime inside the loop.
    /// </remarks>
    internal class SDLMessageLoop : IMessageLoop
    {
        private Window control;
        private bool isControlAlive;
        private bool switchControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsMessageLoop"/> class.
        /// </summary>
        public SDLMessageLoop(Window control)
        {
            Control = control;
        }

        /// <summary>
        /// Gets or sets the control to associate with the current render loop.
        /// </summary>
        /// <value>The control.</value>
        /// <exception cref="System.InvalidOperationException">Control is already disposed</exception>
        public Window Control
        {
            get
            {
                return control;
            }
            set
            {
                if (control == value) return;

                // Remove any previous control
                if (control != null && !switchControl)
                {
                    isControlAlive = false;
                    control.Disposed -= ControlDisposed;
                }

                if (value != null && value.IsDisposed)
                {
                    throw new InvalidOperationException("Control is already disposed");
                }

                control = value;
                switchControl = true;
            }
        }

        /// <summary>
        /// Calls this method on each frame.
        /// </summary>
        /// <returns><c>true</c> if if the control is still active, <c>false</c> otherwise.</returns>
        /// <exception cref="System.InvalidOperationException">An error occurred </exception>
        public bool NextFrame()
        {
            // Setup new control
            // TODO this is not completely thread-safe. We should use a lock to handle this correctly
            if (switchControl && control != null)
            {
                control.Disposed += ControlDisposed;
                isControlAlive = true;
                switchControl = false;
            }

            if (isControlAlive)
            {
                SDL.SDL_Event e;
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    Application.ProcessEvent(e);
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        isControlAlive = false;
                    }
                }
            }

            return isControlAlive || switchControl;
        }

        private void ControlDisposed(object sender, EventArgs e)
        {
            isControlAlive = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Control = null;
        }

        /// <summary>
        /// Delegate for the rendering loop.
        /// </summary>
        public delegate void RenderCallback();

        /// <summary>
        /// Runs the specified main loop for the specified windows form.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <param name="renderCallback">The rendering callback.</param>
        /// <exception cref="System.ArgumentNullException">form
        /// or
        /// renderCallback</exception>
        public static void Run(Window form, RenderCallback renderCallback)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (renderCallback == null) throw new ArgumentNullException(nameof(renderCallback));

            form.Show();
            using (var renderLoop = new SDLMessageLoop(form))
            {
                while (renderLoop.NextFrame())
                {
                    renderCallback();
                }
            }
        }
   }
}
#endif
