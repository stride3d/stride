// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Xenko.Core.Presentation.Behaviors
{
    /// <summary>
    /// Allows the bind the <see cref="Control.ToolTip"/> property of a control to a particular target property when the attached control is hovered by the mouse.
    /// This behavior can be used to display the same message that the tool-tip in a status bar, for example.
    /// </summary>
    /// <remarks>This behavior can be used to display the tool tip of some controls in another place, such as a status bar.</remarks>
    public class BindCurrentToolTipStringBehavior : Behavior<Control>
    {
        /// <summary>
        /// Identifies the <see cref="ToolTipTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTargetProperty = DependencyProperty.Register(nameof(ToolTipTarget), typeof(string), typeof(BindCurrentToolTipStringBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Identifies the <see cref="DefaultValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(nameof(DefaultValue), typeof(string), typeof(BindCurrentToolTipStringBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets the tool tip text of the control when the mouse is over the control, or <see cref="DefaultValue"/> otherwise. This property should usually be bound.
        /// </summary>
        public string ToolTipTarget { get { return (string)GetValue(ToolTipTargetProperty); } set { SetValue(ToolTipTargetProperty, value); } }

        /// <summary>
        /// Gets or sets the default value to set when the mouse is not over the control.
        /// </summary>
        public string DefaultValue { get { return (string)GetValue(DefaultValueProperty); } set { SetValue(DefaultValueProperty, value); } }
        
        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseEnter += MouseEnter;
            AssociatedObject.MouseLeave += MouseLeave;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.MouseEnter -= MouseEnter;
            AssociatedObject.MouseLeave -= MouseLeave;
            base.OnDetaching();
        }

        private void MouseEnter(object sender, MouseEventArgs e)
        {
            SetCurrentValue(ToolTipTargetProperty, AssociatedObject.ToolTip);
        }

        private void MouseLeave(object sender, MouseEventArgs e)
        {
            SetCurrentValue(ToolTipTargetProperty, DefaultValue);
        }

    }
}
