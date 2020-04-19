// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Controls
{
    public class Vector2Editor : VectorEditor<Vector2?>
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(float?), typeof(Vector2Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register(nameof(Y), typeof(float?), typeof(Vector2Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Length"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(nameof(Length), typeof(float?), typeof(Vector2Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceLengthValue));

        static Vector2Editor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Vector2Editor), new FrameworkPropertyMetadata(typeof(Vector2Editor)));
        }

        /// <summary>
        /// Gets or sets the X component (in Cartesian coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float? X { get { return (float?)GetValue(XProperty); } set { SetValue(XProperty, value); } }

        /// <summary>
        /// Gets or sets the Y component (in Cartesian coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float? Y { get { return (float?)GetValue(YProperty); } set { SetValue(YProperty, value); } }

        /// <summary>
        /// Gets or sets the length (in polar coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float? Length { get { return (float?)GetValue(LengthProperty); } set { SetValue(LengthProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Vector2? value)
        {
            if (value != null)
            {
                SetCurrentValue(XProperty, value.Value.X);
                SetCurrentValue(YProperty, value.Value.Y);
                SetCurrentValue(LengthProperty, value.Value.Length());
            }
        }

        /// <inheritdoc/>
        protected override Vector2? UpdateValueFromComponent(DependencyProperty property)
        {
            switch (EditingMode)
            {
                case VectorEditingMode.Normal:
                    if (property == XProperty)
                        return X.HasValue && Value.HasValue ? (Vector2?)new Vector2(X.Value, Value.Value.Y) : null;
                    if (property == YProperty)
                        return Y.HasValue && Value.HasValue ? (Vector2?)new Vector2(Value.Value.X, Y.Value) : null;
                    break;

                case VectorEditingMode.AllComponents:
                    if (property == XProperty)
                        return X.HasValue ? (Vector2?)new Vector2(X.Value) : null;
                    if (property == YProperty)
                        return Y.HasValue ? (Vector2?)new Vector2(Y.Value) : null;
                    break;

                case VectorEditingMode.Length:
                    if (property == LengthProperty)
                        return Length.HasValue ? (Vector2?)FromLength(Value ?? Vector2.One, Length.Value) : null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(EditingMode));
            }

            throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)} in {EditingMode} mode.");
        }

        /// <inheritdoc/>
        protected override Vector2? UpateValueFromFloat(float value)
        {
            return new Vector2(value);
        }

        /// <summary>
        /// Coerce the value of the Length so it is always positive
        /// </summary>
        [NotNull]
        private static object CoerceLengthValue(DependencyObject sender, object baseValue)
        {
            baseValue = CoerceComponentValue(sender, baseValue);
            return Math.Max(0.0f, (float)baseValue);
        }

        private static Vector2 FromLength(Vector2 value, float length)
        {
            var newValue = value;
            newValue.Normalize();
            newValue *= length;
            return newValue;
        }
    }
}
