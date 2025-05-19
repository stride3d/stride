// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.GameStudio.Avalonia.Settings;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs _)
    {
        // Size the window to best fit the current screen size
        InitializeWindowSize();
        return;

        void InitializeWindowSize()
        {
            var previousWorkAreaWidth = GameStudioInternalSettings.WorkAreaWidth.GetValue();
            var previousWorkAreaHeight = GameStudioInternalSettings.WorkAreaHeight.GetValue();
            var wasWindowMaximized = GameStudioInternalSettings.WindowMaximized.GetValue();
            var workArea = GameStudioInternalSettings.GetWorkArea();

            if (wasWindowMaximized || previousWorkAreaWidth > workArea.Width || previousWorkAreaHeight > workArea.Height)
            {
                // Resolution has changed (and is now smaller), let's make the window fill all available space.
                FillArea(workArea);
                WindowState = WindowState.Maximized;
            }
            else
            {
                // Load state
                var previousWindowWidth = GameStudioInternalSettings.WindowWidth.GetValue();
                var previousWindowHeight = GameStudioInternalSettings.WindowHeight.GetValue();
                // Set window size
                Width = Math.Min(previousWindowWidth, workArea.Width);
                Height = Math.Min(previousWindowHeight, workArea.Height);
                // Window is centered by default
                CenterToArea(workArea);
                WindowState = WindowState.Normal;
            }

            return;

            void CenterToArea(Rect area)
            {
                Position = new PixelPoint((int)Math.Abs(area.Width - Width) / 2, (int)Math.Abs(area.Height - Height) / 2) + new PixelPoint((int)area.Position.X, (int)area.Position.Y);
            }

            void FillArea(Rect area)
            {
                Width = area.Width;
                Height = area.Height;
                Position = new PixelPoint((int)area.Position.X, (int)area.Position.Y);
            }
        }
    }
}
