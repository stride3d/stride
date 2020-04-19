// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Core
{
    /// <summary>
    /// This class hold the <see cref="IsFocusedProperty"/> attached dependency property that allows to give the focus to a control using bindings.
    /// </summary>
    public static class FocusManager
    {
        /// <summary>
        /// Identify the IsFocused attached dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusManager), new UIPropertyMetadata(BooleanBoxes.FalseBox, OnIsFocusedPropertyChanged));

        /// <summary>
        /// Gets whether the given object has currently the focus.
        /// </summary>
        /// <param name="obj">The object. If it is not an <see cref="UIElement"/>, this method will return <c>false</c>.</param>
        /// <returns><c>true</c> if the given object has the focus, false otherwise.</returns>
        public static bool GetIsFocused(DependencyObject obj)
        {
            var uiElement = obj as UIElement;
            return uiElement != null && uiElement.IsFocused;
        }

        /// <summary>
        /// Gives the focus to the given object.
        /// </summary>
        /// <param name="obj">The object that should get the focus.</param>
        /// <param name="value">The state of the focus. If value is <c>true</c>, the object will get the focus. Otherwise, this method does nothing.</param>
        public static void SetIsFocused([NotNull] DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        /// <summary>
        /// Raised when the <see cref="IsFocusedProperty"/> dependency property is modified.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnIsFocusedPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement)obj;
            if ((bool)e.NewValue)
            {
                uiElement.Focus();
            }
        }
    }
}
