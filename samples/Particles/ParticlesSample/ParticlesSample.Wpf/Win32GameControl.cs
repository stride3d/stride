using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Stride.Core.Presentation.Controls;
using Stride.Engine;
using Stride.Games;

namespace ParticlesSample
{
    internal class Win32GameControl : FrameworkElement
    {
        public Win32GameControl()
        {
            Loaded += Win32GameHost_Loaded;
            Unloaded += Win32GameHost_Unloaded;
        }

        protected override int VisualChildrenCount => _gameEngineHost is null ? 0 : 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && _gameEngineHost is not null) return _gameEngineHost;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _gameEngineHost?.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_gameEngineHost is not null)
            {
                _gameEngineHost.Measure(availableSize);
                return _gameEngineHost.DesiredSize;
            }

            return availableSize;
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
                (int)ActualWidth, (int)ActualHeight,
                hostHandle,
                IntPtr.Zero, IntPtr.Zero, 0);

            // Create SDL window using child
            _sdlWindow = new Stride.Graphics.SDL.Window("Embedded Stride Window", childHandle);
            _gameEngineHost = new GameEngineHost(childHandle);
            AddVisualChild(_gameEngineHost);
            InvalidateMeasure(); // force refresh
            var context = new GameContextSDL(_sdlWindow, _sdlWindow.Size.Width, _sdlWindow.Size.Height);

            // Start the game
            _game = new();
            Task.Factory.StartNew(() =>
            {
                // Running the game in its own task allows rendering while
                // dragging and resizing the window.
                _game.Run(context);
            }, TaskCreationOptions.LongRunning);
        }

        private void Win32GameHost_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_game is not null)
            {
                _game.Dispose();
                _game = null;
            }

            if (_gameEngineHost is not null)
            {
                RemoveVisualChild(_gameEngineHost);
                _gameEngineHost.Dispose();
                _gameEngineHost = null;
            }

            if (_sdlWindow is not null)
            {
                _sdlWindow.Dispose();
                _sdlWindow = null;
            }
        }

        private Game? _game;
        private GameEngineHost? _gameEngineHost;
        private Stride.Graphics.SDL.Window? _sdlWindow;
    }
}
