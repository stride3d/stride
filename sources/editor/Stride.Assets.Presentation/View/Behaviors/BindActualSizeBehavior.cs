// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;

using Xenko.Core.Presentation.Core;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    /// <summary>
    /// This behavior allows to create bindings on the <see cref="FrameworkElement.ActualWidth"/> and the <see cref="FrameworkElement.ActualHeight"/>
    /// properties of a <see cref="FrameworkElement"/>.
    /// </summary>
    class BindActualSizeBehavior : Behavior<FrameworkElement>
    {
        private readonly DependencyPropertyWatcher propertyWatcher = new DependencyPropertyWatcher();

        public static readonly DependencyProperty ActualWidthProperty = DependencyProperty.Register("ActualWidth", typeof(double), typeof(BindActualSizeBehavior));

        public static readonly DependencyProperty ActualHeightProperty = DependencyProperty.Register("ActualHeight", typeof(double), typeof(BindActualSizeBehavior));

        public double ActualWidth { get { return (double)GetValue(ActualWidthProperty); } set { SetValue(ActualWidthProperty, value); } }

        public double ActualHeight { get { return (double)GetValue(ActualHeightProperty); } set { SetValue(ActualHeightProperty, value); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            propertyWatcher.Attach(AssociatedObject);
            propertyWatcher.RegisterValueChangedHandler(FrameworkElement.ActualWidthProperty, OnActualWidthChanged);
            propertyWatcher.RegisterValueChangedHandler(FrameworkElement.ActualHeightProperty, OnActualHeightChanged);
        }

        protected override void OnDetaching()
        {
            propertyWatcher.Detach();
            base.OnDetaching();
        }

        private void OnActualWidthChanged(object sender, EventArgs e)
        {
            SetCurrentValue(ActualWidthProperty, AssociatedObject.ActualWidth);
        }

        private void OnActualHeightChanged(object sender, EventArgs e)
        {
            SetCurrentValue(ActualHeightProperty, AssociatedObject.ActualHeight);
        }
    }
}
