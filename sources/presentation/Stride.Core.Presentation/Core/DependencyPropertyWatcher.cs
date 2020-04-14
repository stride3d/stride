// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;

namespace Xenko.Core.Presentation.Core
{
    public class DependencyPropertyWatcher : IAttachedObject
    {
        private readonly List<Tuple<DependencyProperty, EventHandler>> handlers = new List<Tuple<DependencyProperty, EventHandler>>();
        private readonly Dictionary<DependencyProperty, DependencyPropertyDescriptor> descriptors = new Dictionary<DependencyProperty, DependencyPropertyDescriptor>();
        private FrameworkElement frameworkElement;

        private bool handlerRegistered;

        public DependencyPropertyWatcher()
        {
        }

        public DependencyPropertyWatcher([NotNull] FrameworkElement attachTo)
        {
            Attach(attachTo);
        }

        public DependencyObject AssociatedObject => frameworkElement;

        public void Attach([NotNull] DependencyObject dependencyObject)
        {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));
            if (ReferenceEquals(dependencyObject, frameworkElement))
                return;

            if (frameworkElement != null)
                throw new InvalidOperationException("A dependency object is already attached to this instance of DependencyPropertyWatcher.");
            frameworkElement = dependencyObject as FrameworkElement;

            if (frameworkElement == null)
                throw new ArgumentException("The dependency object to attach to the DependencyPropertyWatcher must be a FrameworkElement.");

            frameworkElement.Loaded += ElementLoaded;
            frameworkElement.Unloaded += ElementUnloaded;
            AttachHandlers();
        }

        public void Detach()
        {
            frameworkElement.Loaded -= ElementLoaded;
            frameworkElement.Unloaded -= ElementUnloaded;
            DetachHandlers();
            handlers.Clear();
            frameworkElement = null;
        }

        public void RegisterValueChangedHandler(DependencyProperty property, EventHandler handler)
        {
            handlers.Add(Tuple.Create(property, handler));
            if (handlerRegistered)
            {
                AttachHandler(property, handler);
            }
        }

        public void UnregisterValueChangedHander(DependencyProperty property, EventHandler handler)
        {
            handlers.RemoveWhere(x => x.Item1 == property && x.Item2 == handler);
            if (handlerRegistered)
            {
                DetachHandler(property, handler);
            }
        }

        private void AttachHandlers()
        {
            if (!handlerRegistered)
            {
                foreach (var handler in handlers)
                {
                    AttachHandler(handler.Item1, handler.Item2);
                }
                handlerRegistered = true;
            }
        }

        private void DetachHandlers()
        {
            if (handlerRegistered)
            {
                foreach (var handler in handlers)
                {
                    DetachHandler(handler.Item1, handler.Item2);
                }
                handlerRegistered = false;
            }
        }

        private void AttachHandler([NotNull] DependencyProperty property, [NotNull] EventHandler handler)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (frameworkElement == null) throw new InvalidOperationException("A dependency object must be attached in order to register a handler.");

            DependencyPropertyDescriptor descriptor;
            if (!descriptors.TryGetValue(property, out descriptor))
            {
                descriptor = DependencyPropertyDescriptor.FromProperty(property, AssociatedObject.GetType());
                descriptors.Add(property, descriptor);
            }
            descriptor.AddValueChanged(AssociatedObject, handler);
        }

        private void DetachHandler([NotNull] DependencyProperty property, [NotNull] EventHandler handler)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (frameworkElement == null) throw new InvalidOperationException("A dependency object must be attached in order to unregister a handler.");

            DependencyPropertyDescriptor descriptor;
            if (!descriptors.TryGetValue(property, out descriptor))
            {
                throw new InvalidOperationException("No handler was previously registered for this dependency property.");
            }
            descriptor.RemoveValueChanged(AssociatedObject, handler);
        }

        private void ElementLoaded(object sender, RoutedEventArgs e)
        {
            AttachHandlers();
        }

        private void ElementUnloaded(object sender, RoutedEventArgs e)
        {
            DetachHandlers();
        }
    }
}
