// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Graphics.SDL
{
    using System.Diagnostics;
    // Using is here otherwise it would conflict with the current namespace that also defines SDL.
    using SDL2;

    public static class Application
    {
        /// <summary>
        /// Initialize Application for handling events and available windows.
        /// </summary>
        static Application()
        {
            InternalWindows = new Dictionary<IntPtr, WeakReference<Window>>(10);
        }

        /// <summary>
        /// Register <paramref name="c"/> to the list of available windows.
        /// </summary>
        /// <param name="c">Window to register</param>
        public static void RegisterWindow(Window c)
        {
            lock (InternalWindows)
            {
                InternalWindows.Add(c.SdlHandle, new WeakReference<Window>(c));
            }
        }

        /// <summary>
        /// Unregister <paramref name="c"/> from the list of available windows.
        /// </summary>
        /// <param name="c">Window to unregister</param> 
        public static void UnregisterWindow(Window c)
        {
            lock (InternalWindows)
            {
                InternalWindows.Remove(c.SdlHandle);
            }
        }

        /// <summary>
        /// Window that currently has the focus.
        /// </summary>
        public static Window WindowWithFocus { get; private set; }

        /// <summary>
        /// Screen coordinate of the mouse.
        /// </summary>
        public static Point MousePosition
        {
            get
            {
                int x, y;
                SDL.SDL_GetGlobalMouseState(out x, out y);
                return new Point(x, y);
            }
            set
            {
                int err = SDL.SDL_WarpMouseGlobal(value.X, value.Y);
                if (err != 0)
                    throw new NotSupportedException("Current platform doesn't let you set the position of the mouse cursor.");
            }
        }

        /// <summary>
        /// List of windows managed by the application.
        /// </summary>
        public static List<Window> Windows
        {
            get
            {
                lock (InternalWindows)
                {
                    var res = new List<Window>(InternalWindows.Count);
                    List<IntPtr> toRemove = null;
                    foreach (var weakRef in InternalWindows)
                    {
                        Window ctrl;
                        if (weakRef.Value.TryGetTarget(out ctrl))
                        {
                            res.Add(ctrl);
                        }
                        else
                        {
                                // Window was reclaimed without being unregistered first.
                                // We add it to `toRemove' to remove it from InternalWindows later.
                            if (toRemove == null)
                            {
                                toRemove = new List<IntPtr>(5);
                            }
                            toRemove.Add(weakRef.Key);
                        }
                    }
                        // Clean InternalWindows from windows that have been collected.
                    if (toRemove != null)
                    {
                        foreach (var w in toRemove)
                        {
                            InternalWindows.Remove(w);
                        }
                    }
                    return res;
                }
            }
        }

        /// <summary>
        /// Process all available events.
        /// </summary>
        public static void ProcessEvents()
        {
            SDL.SDL_Event e;
            while (SDL.SDL_PollEvent(out e) > 0)
            {
                // Handy for debugging
                //if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
                //    Debug.WriteLine(e.window.windowEvent);

                Application.ProcessEvent(e);
            }
        }

        /// <summary>
        /// Process a single event and dispatch it to the right window.
        /// </summary>
        public static void ProcessEvent(SDL.SDL_Event e)
        {
            Window ctrl = null;

                // Code below is to extract the associated `Window' instance and to find out the window
                // with focus. In the future, we could even add events handled at the application level.
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.button.windowID));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.motion.windowID));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.wheel.windowID));
                    break;
                    
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.key.windowID));
                    break;

                case SDL.SDL_EventType.SDL_TEXTEDITING:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.edit.windowID));
                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.text.windowID));
                    break;

                case SDL.SDL_EventType.SDL_FINGERMOTION:
                case SDL.SDL_EventType.SDL_FINGERDOWN:
                case SDL.SDL_EventType.SDL_FINGERUP:
                    ctrl = WindowWithFocus;
                    break;

                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                {
                    ctrl = WindowFromSdlHandle(SDL.SDL_GetWindowFromID(e.window.windowID));
                    switch (e.window.windowEvent)
                    {
                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                            WindowWithFocus = ctrl;
                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            WindowWithFocus = null;
                            break;
                    }
                    break;
                }
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    // Send these events to all the windows
                    Windows.ForEach(x => x.ProcessEvent(e));
                    break;
            }
            ctrl?.ProcessEvent(e);
        }

        /// <summary>
        /// Given a SDL Handle of a SDL window, retrieve the corresponding managed object. If object
        /// was already garbage collected, we will also clean up <see cref="InternalWindows"/>.
        /// </summary>
        /// <param name="w">SDL Handle of the window we are looking for</param>
        /// <returns></returns>
        private static Window WindowFromSdlHandle(IntPtr w)
        {
            lock (InternalWindows)
            {
                WeakReference<Window> weakRef;
                if (InternalWindows.TryGetValue(w, out weakRef))
                {
                    Window ctrl;
                    if (weakRef.TryGetTarget(out ctrl))
                    {
                        return ctrl;
                    } 
                    else
                    {
                            // Window does not exist anymore in our code. Clean `InternalWindows'.
                        InternalWindows.Remove(w);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Backup storage for windows of current application.
        /// </summary>
        private static readonly Dictionary<IntPtr, WeakReference<Window>> InternalWindows;
    }
}
#endif

