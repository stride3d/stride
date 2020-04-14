// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Adorners;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Behaviors
{
    public class ContainTextAdornerBehavior : Behavior<TextBox>
    {
        private readonly DependencyPropertyWatcher propertyWatcher = new DependencyPropertyWatcher();
        private HighlightBorderAdorner adorner;

        /// <summary>
        /// Identifies the <see cref="BorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty = 
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(ContainTextAdornerBehavior), new PropertyMetadata(Brushes.SteelBlue, PropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderCornerRadiusProperty =
            DependencyProperty.Register(nameof(BorderCornerRadius), typeof(double), typeof(ContainTextAdornerBehavior), new PropertyMetadata(3.0, PropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(ContainTextAdornerBehavior), new PropertyMetadata(2.0, PropertyChanged));
        
        /// <summary>
        /// Gets or sets the border brush when the adorner visible.
        /// </summary>
        public Brush BorderBrush { get { return (Brush)GetValue(BorderBrushProperty); } set { SetValue(BorderBrushProperty, value); } }
        /// <summary>
        /// Gets or sets the border corner radius when the adorner is visible.
        /// </summary>
        public double BorderCornerRadius { get { return (double)GetValue(BorderCornerRadiusProperty); } set { SetValue(BorderCornerRadiusProperty, value); } }
        /// <summary>
        /// Gets or sets the border thickness when the adorner is visible.
        /// </summary>
        public double BorderThickness { get { return (double)GetValue(BorderThicknessProperty); } set { SetValue(BorderThicknessProperty, value); } }

        protected override void OnAttached()
        {
            var textProperty = AssociatedObject.GetDependencyProperties(true).FirstOrDefault(dp => dp.Name == nameof(AssociatedObject.Text));
            if (textProperty == null)
                throw new ArgumentException($"Unable to find public property '{nameof(AssociatedObject.Text)}' on object of type '{AssociatedObject.GetType().FullName}'.");

            propertyWatcher.Attach(AssociatedObject);
            propertyWatcher.RegisterValueChangedHandler(textProperty, OnTextChanged);
            
            var adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            if (adornerLayer != null)
            {
                adorner = new HighlightBorderAdorner(AssociatedObject)
                {
                    BackgroundBrush = null,
                    BorderBrush = BorderBrush,
                    BorderCornerRadius = BorderCornerRadius,
                    BorderThickness = BorderThickness,
                    State = HighlightAdornerState.Hidden,
                };
                adornerLayer.Add(adorner);
            }
        }

        protected override void OnDetaching()
        {
            propertyWatcher.Detach();
            
            if (adorner != null)
            {
                AdornerLayer.GetAdornerLayer(AssociatedObject)?.Remove(adorner);
            }
        }

        private static void PropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ContainTextAdornerBehavior)d;
            var adorner = behavior.adorner;
            if (adorner != null)
            {
                if (e.Property == BorderBrushProperty)
                    adorner.BorderBrush = behavior.BorderBrush;

                if (e.Property == BorderCornerRadiusProperty)
                    adorner.BorderCornerRadius = behavior.BorderCornerRadius;

                if (e.Property == BorderThicknessProperty)
                    adorner.BorderThickness = behavior.BorderThickness;
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            if (adorner == null)
                return;

            var showAdorner = !string.IsNullOrEmpty(AssociatedObject.Text);
            adorner.State = showAdorner ? HighlightAdornerState.Visible : HighlightAdornerState.Hidden;
        }
    }
}
