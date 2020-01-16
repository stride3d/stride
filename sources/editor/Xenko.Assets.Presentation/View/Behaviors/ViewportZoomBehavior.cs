// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Xenko.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    class ViewportZoomBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty ModifiersProperty =
               DependencyProperty.Register(nameof(Modifiers), typeof(ModifierKeys), typeof(ViewportZoomBehavior), new PropertyMetadata(ModifierKeys.Control));

        public static readonly DependencyProperty ViewportProperty =
               DependencyProperty.Register(nameof(Viewport), typeof(ViewportViewModel), typeof(ViewportZoomBehavior));
        
        public ModifierKeys Modifiers
        {
            get { return (ModifierKeys)GetValue(ModifiersProperty); }
            set { SetValue(ModifiersProperty, value); }
        }
        public ViewportViewModel Viewport
        {
            get { return (ViewportViewModel)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Viewport == null || !Keyboard.Modifiers.HasFlag(Modifiers))
                return;

            var position = e.MouseDevice.GetPosition((IInputElement)sender);
            if (e.Delta > 0)
            {
                Viewport.ZoomIn(position.X, position.Y);
            }
            if (e.Delta < 0)
            {
                Viewport.ZoomOut(position.X, position.Y);
            }
        }
    }
}
