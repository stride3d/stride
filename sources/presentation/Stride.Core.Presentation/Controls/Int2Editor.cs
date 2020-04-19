// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Controls
{
    public class Int2Editor : VectorEditorBase<Int2?>
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(int?), typeof(Int2Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(int?), typeof(Int2Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Gets or sets the X component of the <see cref="Int2"/> associated to this control.
        /// </summary>
        public int? X { get { return (int?)GetValue(XProperty); } set { SetValue(XProperty, value); } }

        /// <summary>
        /// Gets or sets the Y component of the <see cref="Int2"/> associated to this control.
        /// </summary>
        public int? Y { get { return (int?)GetValue(YProperty); } set { SetValue(YProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Int2? value)
        {
            if (value != null)
            {
                SetCurrentValue(XProperty, value.Value.X);
                SetCurrentValue(YProperty, value.Value.Y);
            }
        }

        /// <inheritdoc/>
        protected override Int2? UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == XProperty)
                return X.HasValue && Value.HasValue ? (Int2?)new Int2(X.Value, Value.Value.Y) : null;
            if (property == YProperty)
                return Y.HasValue && Value.HasValue ? (Int2?)new Int2(Value.Value.X, Y.Value) : null;
              
            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override Int2? UpateValueFromFloat(float value)
        {
            return new Int2((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }
    }
}
