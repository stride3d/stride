using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NuGet.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Stride.NuGetResolver
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            this.InitializeComponent();
        }

        public void AppendMessage(LogLevel level, string message)
        {
            if (level == LogLevel.Error)
            {
                CloseButton.Visibility = Visibility.Visible;
                Message.Text = "Error restoring NuGet packages!";
                Message.Foreground = new SolidColorBrush(Colors.Red);
            }
            Log.AppendText($"[{level}] {message}{Environment.NewLine}");
            Log.ScrollToEnd();
        }

        public void CloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
