// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;
using Xenko.Core;
using Xenko.Core.Presentation.Extensions;

namespace Xenko.Core.Presentation.Behaviors
{
    /// <summary>
    /// Represents a behavior capable of interrupting a <c>Binding</c> on a <c>UIElement</c> when it receives focus,
    /// and resuming that <c>Binding</c> when it loses focus.
    /// </summary>
    /// <remarks>The host element must be of type <c>UIElement</c> and the <c>DependencyProprty</c> defined by PropertyName property must be set to a <c>BindingBase</c> object.</remarks>
    public class OnFocusBindingInterruptionBehavior : Behavior<DependencyObject>
    {
        private IDisposable subscriber;
        private DependencyProperty property;

        /// <summary>
        /// Gets or sets the name of the DependencyProperty on which the Binding has to be interrupted.
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// Gets or sets the binding to apply on the Host property defined by 'PropertyName' behavior property.
        /// </summary>
        public BindingBase Binding { get; set; }

        protected override void OnAttached()
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                throw new ArgumentException("PropertyName must be set.");

            if (Binding == null)
            {
                throw new ArgumentException($"Binding must be set for {PropertyName} property of host '{AssociatedObject}' on behavior '{GetType().FullName}'.");
            }

            property = AssociatedObject.GetDependencyProperties(true).FirstOrDefault(dp => dp.Name == PropertyName);

            if (property == null /* need to check DesignMode as well ? */)
                throw new InvalidOperationException($"Impossible to find property named '{PropertyName}' on object typed '{AssociatedObject.GetType()}'.");

            if ((Binding is Binding) == false)
                throw new InvalidOperationException("Not supported binding type.");

            var element = AssociatedObject as FrameworkElement;
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"Behavior of type '{GetType()}' must be bound to objects of type '{typeof(FrameworkElement)}'. (currently bound to object typed '{AssociatedObject.GetType()}')");
            }

            BindingOperations.SetBinding(AssociatedObject, property, Binding);

            // subscribe to *Focus events
            element.GotFocus += OnHostGotFocus;
            element.LostFocus += OnHostLostFocus;

            subscriber = new AnonymousDisposable(() =>
            {
                // unsubscribe from *Focus events
                element.LostFocus -= OnHostLostFocus;
                element.GotFocus -= OnHostGotFocus;
            });
        }

        protected override void OnDetaching()
        {
            // fire events unsubscription
            subscriber.Dispose();
        }

        private void OnHostGotFocus(object sender, RoutedEventArgs e)
        {
            // retrieve current value before clearing binding
            var value = AssociatedObject.GetValue(property);

            // clear binding (the side-effect is that value is also clear from within the control)
            BindingOperations.ClearBinding(AssociatedObject, property);

            // set back value stored before clearing binding
            AssociatedObject.SetValue(property, value);
        }

        private void OnHostLostFocus(object sender, RoutedEventArgs e)
        {
            PushTargetValueToSource();
        }

        // this method may not support several specific cases (to be improved)
        private void PushTargetValueToSource()
        {
            // retrieve the current value of the target (UI control)
            var currentValue = AssociatedObject.GetValue(property);

            var binding = (Binding)Binding;

            // resolve the source instance here (seems BindingOperations.SetBinding does not resolve DataContext)
            var source = binding.Source ?? ((FrameworkElement)AssociatedObject).DataContext;

            var intermediateBinding = new Binding
            {
                // update on PropertyChanged because LostFocus event already happened
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,

                // ensure binding cannot be perturbated by source value
                Mode = BindingMode.OneWayToSource,

                Path = binding.Path,
                Source = source,

                Converter = binding.Converter,
                ConverterParameter = binding.ConverterParameter,
            };

            // apply custom binding
            BindingOperations.SetBinding(AssociatedObject, property, intermediateBinding);

            // set target property, side-effect is the source property value is set too
            AssociatedObject.SetValue(property, currentValue);

            // restore cleared binding
            BindingOperations.SetBinding(AssociatedObject, property, Binding);
        }
    }
}
