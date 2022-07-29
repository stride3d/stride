using System.Runtime.InteropServices;
using Stride.Engine;
using Stride.Games;

namespace ParticlesSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Create SDL window using panel
            _sdlWindow = new Stride.Graphics.SDL.Window("Embedded Stride Window", panel1.Handle);
            var context = new GameContextSDL(_sdlWindow);
            
            // Start the game
            _game = new();
            Task.Factory.StartNew(() =>
            {
                // Must move this off current thread or the form will hang.
                _game.Run(context);
            }, TaskCreationOptions.LongRunning);
        }

        private void Panel1_Layout(object? sender, LayoutEventArgs e)
        {
            if (sender is Control control)
            {
                _sdlWindow.Size = new(control.Width, control.Height);
            }
        }

        private Game _game;
        private Stride.Graphics.SDL.Window _sdlWindow;
    }
}
