// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Silk.NET.SDL;
using Point = Stride.Core.Mathematics.Point;

namespace Stride.Graphics.SDL
{
    public unsafe class Window : IDisposable
    {
        public static Sdl SDL;

        private Silk.NET.SDL.Window* sdlHandle;

#region Initialization

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            SDL = Silk.NET.SDL.Sdl.GetApi();

            SDL.Init(Sdl.InitEverything);

            // Pass first mouse event when user clicked on window 
            SDL.SetHint(Sdl.HintMouseFocusClickthrough, "1");

            // Don't leave fullscreen on focus loss
            SDL.SetHint(Sdl.HintVideoMinimizeOnFocusLoss, "0");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class with <paramref name="title"/> as the title of the Window.
        /// </summary>
        /// <param name="title">Title of the window, see Text property.</param>
        public unsafe Window(string title)
        {
            WindowFlags flags = WindowFlags.WindowAllowHighdpi;
#if STRIDE_GRAPHICS_API_OPENGL
            flags |= WindowFlags.WindowOpengl;
#elif STRIDE_GRAPHICS_API_VULKAN
            flags |= WindowFlags.WindowVulkan;
#endif
#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
            flags |= WindowFlags.WindowBorderless | WindowFlags.WindowFullscreen | WindowFlags.WindowShown;
#else
            flags |= WindowFlags.WindowHidden | WindowFlags.WindowResizable;
#endif
            // Create the SDL window and then extract the native handle.
            sdlHandle = SDL.CreateWindow(title, Sdl.WindowposUndefined, Sdl.WindowposUndefined, 640, 480, (uint)flags);

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
            GraphicsAdapter.DefaultWindow = sdlHandle;
#endif

            if (sdlHandle == null)
            {
                throw new Exception("Cannot allocate SDL Window: " + SDL.GetErrorS()); 
            }

            SysWMInfo info = default;
            SDL.GetVersion(&info.Version);
            if (!SDL.GetWindowWMInfo(sdlHandle, &info))
            {
                throw new Exception("Cannot get Window information: " + SDL.GetErrorS());
            }

            if (Core.Platform.Type == Core.PlatformType.Windows)
            {
                Handle = info.Info.Win.Hwnd;
            }
            else if (Core.Platform.Type == Core.PlatformType.Linux)
            {
                Handle = (IntPtr)info.Info.X11.Window;
                Display = (IntPtr)info.Info.X11.Display;
            }
            else if (Core.Platform.Type == Core.PlatformType.Android)
            {
                Handle = (IntPtr)info.Info.Android.Window;
                Surface = (IntPtr)info.Info.Android.Surface;
            }
            else if (Core.Platform.Type == Core.PlatformType.macOS)
            {
                Handle = (IntPtr)info.Info.Cocoa.Window;
            }
            Application.RegisterWindow(this);
            Application.ProcessEvents();
        }
#endregion

        /// <summary>
        /// Move window to back.
        /// </summary>
        public virtual void SendToBack()
        {
            //no op
        }

        /// <summary>
        /// Move window to front.
        /// </summary>
        public virtual void BringToFront()
        {
            SDL.RaiseWindow(sdlHandle);
        }

        /// <summary>
        /// Get the mouse position on screen.
        /// </summary>
        public static Point MousePosition
        {
            get { return Application.MousePosition; }
        }

        /// <summary>
        /// Get the coordinate of the mouse in Window coordinates
        /// </summary>
        public Point RelativeCursorPosition
        {
            get
            {
                int x, y;
                SDL.GetMouseState(&x, &y);
                return new Point(x, y);
            }
            set
            {
                SDL.WarpMouseInWindow(sdlHandle, value.X, value.Y);
            }
        }

        /// <summary>
        /// Make the window topmost
        /// </summary>
        public bool TopMost
        {
            get { return SDL.GetHintS(Sdl.HintAllowTopmost) == "1"; }
            set
            {
                // FIXME: This is not yet implemented on SDL. We are using SDL_SetWindowPosition in
                // FIXME: the hope that it will apply the new hint.
                Point loc = Location;
                SDL.SetHint(Sdl.HintAllowTopmost, (value ? "1" : "0"));
                SDL.SetWindowPosition(sdlHandle, loc.X, loc.Y);
            }
        }

        /// <summary>
        /// Minimize the window when focus is lost in fullscreen, default is false.
        /// </summary>
        public bool MinimizeOnFocusLoss
        {
            get { return SDL.GetHintS(Sdl.HintVideoMinimizeOnFocusLoss) == "1"; }
            set { SDL.SetHint(Sdl.HintVideoMinimizeOnFocusLoss, (value ? "1" : "0")); }
        }

        /// <summary>
        /// Show window. The first time a window is shown we execute any actions from <see cref="HandleCreated"/>.
        /// </summary>
        public void Show()
        {
            SDL.ShowWindow(sdlHandle);
        }

        /// <summary>
        /// Gets or sets a value indicating whether fullscreen mode should be a borderless window matching the desktop size.
        /// Decides whether to set the SDL_WINDOW_FULLSCREEN_DESKTOP (fake fullscreen) or SDL_WINDOW_FULLSCREEN (real fullscreen) flag.
        /// </summary>
        public bool FullscreenIsBorderlessWindow { get; set; } = false;

        /// <summary>
        /// Are we showing the window in full screen mode?
        /// </summary>
        public bool IsFullScreen
        {
            get
            {
                var flags = SDL.GetWindowFlags(sdlHandle);
                return CheckFullscreenFlag(flags);
            }
            set
            {
                var fsFlag = GetFullscreenFlag();
                SDL.SetWindowFullscreen(sdlHandle, (uint)(value ? fsFlag : 0));
            }
        }

        private WindowFlags GetFullscreenFlag()
        {
            return FullscreenIsBorderlessWindow ? WindowFlags.WindowFullscreenDesktop : WindowFlags.WindowFullscreen;
        }

        private static bool CheckFullscreenFlag(uint flags)
        {
            return ((flags & (uint)WindowFlags.WindowFullscreen) != 0) || ((flags & (uint)WindowFlags.WindowFullscreenDesktop) != 0);
        }

        /// <summary>
        /// Is current window visible?
        /// </summary>
        public bool Visible
        {
            get
            {
                return (SDL.GetWindowFlags(sdlHandle) & (uint)WindowFlags.WindowShown) != 0;
            }
            set
            {
                if (value)
                {
                    SDL.ShowWindow(sdlHandle);
                }
                else
                {
                    SDL.HideWindow(sdlHandle);
                }
            }
        }

        /// <summary>
        /// State of the window which can be either of Normal, Maximized or Minimized.
        /// </summary>
        public FormWindowState WindowState
        {
            get
            {
                uint flags = SDL.GetWindowFlags(sdlHandle);
                if (CheckFullscreenFlag(flags))
                {
                    return FormWindowState.Fullscreen;
                }
                if ((flags & (uint)WindowFlags.WindowMaximized) != 0)
                {
                    return FormWindowState.Maximized;
                }
                else if ((flags & (uint)WindowFlags.WindowMinimized) != 0)
                {
                    return FormWindowState.Minimized;
                }
                else
                {
                    return FormWindowState.Normal;
                }
            }
            set
            {
                switch (value)
                {
                    case FormWindowState.Fullscreen:
                        SDL.SetWindowFullscreen(sdlHandle, (uint)GetFullscreenFlag());
                        break;
                    case FormWindowState.Maximized:
                        SDL.MaximizeWindow(sdlHandle);
                        break;
                    case FormWindowState.Minimized:
                        SDL.MinimizeWindow(sdlHandle);
                        break;
                    case FormWindowState.Normal:
                        SDL.RestoreWindow(sdlHandle);
                        break;
                }
            }
        }

        /// <summary>
        /// Is current window focused?
        /// </summary>
        public bool Focused
        {
            get
            {
                return (SDL.GetWindowFlags(sdlHandle) & (uint)WindowFlags.WindowInputFocus) != 0;
            }
        }

        /// <summary>
        /// Does current window offer a maximize button?
        /// </summary>
        /// <remarks>Setter is not implemented on SDL, since we do have callers, for the time being, the code does nothing instead of throwing an exception.</remarks>
        public bool MaximizeBox
        {
            get { return FormBorderStyle == FormBorderStyle.Sizable; }
            set
            {
                // FIXME: How to implement this since this is being called.
            }
        }

        /// <summary>
        /// Size of window.
        /// </summary>
        public Size2 Size
        {
            get
            {
                int w, h;
                SDL.GetWindowSize(sdlHandle, &w, &h);
                return new Size2(w, h);
            }
            set { SDL.SetWindowSize(sdlHandle, value.Width, value.Height); }
        }

        /// <summary>
        /// Size of the client area of a window.
        /// </summary>
        public unsafe Size2 ClientSize
        {
            get
            {
#if STRIDE_GRAPHICS_API_OPENGL || STRIDE_GRAPHICS_API_VULKAN
                int w, h;
                SDL.GLGetDrawableSize(sdlHandle, &w, &h);
                return new Size2(w, h);
#else
                var surface = SDL.GetWindowSurface(sdlHandle);
                return new Size2(surface->W, surface->H);
#endif
            }
            set
            {
                // FIXME: We need to adapt the ClientSize to an actual Size to take into account borders.
                // FIXME: On Windows you do this by using AdjustWindowRect.
                // SDL.SDL_GetWindowBordersSize(sdlHandle, out var top, out var left, out var bottom, out var right);
                // From SDL documentaion: Use this function to set the size of a window's client area.
                SDL.SetWindowSize(sdlHandle, value.Width, value.Height);
            }
        }

        /// <summary>
        /// Size of client area expressed as a rectangle.
        /// </summary>
        public unsafe Rectangle ClientRectangle
        {
            get
            {
#if STRIDE_GRAPHICS_API_OPENGL || STRIDE_GRAPHICS_API_VULKAN
                int w, h;
                SDL.GLGetDrawableSize(sdlHandle, &w, &h);
                return new Rectangle(0, 0, w, h);
#else
                var surface = SDL.GetWindowSurface(sdlHandle);
                return new Rectangle(0, 0, surface->W, surface->H);
#endif
            }
            set
            {
                // From SDL documentaion: Use this function to set the size of a window's client area.
                SDL.SetWindowSize(sdlHandle, value.Width, value.Height);
                SDL.SetWindowPosition(sdlHandle, value.X, value.Y);
            }
        }

        /// <summary>
        /// Coordinates of the top-left corner of the window in screen coordinate.
        /// </summary>
        public Point Location
        {
            get
            {
                int x, y;
                SDL.GetWindowPosition(sdlHandle, &x, &y);
                return new Point(x, y);
            }
            set
            {
                SDL.SetWindowPosition(sdlHandle, value.X, value.Y);
            }
        }

        /// <summary>
        /// Text of the title of the Window.
        /// </summary>
        public string Text
        {
            get { return SDL.GetWindowTitleS(sdlHandle); }
            set { SDL.SetWindowTitle(sdlHandle, value); }
        }

        /// <summary>
        /// Style of border. Currently can only be Sizable or FixedSingle.
        /// </summary>
        public FormBorderStyle FormBorderStyle
        {
            get
            {
                uint flags = SDL.GetWindowFlags(sdlHandle);
                var isResizeable = (flags & (uint)WindowFlags.WindowResizable) != 0;
                var isBorderless = (flags & (uint)WindowFlags.WindowBorderless) != 0;
                if (isBorderless)
                {
                    return FormBorderStyle.None;
                }
                else
                {
                    return isResizeable ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;
                }
            }
            set
            {
                SDL.SetWindowBordered(sdlHandle, value == FormBorderStyle.None ? SdlBool.False : SdlBool.True);
                SDL.SetWindowResizable(sdlHandle, value == FormBorderStyle.Sizable ? SdlBool.True : SdlBool.False);
            }
        }

        public void SetRelativeMouseMode(bool enabled)
        {
            SDL.SetRelativeMouseMode(enabled ? SdlBool.True : SdlBool.False);
        }

        // Events that one can hook up.
        public delegate void MouseButtonDelegate(MouseButtonEvent e);
        public delegate void MouseMoveDelegate(MouseMotionEvent e);
        public delegate void MouseWheelDelegate(MouseWheelEvent e);
        public delegate void TextEditingDelegate(TextEditingEvent e);
        public delegate void TextInputDelegate(TextInputEvent e);
        public delegate void WindowEventDelegate(WindowEvent e);
        public delegate void KeyDelegate(KeyboardEvent e);
        public delegate void JoystickDeviceChangedDelegate(int which);
        public delegate void TouchFingerDelegate(TouchFingerEvent e);
        public delegate void NotificationDelegate();
        public delegate void DropEventDelegate(string content);

        public event MouseButtonDelegate PointerButtonPressActions;
        public event MouseButtonDelegate PointerButtonReleaseActions;
        public event MouseWheelDelegate MouseWheelActions;
        public event MouseMoveDelegate MouseMoveActions;
        public event KeyDelegate KeyDownActions;
        public event KeyDelegate KeyUpActions;
        public event TextEditingDelegate TextEditingActions;
        public event TextInputDelegate TextInputActions;
        public event NotificationDelegate CloseActions;
        public event JoystickDeviceChangedDelegate JoystickDeviceAdded;
        public event JoystickDeviceChangedDelegate JoystickDeviceRemoved;
        public event TouchFingerDelegate FingerMoveActions;
        public event TouchFingerDelegate FingerPressActions;
        public event TouchFingerDelegate FingerReleaseActions;
        public event WindowEventDelegate ResizeBeginActions;
        public event WindowEventDelegate ResizeEndActions;
        public event WindowEventDelegate ActivateActions;
        public event WindowEventDelegate DeActivateActions;
        public event WindowEventDelegate MinimizedActions;
        public event WindowEventDelegate MaximizedActions;
        public event WindowEventDelegate RestoredActions;
        public event WindowEventDelegate MouseEnterActions;
        public event WindowEventDelegate MouseLeaveActions;
        public event WindowEventDelegate FocusGainedActions;
        public event WindowEventDelegate FocusLostActions;
        public event DropEventDelegate DropFileActions;

        /// <summary>
        /// Process events for the current window
        /// </summary>
        public virtual void ProcessEvent(Event e)
        {
            switch ((EventType)e.Type)
            {
                case EventType.Quit:
                        // When SDL sends a SDL_QUIT message, we have actually clicked on the close button.
                    CloseActions?.Invoke();
                    break;

                case EventType.Mousebuttondown:
                    PointerButtonPressActions?.Invoke(e.Button);
                    break;

                case EventType.Mousebuttonup:
                    PointerButtonReleaseActions?.Invoke(e.Button);
                    break;

                case EventType.Mousemotion:
                    MouseMoveActions?.Invoke(e.Motion);
                    break;

                case EventType.Mousewheel:
                    MouseWheelActions?.Invoke(e.Wheel);
                    break;

                case EventType.Keydown:
                    KeyDownActions?.Invoke(e.Key);
                    break;

                case EventType.Keyup:
                    KeyUpActions?.Invoke(e.Key);
                    break;

                case EventType.Textediting:
                    TextEditingActions?.Invoke(e.Edit);
                    break;

                case EventType.Textinput:
                    TextInputActions?.Invoke(e.Text);
                    break;

                case EventType.Joydeviceadded:
                    JoystickDeviceAdded?.Invoke(e.Jdevice.Which);
                    break;

                case EventType.Joydeviceremoved:
                    JoystickDeviceRemoved?.Invoke(e.Jdevice.Which);
                    break;

                case EventType.Fingermotion:
                    FingerMoveActions?.Invoke(e.Tfinger);
                    break;

                case EventType.Fingerdown:
                    FingerPressActions?.Invoke(e.Tfinger);
                    break;

                case EventType.Fingerup:
                    FingerReleaseActions?.Invoke(e.Tfinger);
                    break;
                
                case EventType.Dropfile:
                    DropFileActions?.Invoke(Silk.NET.Core.Native.SilkMarshal.PtrToString((IntPtr)e.Drop.File, Silk.NET.Core.Native.NativeStringEncoding.UTF8));
                    break;

                case EventType.Windowevent:
                {
                    switch ((WindowEventID)e.Window.Event)
                    {
                        case WindowEventID.WindoweventSizeChanged:
                            ResizeBeginActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventResized:
                            ResizeEndActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventClose:
                            CloseActions?.Invoke();
                            break;

                        case WindowEventID.WindoweventShown:
                            ActivateActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventHidden:
                            DeActivateActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventMinimized:
                            MinimizedActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventMaximized:
                            MaximizedActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventRestored:
                            RestoredActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventEnter:
                            MouseEnterActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventLeave:
                            MouseLeaveActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventFocusGained:
                            FocusGainedActions?.Invoke(e.Window);
                            break;

                        case WindowEventID.WindoweventFocusLost:
                            FocusLostActions?.Invoke(e.Window);
                            break;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Platform specific handle for Window:
        /// - On Windows: the HWND of the window
        /// - On Unix: the Window ID (XID). Note that on Unix, the value is 32-bit (See X11/X.h for the typedef of XID).
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Display of current Window (valid only for Unix for X11).
        /// </summary>
        public IntPtr Display { get; private set; }

        /// <summary>
        /// Surface of current Window (valid only for Android).
        /// </summary>
        public IntPtr Surface { get; private set; }

        /// <summary>
        /// The SDL window handle.
        /// </summary>
        public IntPtr SdlHandle => (IntPtr)sdlHandle;

        /// <summary>
        /// Is the Window still alive?
        /// </summary>
        public bool Exists
        {
            get { return SdlHandle != IntPtr.Zero; }
        }

#region Disposal
        ~Window()
        {
            Dispose(false);
        }

        /// <summary>
        /// Have we already disposed of the current object?
        /// </summary>
        public bool IsDisposed
        {
            get { return SdlHandle == IntPtr.Zero; }
        }

        /// <summary>
        /// Actions to be called when we dispose of current.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Dispose of current Window.
        /// </summary>
        /// <param name="disposing">If <c>false</c> we are being called from the Finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (sdlHandle != null)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    Disposed?.Invoke(this, EventArgs.Empty);
                    Application.UnregisterWindow(this);
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                SDL.DestroyWindow(sdlHandle);
                sdlHandle = null;
                Handle = IntPtr.Zero;
            }
        }
  
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
                // Performance improvement to avoid being called a second time by the GC.
            GC.SuppressFinalize(this);
        }
#endregion
    }
}
#endif
