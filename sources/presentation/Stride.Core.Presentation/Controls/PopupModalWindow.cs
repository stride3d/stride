// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// A window that show at the mouse cursor location, has no title bar, and is closed (with <see cref="Services.DialogResult.Cancel"/> result) when the
    /// user clicks outside of it or presses Escape.
    /// </summary>
    /// <remarks>
    /// This window will capture mouse. When handling mouse events, <see cref="IsMouseOverWindow"/> can be used to check whether the mouse event
    /// occurred inside the window.
    /// </remarks>
    public abstract class PopupModalWindow : ModalWindow
    {
        private bool closing;

        protected PopupModalWindow()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!IsMouseCaptured)
                Mouse.Capture(this, CaptureMode.SubTree);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var titleBar = GetTemplateChild("TitleBar") as UIElement;
            if (titleBar != null)
                titleBar.Visibility = Visibility.Collapsed;
        }

        public override Task<DialogResult> ShowModal()
        {
            WindowManager.ShowAtCursorPosition(this);
            return base.ShowModal();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                CloseWithCancel();
                e.Handled = true;
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            CloseWithCancel();
        }

        protected override void OnClosing([NotNull] CancelEventArgs e)
        {
            if (!e.Cancel)
                closing = true;

            base.OnClosing(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (!IsMouseOverWindow(e))
            {
                CloseWithCancel();
                e.Handled = true;
            }
        }

        private void CloseWithCancel()
        {
            if (!closing)
            {
                Result = Services.DialogResult.Cancel;
                Close();
            }
        }

        protected bool IsMouseOverWindow([NotNull] MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            return position.X >= 0 && position.Y >= 0 && position.X < ActualWidth && position.Y < ActualHeight;
        }
    }
}
