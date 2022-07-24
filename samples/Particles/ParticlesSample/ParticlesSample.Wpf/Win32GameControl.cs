using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Stride.Core.Presentation.Controls;
using Stride.Engine;
using Stride.Games;

namespace ParticlesSample
{
    internal class Win32GameControl : ContentControl
    {
        public Win32GameControl()
        {
            Loaded += Win32GameHost_Loaded;
            Unloaded += Win32GameHost_Unloaded;
        }

        private void Win32GameHost_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the handle for the content window
            var host = Window.GetWindow(this);
            if (host is null) return;
            var wih = new WindowInteropHelper(host);
            var hostHandle = wih.Handle;

            // Create a child window in which to host the game
            var className = GetType().Name;
            var wndClass = new NativeMethods.WndClassEx();
            wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
            wndClass.hInstance = NativeMethods.GetModuleHandle(null);
            wndClass.lpfnWndProc = NativeMethods.DefaultWindowProc;
            wndClass.lpszClassName = className;
            // If this is not null, the cursor is drawn whenever the mouse is moved.
            wndClass.hCursor = IntPtr.Zero;
            NativeMethods.RegisterClassEx(ref wndClass);

            var childHandle = NativeMethods.CreateWindowEx(
                0,
                className,
                "",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE,
                0,
                0,
                (int)Width, (int)Height,
                hostHandle,
                IntPtr.Zero, IntPtr.Zero, 0);

            // Create SDL window using child
            _sdlWindow = new Stride.Graphics.SDL.Window("Embedded Stride Window", childHandle);
            Content = new GameEngineHost(childHandle);
            var context = new GameContextSDL(_sdlWindow);

            // Start the game
            _game = new();
            _game.Run(context);
        }

        private void Win32GameHost_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_game is not null)
            {
                _game.Dispose();
                _game = null;
            }

            if (Content is not null)
            {
                (Content as IDisposable)?.Dispose();
                Content = null;
            }

            if (_sdlWindow is not null)
            {
                _sdlWindow.Dispose();
                _sdlWindow = null;
            }
        }

        private Game? _game;
        private Stride.Graphics.SDL.Window? _sdlWindow;
    }
}
