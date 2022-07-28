using System.Runtime.InteropServices;
using Stride.Engine;
using Stride.Games;

namespace ParticlesSample
{
    public class Win32GameControl : Control
    {
        public Win32GameControl()
        {
            var hostHandle = this.Handle;

            // Create a child window in which to host the game
            var className = GetType().Name;
            var wndClass = new ParticlesSample.NativeMethods.WndClassEx();
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
                this.Handle,
                IntPtr.Zero, IntPtr.Zero, 0);

            // Create SDL window using child
            _sdlWindow = new Stride.Graphics.SDL.Window("Embedded Stride Window", childHandle);
            var context = new GameContextSDL(_sdlWindow);

            // Start the game
            _game = new();
            //_game.Run(context);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = CreateGraphics();
            graphics.FillRectangle(Brushes.Blue, ClientRectangle);
        }

        private Game? _game;
        private Stride.Graphics.SDL.Window? _sdlWindow;
    }
}
