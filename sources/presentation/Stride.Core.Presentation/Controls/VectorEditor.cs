// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Stride.Core.Presentation.Controls
{
    [TemplatePart(Name = ToggleButtonPartName, Type = typeof(ToggleButton))]
    public abstract class VectorEditor<T> : VectorEditorBase<T>
    {
        /// <summary>
        /// The name of the part for the <see cref="ToggleButton"/>.
        /// </summary>
        private const string ToggleButtonPartName = "PART_ToggleButton";

        /// <summary>
        /// Identifies the <see cref="EditingMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EditingModeProperty =
            DependencyProperty.Register(nameof(EditingMode), typeof(VectorEditingMode), typeof(VectorEditor<T>), new FrameworkPropertyMetadata(VectorEditingMode.Normal, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        
        public VectorEditingMode EditingMode { get { return (VectorEditingMode)GetValue(EditingModeProperty); } set { SetValue(EditingModeProperty, value); } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // TODO: the toggle button has been disabled
            //var toggleButton = GetTemplateChild(ToggleButtonPartName) as ToggleButton;
            //if (toggleButton == null)
            //    throw new InvalidOperationException($"A part named '{ToggleButtonPartName}' must be present in the ControlTemplate, and must be of type '{typeof(ToggleButton).FullName}'.");

            //var toggleButtonStyle = Application.Current.TryFindResource(VectorEditorResources.ToggleButtonStyleKey) as Style;
            //if (toggleButtonStyle != null)
            //{
            //    toggleButton.Style = toggleButtonStyle;
            //}
        }
    }

    public static class VectorEditorResources
    {
        /// <summary>
        /// Resource Key for the ToggleButtonStyle.
        /// </summary>
        public static ComponentResourceKey ToggleButtonStyleKey { get; } = new ComponentResourceKey(typeof(VectorEditorResources), nameof(ToggleButtonStyleKey));
    }
}
