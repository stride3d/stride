// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Controls;

namespace Stride.Assets.Presentation.View.Behaviors
{
    /// <summary>
    /// Shows the value of a slider in the scene while its thumb is dragged, and applies it once on release, so the drag
    /// is a single undo step from the start value to the end value. Canceled drags restore the preview without editing.
    /// Clicks and keys apply their value at once.
    /// </summary>
    /// <remarks>
    /// Only user input applies a value: a value set from code, such as the property grid refreshing the slider or the
    /// slider clamping a weight beyond its range, is not an edit and is not written back. The slider is bound one way
    /// from the text box, and this behavior copies its value to the text box before validating it.
    /// The editor sends each change to the scene game one after another, waiting for the game to apply it, which lags
    /// behind a drag: <see cref="Preview"/> writes the dragged values to the scene directly instead.
    /// </remarks>
    public sealed class LiveSliderEditBehavior : Behavior<Slider>
    {
        public static readonly DependencyProperty TextBoxProperty = DependencyProperty.Register(nameof(TextBox), typeof(NumericTextBox), typeof(LiveSliderEditBehavior));
        public static readonly DependencyProperty PreviewProperty = DependencyProperty.Register(nameof(Preview), typeof(Action<double>), typeof(LiveSliderEditBehavior));

        private bool dragging;
        private bool ignoreMouseUp;
        private double dragStartValue;

        /// <summary>The text box bound to the edited value, validated to apply the slider value.</summary>
        public NumericTextBox TextBox { get { return (NumericTextBox)GetValue(TextBoxProperty); } set { SetValue(TextBoxProperty, value); } }

        /// <summary>Shows a dragged value in the scene without applying it.</summary>
        public Action<double> Preview { get { return (Action<double>)GetValue(PreviewProperty); } set { SetValue(PreviewProperty, value); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)OnDragStarted);
            AssociatedObject.AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)OnDragCompleted);
            AssociatedObject.ValueChanged += OnValueChanged;
            AssociatedObject.KeyUp += OnKeyUp;
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)OnMouseDown, true);
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)OnMouseUp, true);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)OnDragStarted);
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)OnDragCompleted);
            AssociatedObject.ValueChanged -= OnValueChanged;
            AssociatedObject.KeyUp -= OnKeyUp;
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)OnMouseDown);
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)OnMouseUp);
            dragging = false;
            ignoreMouseUp = false;
            base.OnDetaching();
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            dragStartValue = TextBox?.Value ?? AssociatedObject.Value;
            ignoreMouseUp = false;
            dragging = true;
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            dragging = false;
            ignoreMouseUp = true;
            if (e.Canceled)
            {
                AssociatedObject.SetCurrentValue(Slider.ValueProperty, dragStartValue);
                Preview?.Invoke(dragStartValue);
                return;
            }
            Apply();
        }

        private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dragging)
                Preview?.Invoke(e.NewValue);
        }

        // A click on the track moves the thumb without a drag.
        private void OnMouseDown(object sender, MouseButtonEventArgs e) => ignoreMouseUp = false;

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Drag completion already committed or canceled the edit; its mouse-up must not apply it again.
            if (!dragging && !ignoreMouseUp)
                Apply();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.PageUp or Key.Down or Key.PageDown or Key.Left or Key.Right or Key.Home or Key.End)
                Apply();
        }

        private void Apply()
        {
            if (TextBox == null)
                return;
            TextBox.Value = AssociatedObject.Value;
            TextBox.Validate();
        }
    }
}
