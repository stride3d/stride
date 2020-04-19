// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

namespace Stride.LauncherApp.Views
{
    /// <summary>
    /// Interaction logic for SelfUpdateWindow.xaml
    /// </summary>
    public partial class SelfUpdateWindow
    {
        /// <summary>
        /// Initialize new instance of a <see cref="SelfUpdateWindow"/>.
        /// </summary>
        public SelfUpdateWindow()
        {
            InitializeComponent();
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
            // Allow closing only when Exit button is enabled.
            Closing += (sender, e) => e.Cancel = !ExitButton.IsEnabled;
        }

        /// <summary>
        /// Prevents window from being closed during a critical section of the update process.
        /// </summary>
        public void LockWindow()
        {
            ExitButton.IsEnabled = false;
        }

        /// <summary>
        /// Forcibly close the update window.
        /// </summary>
        public void ForceClose()
        {
            ExitButton.IsEnabled = true;
            Close(); 
        }

        private void ExitButtonClicked(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
