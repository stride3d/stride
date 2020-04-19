// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// An implementation of the <see cref="TextBoxBase"/> control that provides additional features such as a proper
    /// validation/cancellation workflow, and a watermark to display when the text is empty.
    /// </summary>
    [TemplatePart(Name = "PART_TrimmedText", Type = typeof(TextBlock))]
    public class TextBox : TextBoxBase
    {
        private TextBlock trimmedTextBlock;
        private readonly Timer validationTimer;

        /// <summary>
        /// Identifies the <see cref="UseTimedValidation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseTimedValidationProperty = DependencyProperty.Register("UseTimedValidation", typeof(bool), typeof(TextBox), new PropertyMetadata(BooleanBoxes.FalseBox, OnUseTimedValidationPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ValidationDelay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationDelayProperty = DependencyProperty.Register("ValidationDelay", typeof(int), typeof(TextBox), new PropertyMetadata(500));
        
        /// <summary>
        /// Identifies the <see cref="TrimmedText"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey TrimmedTextPropertyKey = DependencyProperty.RegisterReadOnly("TrimmedText", typeof(string), typeof(TextBox), new PropertyMetadata(""));

        /// <summary>
        /// Identifies the <see cref="TrimmedText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrimmedTextProperty = TrimmedTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Clears the current <see cref="System.Windows.Controls.TextBox.Text"/> of a text box.
        /// </summary>
        public static RoutedCommand ClearTextCommand { get; }
        
        static TextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(typeof(TextBox)));
            ClearTextCommand = new RoutedCommand("ClearTextCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(ClearTextCommand, OnClearTextCommand));
        }

        public TextBox()
        {
            if (DesignerProperties.GetIsInDesignMode(this) == false)
                validationTimer = new Timer(x => Dispatcher.InvokeAsync(Validate), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Gets or sets whether the text should be automatically validated after a delay defined by the <see cref="ValidationDelay"/> property.
        /// </summary>
        public bool UseTimedValidation { get { return (bool)GetValue(UseTimedValidationProperty); } set { SetValue(UseTimedValidationProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the amount of time before a validation of input text happens, in milliseconds.
        /// Every change to the <see cref="TextBox.Text"/> property reset the timer to this value.
        /// </summary>
        /// <remarks>The default value is <c>500</c> milliseconds.</remarks>
        public int ValidationDelay { get { return (int)GetValue(ValidationDelayProperty); } set { SetValue(ValidationDelayProperty, value); } }
        
        /// <summary>
        /// Gets the trimmed text to display when the control does not have the focus, depending of the value of the <see cref="TextTrimming"/> property.
        /// </summary>
        public string TrimmedText { get { return (string)GetValue(TrimmedTextPropertyKey.DependencyProperty); } private set { SetValue(TrimmedTextPropertyKey, value); } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            trimmedTextBlock = GetTemplateChild("PART_TrimmedText") as TextBlock;
            if (trimmedTextBlock == null)
                throw new InvalidOperationException("A part named 'PART_TrimmedText' must be present in the ControlTemplate, and must be of type 'TextBlock'.");
        }

        /// <summary>
        /// Raised when the text of the TextBox changes.
        /// </summary>
        /// <param name="oldValue">The old value of the <see cref="TextBox.Text"/> property.</param>
        /// <param name="newValue">The new value of the <see cref="TextBox.Text"/> property.</param>
        protected override void OnTextChanged(string oldValue, string newValue)
        {
            if (UseTimedValidation)
            {
                if (ValidationDelay > 0.0)
                {
                    validationTimer?.Change(ValidationDelay, Timeout.Infinite);
                }
                else
                {
                    Validate();
                }
            }
            
            var availableWidth = ActualWidth;
            if (trimmedTextBlock != null)
                availableWidth -= trimmedTextBlock.Margin.Left + trimmedTextBlock.Margin.Right;

            TrimmedText = Trimming.ProcessTrimming(this, Text, availableWidth);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var arrangedSize = base.ArrangeOverride(arrangeBounds);
            var availableWidth = arrangeBounds.Width;
            if (trimmedTextBlock != null)
                availableWidth -= trimmedTextBlock.Margin.Left + trimmedTextBlock.Margin.Right;

            TrimmedText = Trimming.ProcessTrimming(this, Text, availableWidth);
            return arrangedSize;
        }

        private static void OnUseTimedValidationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var txt = (TextBox)sender;
            if ((bool)e.NewValue)
            {
                txt.Validate();
            }
        }

        private static void OnClearTextCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.Clear();
        }
    }
}
