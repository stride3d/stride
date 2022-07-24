using System.Threading.Tasks;
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
            var context = new GameContextSDL(_window);

            _game = new Game();
            _renderTask = new Task(() => { _game.Run(context); }, TaskCreationOptions.LongRunning);
            _renderTask.Start();
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
            _renderTask?.Wait();
            _window?.Dispose();

            _renderTask = null;
            _game = null;
            _window = null;
        }

        private Game? _game;
        private Task? _renderTask;
        private Window? _window;
    }
}
