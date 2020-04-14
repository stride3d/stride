// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// An implementation of the <see cref="OnEventBehavior"/> class that allows to set the value of a dependency property
    /// on the control hosting this behavior when a specific event is raised.
    /// </summary>
    public class OnEventSetPropertyBehavior : OnEventBehavior
    {
        /// <summary>
        /// Identifies the <see cref="Property"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PropertyProperty = DependencyProperty.Register("Property", typeof(DependencyProperty), typeof(OnEventSetPropertyBehavior));

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(OnEventSetPropertyBehavior));

        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(DependencyObject), typeof(OnEventSetPropertyBehavior));

        /// <summary>
        /// Gets or sets the <see cref="DependencyProperty"/> to set when the event is raised.
        /// </summary>
        public DependencyProperty Property { get { return (DependencyProperty)GetValue(PropertyProperty); } set { SetValue(PropertyProperty, value); } }

        /// <summary>
        /// Gets or sets the value to set when the event is raised.
        /// </summary>
        public object Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }

        /// <summary>
        /// Gets or sets the target control to set the dependency property.
        /// If null, it will be set on the control hosting this behavior.
        /// </summary>
        public DependencyObject Target { get { return (DependencyObject)GetValue(TargetProperty); } set { SetValue(TargetProperty, value); } }

        /// <inheritdoc/>
        protected override void OnEvent()
        {
            var target = Target ?? AssociatedObject;
            target.SetCurrentValue(Property, Value);
        }
    }
}
