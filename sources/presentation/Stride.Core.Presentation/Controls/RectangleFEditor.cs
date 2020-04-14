// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Controls
{
    public class RectangleFEditor : VectorEditorBase<RectangleF?>
    {
        /// <summary>
        /// Identifies the <see cref="RectX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RectXProperty = DependencyProperty.Register("RectX", typeof(float?), typeof(RectangleFEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="RectY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RectYProperty = DependencyProperty.Register("RectY", typeof(float?), typeof(RectangleFEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="RectWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RectWidthProperty = DependencyProperty.Register("RectWidth", typeof(float?), typeof(RectangleFEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="RectHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RectHeightProperty = DependencyProperty.Register("RectHeight", typeof(float?), typeof(RectangleFEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Gets or sets the X component of the <see cref="RectangleF"/> associated to this control.
        /// </summary>
        public float? RectX { get { return (float?)GetValue(RectXProperty); } set { SetValue(RectXProperty, value); } }

        /// <summary>
        /// Gets or sets the Y component of the <see cref="RectangleF"/> associated to this control.
        /// </summary>
        public float? RectY { get { return (float?)GetValue(RectYProperty); } set { SetValue(RectYProperty, value); } }

        /// <summary>
        /// Gets or sets the width of the <see cref="RectangleF"/> associated to this control.
        /// </summary>
        public float? RectWidth { get { return (float?)GetValue(RectWidthProperty); } set { SetValue(RectWidthProperty, value); } }

        /// <summary>
        /// Gets or sets the height of the <see cref="RectangleF"/> associated to this control.
        /// </summary>
        public float? RectHeight { get { return (float?)GetValue(RectHeightProperty); } set { SetValue(RectHeightProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(RectangleF? value)
        {
            if (value != null)
            {
                SetCurrentValue(RectXProperty, value.Value.X);
                SetCurrentValue(RectYProperty, value.Value.Y);
                SetCurrentValue(RectWidthProperty, value.Value.Width);
                SetCurrentValue(RectHeightProperty, value.Value.Height);
            }
        }

        /// <inheritdoc/>
        protected override RectangleF? UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == RectXProperty)
                return RectX.HasValue && Value.HasValue ? (RectangleF?)new RectangleF(RectX.Value, Value.Value.Y, Value.Value.Width, Value.Value.Height) : null;
            if (property == RectYProperty)
                return RectY.HasValue && Value.HasValue ? (RectangleF?)new RectangleF(Value.Value.X, RectY.Value, Value.Value.Width, Value.Value.Height) : null;
            if (property == RectWidthProperty)
                return RectWidth.HasValue && Value.HasValue ? (RectangleF?)new RectangleF(Value.Value.X, Value.Value.Y, RectWidth.Value, Value.Value.Height) : null;
            if (property == RectHeightProperty)
                return RectHeight.HasValue && Value.HasValue ? (RectangleF?)new RectangleF(Value.Value.X, Value.Value.Y, Value.Value.Width, RectHeight.Value) : null;

            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override RectangleF? UpateValueFromFloat(float value)
        {
            return new RectangleF(0.0f, 0.0f, value, value);
        }
    }
}
