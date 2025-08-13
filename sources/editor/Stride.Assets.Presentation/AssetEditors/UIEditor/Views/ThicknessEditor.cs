// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Core.Presentation.Controls;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Views
{
    using Thickness = Stride.UI.Thickness;

    public class ThicknessEditor : VectorEditorBase<Thickness?>
    {
        /// <summary>
        /// Identifies the <see cref="Bottom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BottomProperty =
            DependencyProperty.Register(nameof(Bottom), typeof(float?), typeof(ThicknessEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));
        
        /// <summary>
        /// Identifies the <see cref="Left"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register(nameof(Left), typeof(float?), typeof(ThicknessEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Right"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightProperty =
            DependencyProperty.Register(nameof(Right), typeof(float?), typeof(ThicknessEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Top"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register(nameof(Top), typeof(float?), typeof(ThicknessEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));
        
        /// <summary>
        /// Gets or sets the <see cref="Thickness.Bottom"/> component of the <see cref="Thickness"/> associated to this control.
        /// </summary>
        public float? Bottom { get { return (float?)GetValue(BottomProperty); } set { SetValue(BottomProperty, value); } }
        
        /// <summary>
        /// Gets or sets the <see cref="Thickness.Left"/> component of the <see cref="Thickness"/> associated to this control.
        /// </summary>
        public float? Left { get { return (float?)GetValue(LeftProperty); } set { SetValue(LeftProperty, value); } }
        
        /// <summary>
        /// Gets or sets the <see cref="Thickness.Right"/> component of the <see cref="Thickness"/> associated to this control.
        /// </summary>
        public float? Right { get { return (float?)GetValue(RightProperty); } set { SetValue(RightProperty, value); } }

        /// <summary>
        /// Gets or sets the <see cref="Thickness.Top"/> component of the <see cref="Thickness"/> associated to this control.
        /// </summary>
        public float? Top { get { return (float?)GetValue(TopProperty); } set { SetValue(TopProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Thickness? value)
        {
            if (value != null)
            {
                SetCurrentValue(LeftProperty, value.Value.Left);
                SetCurrentValue(TopProperty, value.Value.Top);
                SetCurrentValue(RightProperty, value.Value.Right);
                SetCurrentValue(BottomProperty, value.Value.Bottom);
            }
        }

        /// <inheritdoc/>
        protected override Thickness? UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == LeftProperty)
                return Left.HasValue && Value.HasValue ? (Thickness?)new Thickness(Left.Value, Value.Value.Top, Value.Value.Right, Value.Value.Bottom) : null;
            if (property == TopProperty)
                return Top.HasValue && Value.HasValue ? (Thickness?)new Thickness(Value.Value.Left, Top.Value, Value.Value.Right, Value.Value.Bottom) : null;
            if (property == RightProperty)
                return Right.HasValue && Value.HasValue ? (Thickness?)new Thickness(Value.Value.Left, Value.Value.Top, Right.Value, Value.Value.Bottom) : null;
            if (property == BottomProperty)
                return Bottom.HasValue && Value.HasValue ? (Thickness?)new Thickness(Value.Value.Left, Value.Value.Top, Value.Value.Right, Bottom.Value) : null;

            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override Thickness? UpateValueFromFloat(float value)
        {
            return Thickness.Uniform(value);
        }
    }
}
