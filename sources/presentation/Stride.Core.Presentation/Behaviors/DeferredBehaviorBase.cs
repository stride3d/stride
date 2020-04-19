// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// A <see cref="Behavior{T}"/> that support deferred attachement for a FrameworkElement derived class.
    /// In such a case, the attachement is delayed until the <see cref="FrameworkElement.Loaded"/> event is raised.
    /// </summary>
    /// <typeparam name="T">The type of instance to attach to.</typeparam>
    public abstract class DeferredBehaviorBase<T> : Behavior<T> where T : DependencyObject
    {
        /// <summary>
        /// Represents the <see cref="AttachOnEveryLoadedEvent"/> property.
        /// </summary>
        public static readonly DependencyProperty AttachOnEveryLoadedEventProperty =
            DependencyProperty.Register(nameof(AttachOnEveryLoadedEvent), typeof(bool), typeof(DeferredBehaviorBase<T>), new PropertyMetadata(BooleanBoxes.FalseBox));

        private bool currentlyLoaded;

        /// <summary>
        /// Gets or sets whether <see cref="OnAttachedAndLoaded"/> should be called each time the <see cref="FrameworkElement.Loaded"/> event is raised.
        /// </summary>
        public bool AttachOnEveryLoadedEvent { get { return (bool)GetValue(AttachOnEveryLoadedEventProperty); } set { SetValue(AttachOnEveryLoadedEventProperty, value.Box()); } }

        protected override void OnAttached()
        {
            base.OnAttached();

            var element = AssociatedObject as FrameworkElement;

            if (element != null)
            {
                element.Loaded += AssociatedObjectLoaded;
                element.Unloaded += AssociatedObjectUnloaded;
            }

            if (element == null || element.IsLoaded)
            {
                currentlyLoaded = true;
                OnAttachedAndLoaded();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (currentlyLoaded)
            {
                currentlyLoaded = false;
                OnDetachingAndUnloaded();
            }

            var element = AssociatedObject as FrameworkElement;
            if (element != null)
            {
                element.Loaded -= AssociatedObjectLoaded;
                element.Unloaded -= AssociatedObjectUnloaded;
            }
        }

        protected virtual void OnAttachedAndLoaded()
        {
            // Intentionally does nothing
        }

        protected virtual void OnDetachingAndUnloaded()
        {
            // Intentionally does nothing
        }

        private void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            if (!currentlyLoaded)
            {
                currentlyLoaded = true;
                OnAttachedAndLoaded();
            }
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            if (currentlyLoaded)
            {
                currentlyLoaded = false;
                OnDetachingAndUnloaded();
            }
        }
    }
}
