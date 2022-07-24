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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the handle for the content window
            var host = Window.GetWindow(contentCtrl);
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
            contentCtrl.Content = new GameEngineHost(childHandle);
            var context = new GameContextSDL(_sdlWindow);

            // Start the game
            _game = new();
            _game.Run(context);
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

        private Game? _game;
        private Stride.Graphics.SDL.Window? _sdlWindow;
    }
}
