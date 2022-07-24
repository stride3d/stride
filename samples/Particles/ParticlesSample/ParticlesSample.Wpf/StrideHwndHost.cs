using Silk.NET.SDL;
using Stride.Engine;
using Stride.Games;
using Win32Host;
using Window = Stride.Graphics.SDL.Window;

namespace ParticlesSample
{
    public class StrideHwndHost : Win32HwndHost
    {
        protected unsafe override void InitializeHostedContent()
        {
            _window = new Window("Stride Window", Hwnd);
            _game = new Game();

            var context = new GameContextSDL(_window);
            _game.Run(context);
        }

        protected unsafe override void ResizeHostedContent()
        {
            if (_window is not null)
            {
                var area = this.GetScaledWindowSize();
                var windowPtr = (Silk.NET.SDL.Window*)_window.SdlHandle.ToPointer();
                Sdl.GetApi().SetWindowSize(windowPtr, (int)area.Width, (int)area.Height);
            }
        }

        protected override void UninitializeHostedContent()
        {
            _game?.Dispose();
            _game = null;

            _window?.Dispose();
            _window = null;
        }

        private Game? _game;
        private Window? _window;
    }
}
