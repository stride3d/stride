// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Adorners;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Core;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class TextBoxPropertyValueValidationBehavior : Behavior<TextBoxBase>
    {
        private HighlightBorderAdorner adorner;

        /// <summary>
        /// Identifies the <see cref="AdornerStoryboard"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AdornerStoryboardProperty =
            DependencyProperty.Register(nameof(AdornerStoryboard), typeof(Storyboard), typeof(TextBoxPropertyValueValidationBehavior), new PropertyMetadata(null, AdornerStoryboardPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(TextBoxPropertyValueValidationBehavior), new PropertyMetadata(Brushes.IndianRed, BorderPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderCornerRadiusProperty =
            DependencyProperty.Register(nameof(BorderCornerRadius), typeof(double), typeof(TextBoxPropertyValueValidationBehavior), new PropertyMetadata(3.0, BorderPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(TextBoxPropertyValueValidationBehavior), new PropertyMetadata(2.0, BorderPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="Storyboard"/> associated to this behavior.
        /// </summary>
        public Storyboard AdornerStoryboard { get { return (Storyboard)GetValue(AdornerStoryboardProperty); } set { SetValue(AdornerStoryboardProperty, value); } }
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

            AssociatedObject.Validating += OnValidating;
            AssociatedObject.TextToSourceValueConversionFailed += OnTextToSourceValueConversionFailed;

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Validating -= OnValidating;
            AssociatedObject.TextToSourceValueConversionFailed -= OnTextToSourceValueConversionFailed;

            if (adorner != null)
            {
                if (AdornerStoryboard != null)
                {
                    AdornerStoryboard.Remove(adorner);
                }
                AdornerLayer.GetAdornerLayer(AssociatedObject)?.Remove(adorner);
            }
        }

        private void OnValidating(object sender, CancelRoutedEventArgs e)
        {
            adorner.State = HighlightAdornerState.Hidden;
        }

        private void OnTextToSourceValueConversionFailed(object sender, RoutedEventArgs e)
        {
            if (AdornerStoryboard != null && adorner != null)
            {
                adorner.State = HighlightAdornerState.Visible;
                // Show visual indicator it has failed.
                AdornerStoryboard.Begin(adorner);
            }
        }

        private static void AdornerStoryboardPropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextBoxPropertyValueValidationBehavior)d;
            var adorner = behavior.adorner;

            var previousStoryboard = e.OldValue as Storyboard;
            if (previousStoryboard != null && adorner != null)
            {
                previousStoryboard.Remove(adorner);
            }

            var newStoryboard = e.NewValue as Storyboard;
            if (newStoryboard != null && adorner != null)
            {
                Storyboard.SetTarget(behavior.AdornerStoryboard, adorner);
            }
        }

        private static void BorderPropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextBoxPropertyValueValidationBehavior)d;
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
    }
}
