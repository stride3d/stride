// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.Behaviors
{
    public class OverrideCursorBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CursorProperty = DependencyProperty.Register("Cursor", typeof(Cursor), typeof(OverrideCursorBehavior), new PropertyMetadata(PropertyChanged));

        public static readonly DependencyProperty ForceCursorProperty = DependencyProperty.Register("ForceCursor", typeof(bool), typeof(OverrideCursorBehavior), new PropertyMetadata(PropertyChanged));

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(OverrideCursorBehavior), new PropertyMetadata(BooleanBoxes.TrueBox, PropertyChanged));

        public Cursor Cursor { get { return (Cursor)GetValue(CursorProperty); } set { SetValue(CursorProperty, value); } }

        public bool ForceCursor { get { return (bool)GetValue(ForceCursorProperty); } set { SetValue(ForceCursorProperty, value.Box()); } }

        public bool IsActive { get { return (bool)GetValue(IsActiveProperty); } set { SetValue(IsActiveProperty, value.Box()); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateCursorOverride();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Cursor = null;
            base.OnDetaching();
        }
        private static void PropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (OverrideCursorBehavior)d;
            behavior.UpdateCursorOverride();
        }

        private void UpdateCursorOverride()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Cursor = IsActive ? Cursor : null;
                AssociatedObject.ForceCursor = IsActive && ForceCursor;
            }
        }
    }
}
