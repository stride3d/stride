using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Stride.Core.Presentation.Controls;
using Stride.Engine;
using Stride.Games;

namespace ParticlesSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_game is not null)
            {
                _game.Dispose();
                _game = null;
            }

            if (_sdlWindow is not null)
            {
                _sdlWindow.Dispose();
                _sdlWindow = null;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var host = Window.GetWindow(this);
            WindowInteropHelper wih = new(host);
            IntPtr hwnd = wih.Handle;
            Stride.Graphics.SDL.Window sdlWindow = new("Embedded Stride Window", hwnd);
            contentCtrl.Content = new GameEngineHost(sdlWindow.Handle);

            GameContextSDL context = new(sdlWindow);
            _game = new Game();
            _game.Run(context);
        }

        private Game? _game;
        Stride.Graphics.SDL.Window? _sdlWindow;
    }
}
