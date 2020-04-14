// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using Xenko.Core.Presentation.Extensions;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    using TextBoxBase = Presentation.Controls.TextBoxBase;

    public class ValidateTextBoxAfterSlidingBehavior : Behavior<Slider>
    {
        public static readonly DependencyProperty TextBoxProperty =
            DependencyProperty.Register(nameof(TextBox), typeof(TextBoxBase), typeof(ValidateTextBoxAfterSlidingBehavior));

        public TextBoxBase TextBox { get { return (TextBoxBase)GetValue(TextBoxProperty); } set { SetValue(TextBoxProperty, value); } }

        protected override void OnAttached()
        {
            AssociatedObject.AddHandler(Thumb.DragCompletedEvent, (RoutedEventHandler)OnDragCompleted);
            AssociatedObject.ValueChanged += OnValueChanged;
            AssociatedObject.KeyUp += OnKeyUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, (RoutedEventHandler)OnDragCompleted);
            AssociatedObject.ValueChanged -= OnValueChanged;
            AssociatedObject.KeyUp -= OnKeyUp;
            base.OnDetaching();
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AssociatedObject.FindVisualChildOfType<Thumb>()?.IsDragging ?? true)
                ValidateTextBox();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End)
                ValidateTextBox();
        }

        private void OnDragCompleted(object sender, EventArgs e)
        {
            ValidateTextBox();
        }

        private void ValidateTextBox()
        {
            TextBox?.Validate();
        }
    }
}
