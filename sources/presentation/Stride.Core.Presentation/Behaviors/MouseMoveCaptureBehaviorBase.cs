// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.Behaviors
{
    /// <summary>
    /// Base class for behaviors that capture the mouse.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public abstract class MouseMoveCaptureBehaviorBase<TElement> : Behavior<TElement>
        where TElement : UIElement
    {
        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(MouseMoveCaptureBehaviorBase<TElement>), new PropertyMetadata(BooleanBoxes.TrueBox, IsEnabledChanged));

        /// <summary>
        /// Identifies the <see cref="IsInProgress"/> dependency property key.
        /// </summary>
        protected static readonly DependencyPropertyKey IsInProgressPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsInProgress), typeof(bool), typeof(MouseMoveCaptureBehaviorBase<TElement>), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Identifies the <see cref="IsInProgress"/> dependency property.
        /// </summary>
        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        public static readonly DependencyProperty IsInProgressProperty = IsInProgressPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="Modifiers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register(nameof(Modifiers), typeof(ModifierKeys?), typeof(MouseMoveCaptureBehaviorBase<TElement>), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="UsePreviewEvents"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UsePreviewEventsProperty =
            DependencyProperty.Register(nameof(UsePreviewEvents), typeof(bool), typeof(MouseMoveCaptureBehaviorBase<TElement>), new PropertyMetadata(BooleanBoxes.FalseBox, UsePreviewEventsChanged));
        
        /// <summary>
        /// <c>true</c> if this behavior is enabled; otherwise, <c>false</c>.
        /// </summary>
        public bool IsEnabled { get { return (bool)GetValue(IsEnabledProperty); } set { SetValue(IsEnabledProperty, value.Box()); } }

        /// <summary>
        /// <c>true</c> if an operation is in progress; otherwise, <c>false</c>.
        /// </summary>
        public bool IsInProgress { get { return (bool)GetValue(IsInProgressProperty); } private set { SetValue(IsInProgressPropertyKey, value.Box()); } }

        public ModifierKeys? Modifiers { get { return (ModifierKeys?)GetValue(ModifiersProperty); } set { SetValue(ModifiersProperty, value); } }

        public bool UsePreviewEvents
        {
            get { return (bool)GetValue(UsePreviewEventsProperty); }
            set { SetValue(UsePreviewEventsProperty, value.Box()); }
        }

        private static void IsEnabledChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MouseMoveCaptureBehaviorBase<TElement>)d;
            if ((bool)e.NewValue != true)
            {
                behavior.Cancel();
            }
        }

        private static void UsePreviewEventsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MouseMoveCaptureBehaviorBase<TElement>)d;
            behavior.UnsubscribeFromMouseEvents((bool)e.OldValue);
            behavior.SubscribeToMouseEvents((bool)e.NewValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool AreModifiersValid()
        {
            return Modifiers == null || (Modifiers == ModifierKeys.None ? Keyboard.Modifiers == ModifierKeys.None : Keyboard.Modifiers.HasFlag(Modifiers));
        }

        protected void Cancel()
        {
            if (!IsInProgress)
                return;

            ReleaseMouseCapture();
            CancelOverride();
        }

        protected virtual void CancelOverride()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Captures the mouse to the <see cref="Behavior{TElement}.AssociatedObject"/>.
        /// </summary>
        protected void CaptureMouse()
        {
            AssociatedObject.Focus();
            AssociatedObject.CaptureMouse();
            IsInProgress = true;
        }

        ///  <inheritdoc/>
        protected override void OnAttached()
        {
            SubscribeToMouseEvents(UsePreviewEvents);
            AssociatedObject.PreviewMouseUp += MouseUp;
            AssociatedObject.LostMouseCapture += OnLostMouseCapture;
        }

        ///  <inheritdoc/>
        protected override void OnDetaching()
        {
            UnsubscribeFromMouseEvents(UsePreviewEvents);
            AssociatedObject.PreviewMouseUp -= MouseUp;
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
        }

        protected abstract void OnMouseDown([NotNull] MouseButtonEventArgs e);

        protected abstract void OnMouseMove([NotNull] MouseEventArgs e);

        protected abstract void OnMouseUp([NotNull] MouseButtonEventArgs e);

        /// <summary>
        /// Releases the mouse capture, if the <see cref="Behavior{TElement}.AssociatedObject"/> held the capture. 
        /// </summary>
        protected void ReleaseMouseCapture()
        {
            IsInProgress = false;
            if (AssociatedObject.IsMouseCaptured)
            {
                AssociatedObject.ReleaseMouseCapture();
            }
        }

        private void MouseDown(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (!IsEnabled || IsInProgress)
                return;

            OnMouseDown(e);
        }

        private void MouseMove(object sender, [NotNull] MouseEventArgs e)
        {
            if (!IsEnabled || !IsInProgress)
                return;

            OnMouseMove(e);
        }

        private void MouseUp(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (!IsEnabled || !IsInProgress || !AssociatedObject.IsMouseCaptured)
                return;

            OnMouseUp(e);
        }

        private void OnLostMouseCapture(object sender, [NotNull] MouseEventArgs e)
        {
            if (!ReferenceEquals(Mouse.Captured, sender))
            {
                Cancel();
            }
        }

        private void SubscribeToMouseEvents(bool usePreviewEvents)
        {
            if (AssociatedObject == null)
                return;

            if (usePreviewEvents)
            {
                AssociatedObject.PreviewMouseDown += MouseDown;
                AssociatedObject.PreviewMouseMove += MouseMove;
            }
            else
            {
                AssociatedObject.MouseDown += MouseDown;
                AssociatedObject.MouseMove += MouseMove;
            }
        }

        private void UnsubscribeFromMouseEvents(bool usePreviewEvents)
        {
            if (AssociatedObject == null)
                return;

            if (usePreviewEvents)
            {
                AssociatedObject.PreviewMouseDown -= MouseDown;
                AssociatedObject.PreviewMouseMove -= MouseMove;
            }
            else
            {
                AssociatedObject.MouseDown -= MouseDown;
                AssociatedObject.MouseMove -= MouseMove;
            }
        }
    }
}
