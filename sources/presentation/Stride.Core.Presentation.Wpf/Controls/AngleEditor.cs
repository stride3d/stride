// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// Represents a control that allows to edit an angle value stored in radians but displayed in degrees.
    /// </summary>
    public class AngleEditor : VectorEditorBase<float>
    {
        /// <summary>
        /// Identifies the <see cref="Degrees"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DegreesProperty = DependencyProperty.Register(
            nameof(Degrees),
            typeof(float),
            typeof(AngleEditor),
            new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Gets or sets the angle in degrees.
        /// </summary>
        public float Degrees
        {
            get => (float)GetValue(DegreesProperty);
            set => SetValue(DegreesProperty, value);
        }

        /// <inheritdoc/>
        public override void ResetValue()
        {
            Value = DefaultValue;
        }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(float value)
        {
            var degrees = GetDisplayValue(value);
            SetCurrentValue(DegreesProperty, degrees);
        }

        /// <inheritdoc/>
        protected override float UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == DegreesProperty)
            {
                return MathUtil.DegreesToRadians(Degrees);
            }

            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override float UpdateValueFromFloat(float value)
        {
            return MathUtil.DegreesToRadians(value);
        }

        /// <summary>
        /// Converts radians to degrees for display, handling edge cases like -0.
        /// </summary>
        /// <param name="angleRadians">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        private static float GetDisplayValue(float angleRadians)
        {
            var degrees = MathUtil.RadiansToDegrees(angleRadians);

            // Normalize -0 to +0 for cleaner display
            if (degrees == 0 && float.IsNegative(degrees))
            {
                degrees = 0;
            }

            return degrees;
        }
    }
}
