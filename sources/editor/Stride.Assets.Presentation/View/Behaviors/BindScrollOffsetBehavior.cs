// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

using Xenko.Core.Presentation.Core;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    /// <summary>
    /// This behavior allows to create bindings on the <see cref="ScrollViewer.HorizontalOffset"/> and the <see cref="ScrollViewer.VerticalOffset"/>
    /// properties of a <see cref="FrameworkElement"/>.
    /// </summary>
    class BindScrollOffsetBehavior : Behavior<ScrollViewer>
    {
        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register("HorizontalOffset", typeof(double), typeof(BindScrollOffsetBehavior), new PropertyMetadata(default(double), OnHorizontalOffsetChanged));

        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register("VerticalOffset", typeof(double), typeof(BindScrollOffsetBehavior), new PropertyMetadata(default(double), OnVerticalOffsetChanged));

        private readonly DependencyPropertyWatcher propertyWatcher = new DependencyPropertyWatcher();

        public double HorizontalOffset { get { return (double)GetValue(HorizontalOffsetProperty); } set { SetValue(HorizontalOffsetProperty, value); } }

        public double VerticalOffset { get { return (double)GetValue(VerticalOffsetProperty); } set { SetValue(VerticalOffsetProperty, value); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            propertyWatcher.Attach(AssociatedObject);
            propertyWatcher.RegisterValueChangedHandler(ScrollViewer.HorizontalOffsetProperty, OnControlHorizontalOffsetChanged);
            propertyWatcher.RegisterValueChangedHandler(ScrollViewer.VerticalOffsetProperty, OnControlVerticalOffsetChanged);
        }

        protected override void OnDetaching()
        {
            propertyWatcher.Detach();
            base.OnDetaching();
        }

        private void OnControlHorizontalOffsetChanged(object sender, EventArgs e)
        {
            SetCurrentValue(HorizontalOffsetProperty, AssociatedObject.HorizontalOffset);
        }

        private void OnControlVerticalOffsetChanged(object sender, EventArgs e)
        {
            SetCurrentValue(VerticalOffsetProperty, AssociatedObject.VerticalOffset);
        }

        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (BindScrollOffsetBehavior)d;
            behavior.AssociatedObject.ScrollToHorizontalOffset(behavior.HorizontalOffset);
        }

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (BindScrollOffsetBehavior)d;
            behavior.AssociatedObject.ScrollToVerticalOffset(behavior.VerticalOffset);
        }
    }
}
