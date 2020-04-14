// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Controls
{
    public class Int3Editor : VectorEditorBase<Int3?>
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(int?), typeof(Int3Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(int?), typeof(Int3Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Z"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZProperty = DependencyProperty.Register("Z", typeof(int?), typeof(Int3Editor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Gets or sets the X component of the <see cref="Int3"/> associated to this control.
        /// </summary>
        public int? X { get { return (int?)GetValue(XProperty); } set { SetValue(XProperty, value); } }

        /// <summary>
        /// Gets or sets the Y component of the <see cref="Int3"/> associated to this control.
        /// </summary>
        public int? Y { get { return (int?)GetValue(YProperty); } set { SetValue(YProperty, value); } }

        /// <summary>
        /// Gets or sets the Z component of the <see cref="Int3"/> associated to this control.
        /// </summary>
        public int? Z { get { return (int?)GetValue(ZProperty); } set { SetValue(ZProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Int3? value)
        {
            if (value != null)
            {
                SetCurrentValue(XProperty, value.Value.X);
                SetCurrentValue(YProperty, value.Value.Y);
                SetCurrentValue(ZProperty, value.Value.Z);
            }
        }

        /// <inheritdoc/>
        protected override Int3? UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == XProperty)
                return X.HasValue && Value.HasValue ? (Int3?)new Int3(X.Value, Value.Value.Y, Value.Value.Z) : null;
            if (property == YProperty)
                return Y.HasValue && Value.HasValue ? (Int3?)new Int3(Value.Value.X, Y.Value, Value.Value.Z) : null;
            if (property == ZProperty)
                return Z.HasValue && Value.HasValue ? (Int3?)new Int3(Value.Value.X, Value.Value.Y, Z.Value) : null;

            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override Int3? UpateValueFromFloat(float value)
        {
            return new Int3((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }
    }
}
