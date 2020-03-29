// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.Behaviors
{
    public enum MouseEventType
    {
        None,
        MouseDown,
        MouseUp,
        MouseMove,
        MouseLeftButtonDown,
        MouseLeftButtonUp,
        MouseRightButtonDown,
        MouseRightButtonUp,
        PreviewMouseDown,
        PreviewMouseUp,
        PreviewMouseMove,
        PreviewMouseLeftButtonDown,
        PreviewMouseLeftButtonUp,
        PreviewMouseRightButtonDown,
        PreviewMouseRightButtonUp,
    }

    public class OnMouseEventBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty EventTypeProperty = DependencyProperty.Register(nameof(EventType), typeof(MouseEventType), typeof(OnMouseEventBehavior), new FrameworkPropertyMetadata(MouseEventType.None, EventTypeChanged));

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(OnMouseEventBehavior));

        /// <summary>
        /// Identifies the <see cref="HandleEvent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HandleEventProperty = DependencyProperty.Register(nameof(HandleEvent), typeof(bool), typeof(OnMouseEventBehavior));

        /// <summary>
        /// Identifies the <see cref="Modifiers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ModifiersProperty =
               DependencyProperty.Register(nameof(Modifiers), typeof(ModifierKeys?), typeof(OnMouseEventBehavior), new PropertyMetadata(null));

        public MouseEventType EventType { get { return (MouseEventType)GetValue(EventTypeProperty); } set { SetValue(EventTypeProperty, value); } }

        /// <summary>
        /// Gets or sets the command to invoke when the event is raised.
        /// </summary>
        public ICommand Command { get { return (ICommand)GetValue(CommandProperty); } set { SetValue(CommandProperty, value); } }

        /// <summary>
        /// Gets or sets whether to set the event as handled.
        /// </summary>
        public bool HandleEvent { get { return (bool)GetValue(HandleEventProperty); } set { SetValue(HandleEventProperty, value.Box()); } }

        public ModifierKeys? Modifiers { get { return (ModifierKeys?)GetValue(ModifiersProperty); } set { SetValue(ModifiersProperty, value); } }

        protected bool AreModifiersValid()
        {
            if (Modifiers == null)
                return true;
            return Modifiers == ModifierKeys.None ? Keyboard.Modifiers == ModifierKeys.None : Keyboard.Modifiers.HasFlag(Modifiers);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            RegisterHandler(EventType);
        }

        protected override void OnDetaching()
        {
            UnregisterHandler(EventType);
            base.OnAttached();
        }

        private static void EventTypeChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (OnMouseEventBehavior)d;
            if (behavior.AssociatedObject == null)
                return;

            var oldValue = (MouseEventType)e.OldValue;
            behavior.UnregisterHandler(oldValue);
            var newValue = (MouseEventType)e.NewValue;
            behavior.RegisterHandler(newValue);
        }

        private void RegisterHandler(MouseEventType type)
        {
            switch (type)
            {
                case MouseEventType.MouseDown:
                    AssociatedObject.MouseDown += MouseButtonHandler;
                    break;
                case MouseEventType.MouseUp:
                    AssociatedObject.MouseUp += MouseButtonHandler;
                    break;
                case MouseEventType.MouseMove:
                    AssociatedObject.MouseMove += MouseMoveHandler;
                    break;
                case MouseEventType.MouseLeftButtonDown:
                    AssociatedObject.MouseLeftButtonDown += MouseMoveHandler;
                    break;
                case MouseEventType.MouseLeftButtonUp:
                    AssociatedObject.MouseLeftButtonUp += MouseMoveHandler;
                    break;
                case MouseEventType.MouseRightButtonDown:
                    AssociatedObject.MouseRightButtonDown += MouseMoveHandler;
                    break;
                case MouseEventType.MouseRightButtonUp:
                    AssociatedObject.MouseRightButtonUp += MouseMoveHandler;
                    break;
                case MouseEventType.PreviewMouseDown:
                    AssociatedObject.PreviewMouseDown += MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseUp:
                    AssociatedObject.PreviewMouseUp += MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseMove:
                    AssociatedObject.PreviewMouseMove += MouseMoveHandler;
                    break;
                case MouseEventType.PreviewMouseLeftButtonDown:
                    AssociatedObject.PreviewMouseLeftButtonDown += MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseLeftButtonUp:
                    AssociatedObject.PreviewMouseLeftButtonUp += MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseRightButtonDown:
                    AssociatedObject.PreviewMouseRightButtonDown += MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseRightButtonUp:
                    AssociatedObject.PreviewMouseRightButtonUp += MouseButtonHandler;
                    break;
            }
        }

        private void UnregisterHandler(MouseEventType type)
        {
            switch (type)
            {
                case MouseEventType.MouseDown:
                    AssociatedObject.MouseDown -= MouseButtonHandler;
                    break;
                case MouseEventType.MouseUp:
                    AssociatedObject.MouseUp -= MouseButtonHandler;
                    break;
                case MouseEventType.MouseMove:
                    AssociatedObject.MouseMove -= MouseMoveHandler;
                    break;
                case MouseEventType.MouseLeftButtonDown:
                    AssociatedObject.MouseLeftButtonDown -= MouseMoveHandler;
                    break;
                case MouseEventType.MouseLeftButtonUp:
                    AssociatedObject.MouseLeftButtonUp -= MouseMoveHandler;
                    break;
                case MouseEventType.MouseRightButtonDown:
                    AssociatedObject.MouseRightButtonDown -= MouseMoveHandler;
                    break;
                case MouseEventType.MouseRightButtonUp:
                    AssociatedObject.MouseRightButtonUp -= MouseMoveHandler;
                    break;
                case MouseEventType.PreviewMouseDown:
                    AssociatedObject.PreviewMouseDown -= MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseUp:
                    AssociatedObject.PreviewMouseUp -= MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseMove:
                    AssociatedObject.PreviewMouseMove -= MouseMoveHandler;
                    break;
                case MouseEventType.PreviewMouseLeftButtonDown:
                    AssociatedObject.PreviewMouseLeftButtonDown -= MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseLeftButtonUp:
                    AssociatedObject.PreviewMouseLeftButtonUp -= MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseRightButtonDown:
                    AssociatedObject.PreviewMouseRightButtonDown -= MouseButtonHandler;
                    break;
                case MouseEventType.PreviewMouseRightButtonUp:
                    AssociatedObject.PreviewMouseRightButtonUp -= MouseButtonHandler;
                    break;
            }
        }

        private void MouseButtonHandler(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (!AreModifiersValid())
                return;

            MouseMoveHandler(sender, e);
        }

        private void MouseMoveHandler(object sender, [NotNull] MouseEventArgs e)
        {
            if (!AreModifiersValid())
                return;

            if (HandleEvent)
            {
                e.Handled = true;
            }
            var cmd = Command;
            var position = e.GetPosition(AssociatedObject);
            if (cmd != null && cmd.CanExecute(position))
                cmd.Execute(position);
        }
    }
}
