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

            // Create SDL window using child
            var _sdlWindow = new Stride.Graphics.SDL.Window("Embedded Stride Window", panel1.Handle);
            var context = new GameContextSDL(_sdlWindow);

            // Start the game
            Game _game = new();
            Task.Run(() =>
            {
                _game.Run(context); // This causes the form to fail :-(
            });
        }
    }
}
