// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Adorners;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class TextBoxVectorPropertyValueValidationBehavior : Behavior<VectorEditorBase>
    {
        private TextBoxAndAdorner[] textBoxAndAdorners;

        /// <summary>
        /// Identifies the <see cref="AdornerStoryboard"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AdornerStoryboardProperty =
            DependencyProperty.Register(nameof(AdornerStoryboard), typeof(Storyboard), typeof(TextBoxVectorPropertyValueValidationBehavior), new PropertyMetadata(null, AdornerStoryboardPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(TextBoxVectorPropertyValueValidationBehavior), new PropertyMetadata(Brushes.IndianRed, BorderPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderCornerRadiusProperty =
            DependencyProperty.Register(nameof(BorderCornerRadius), typeof(double), typeof(TextBoxVectorPropertyValueValidationBehavior), new PropertyMetadata(3.0, BorderPropertyChanged));
        /// <summary>
        /// Identifies the <see cref="BorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(TextBoxVectorPropertyValueValidationBehavior), new PropertyMetadata(2.0, BorderPropertyChanged));

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
            AssociatedObject.Loaded += OnAssociatedObjectLoaded;

            base.OnAttached();
        }

        private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            var textBoxes = AssociatedObject.FindVisualChildrenOfType<TextBoxBase>();
            textBoxes.ForEach(x =>
            {
                x.Validating += OnValidating;
                x.TextToSourceValueConversionFailed += OnTextToSourceValueConversionFailed;
            });

            var adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            if (adornerLayer != null)
            {
                textBoxAndAdorners = textBoxes.Select(textBox =>
                {
                    var adorner = new HighlightBorderAdorner(textBox)
                    {
                        BackgroundBrush = null,
                        BorderBrush = BorderBrush,
                        BorderCornerRadius = BorderCornerRadius,
                        BorderThickness = BorderThickness,
                        State = HighlightAdornerState.Hidden,
                    };
                    adornerLayer.Add(adorner);
                    return new TextBoxAndAdorner(textBox, adorner);
                }).ToArray();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            var textBoxes = AssociatedObject.FindVisualChildrenOfType<TextBoxBase>();
            textBoxes.ForEach(x =>
            {
                x.Validating -= OnValidating;
                x.TextToSourceValueConversionFailed -= OnTextToSourceValueConversionFailed;
            });

            if (textBoxAndAdorners != null)
            {
                if (AdornerStoryboard != null)
                {
                    textBoxAndAdorners.ForEach(tba => AdornerStoryboard.Remove(tba.Adorner));
                }
                textBoxAndAdorners.ForEach(tba => AdornerLayer.GetAdornerLayer(tba.TextBox)?.Remove(tba.Adorner));
                textBoxAndAdorners = null;
            }
        }

        private void OnValidating(object sender, CancelRoutedEventArgs e)
        {
            if (textBoxAndAdorners != null)
            {
                var adorner = textBoxAndAdorners.FirstOrDefault(x => x.TextBox == sender).Adorner;
                if (adorner != null)
                {
                    adorner.State = HighlightAdornerState.Hidden;
                }
            }
        }

        private void OnTextToSourceValueConversionFailed(object sender, RoutedEventArgs e)
        {
            if (textBoxAndAdorners != null && AdornerStoryboard != null)
            {
                var adorner = textBoxAndAdorners.FirstOrDefault(x => x.TextBox == sender).Adorner;
                if (adorner != null)
                {
                    adorner.State = HighlightAdornerState.Visible;
                    // Show visual indicator it has failed.
                    AdornerStoryboard.Begin(adorner);
                }
            }
        }

        private static void AdornerStoryboardPropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextBoxVectorPropertyValueValidationBehavior)d;
            var textBoxAndAdorners = behavior.textBoxAndAdorners;
            if (textBoxAndAdorners != null)
            {
                foreach (var tba in textBoxAndAdorners)
                {
                    var previousStoryboard = e.OldValue as Storyboard;
                    if (previousStoryboard != null && tba.Adorner != null)
                    {
                        previousStoryboard.Remove(tba.Adorner);
                    }

                    var newStoryboard = e.NewValue as Storyboard;
                    if (newStoryboard != null && tba.Adorner != null)
                    {
                        Storyboard.SetTarget(behavior.AdornerStoryboard, tba.Adorner);
                    }
                }
            }
        }

        private static void BorderPropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (TextBoxVectorPropertyValueValidationBehavior)d;
            var textBoxAndAdorners = behavior.textBoxAndAdorners;
            if (textBoxAndAdorners != null)
            {
                foreach (var tba in textBoxAndAdorners)
                {
                    if (e.Property == BorderBrushProperty)
                        tba.Adorner.BorderBrush = behavior.BorderBrush;

                    if (e.Property == BorderCornerRadiusProperty)
                        tba.Adorner.BorderCornerRadius = behavior.BorderCornerRadius;

                    if (e.Property == BorderThicknessProperty)
                        tba.Adorner.BorderThickness = behavior.BorderThickness;
                }
            }
        }

        private readonly struct TextBoxAndAdorner
        {
            public readonly TextBoxBase TextBox;
            public readonly HighlightBorderAdorner Adorner;

            public TextBoxAndAdorner(TextBoxBase textBox, HighlightBorderAdorner adorner)
            {
                TextBox = textBox;
                Adorner = adorner;
            }
        }
    }
}
