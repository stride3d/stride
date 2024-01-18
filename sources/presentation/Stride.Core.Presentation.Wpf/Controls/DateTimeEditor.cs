// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    public sealed class DateTimeEditor : Control
    {
        private bool interlock;
        private bool templateApplied;
        private DependencyProperty initializingProperty;

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(DateTimeEditor), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Identifies the <see cref="WatermarkContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkContentProperty = DependencyProperty.Register("WatermarkContent", typeof(object), typeof(DateTimeEditor), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="WatermarkContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkContentTemplateProperty = DependencyProperty.Register("WatermarkContentTemplate", typeof(DataTemplate), typeof(DateTimeEditor), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(DateTime?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged, null, false, UpdateSourceTrigger.Explicit));

        /// <summary>
        /// Identifies the <see cref="Year"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearProperty = DependencyProperty.Register("Year", typeof(int?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Month"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthProperty = DependencyProperty.Register("Month", typeof(int?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Day"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayProperty = DependencyProperty.Register("Day", typeof(int?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Hour"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourProperty = DependencyProperty.Register("Hour", typeof(int?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Minute"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteProperty = DependencyProperty.Register("Minute", typeof(int?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Second"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondProperty = DependencyProperty.Register("Second", typeof(double?), typeof(DateTimeEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged));

        /// <summary>
        /// Gets or sets whether the drop-down of this control editor is currently open.
        /// </summary>
        public bool IsDropDownOpen { get { return (bool)GetValue(IsDropDownOpenProperty); } set { SetValue(IsDropDownOpenProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the content to display when the TextBox is empty.
        /// </summary>
        public object WatermarkContent { get { return GetValue(WatermarkContentProperty); } set { SetValue(WatermarkContentProperty, value); } }

        /// <summary>
        /// Gets or sets the template of the content to display when the TextBox is empty.
        /// </summary>
        public DataTemplate WatermarkContentTemplate { get { return (DataTemplate)GetValue(WatermarkContentTemplateProperty); } set { SetValue(WatermarkContentTemplateProperty, value); } }

        /// <summary>
        /// Gets or sets the date time associated to this control.
        /// </summary>
        public DateTime? Value { get { return (DateTime?)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }

        /// <summary>
        /// Gets or sets the year displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public int? Year { get { return (int?)GetValue(YearProperty); } set { SetValue(YearProperty, value); } }

        /// <summary>
        /// Gets or sets the month displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public int? Month { get { return (int?)GetValue(MonthProperty); } set { SetValue(MonthProperty, value); } }

        /// <summary>
        /// Gets or sets the day displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public int? Day { get { return (int?)GetValue(DayProperty); } set { SetValue(DayProperty, value); } }

        /// <summary>
        /// Gets or sets the hour displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public int? Hour { get { return (int?)GetValue(HourProperty); } set { SetValue(HourProperty, value); } }

        /// <summary>
        /// Gets or sets the minute displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public int? Minute { get { return (int?)GetValue(MinuteProperty); } set { SetValue(MinuteProperty, value); } }

        /// <summary>
        /// Gets or sets the second displayed in the <see cref="DateTimeEditor"/>.
        /// </summary>
        public double? Second { get { return (double?)GetValue(SecondProperty); } set { SetValue(SecondProperty, value); } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            templateApplied = false;
            base.OnApplyTemplate();
            templateApplied = true;
        }

        /// <inheritdoc/>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            if (IsDropDownOpen && !IsKeyboardFocusWithin)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        /// <summary>
        /// Updates the properties corresponding to the components of the date time from the given date time value.
        /// </summary>
        /// <param name="value">The date time from which to update component properties.</param>
        private void UpdateComponentsFromValue(DateTime? value)
        {
            if (value != null)
            {
                SetCurrentValue(YearProperty, value.Value.Year);
                SetCurrentValue(MonthProperty, value.Value.Month);
                SetCurrentValue(DayProperty, value.Value.Day);
                SetCurrentValue(HourProperty, value.Value.Hour);
                SetCurrentValue(MinuteProperty, value.Value.Minute);
                SetCurrentValue(SecondProperty, (double)(value.Value.Ticks % TimeSpan.TicksPerMinute) / TimeSpan.TicksPerSecond);
            }
        }

        /// <summary>
        /// Updates the <see cref="Value"/> property according to a change in the given component property.
        /// </summary>
        /// <param name="property">The component property from which to update the <see cref="Value"/>.</param>
        private DateTime? UpdateValueFromComponent(DependencyProperty property)
        {
            // NOTE: Precision must be on OS tick level.

            if (property == YearProperty)
            {
                if (!Year.HasValue || !Value.HasValue)
                    return null;
                long ticks = new DateTime(Year.Value, Value.Value.Month, Math.Min(DateTime.DaysInMonth(Year.Value, Value.Value.Month), Value.Value.Day), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
                return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
            }

            if (property == MonthProperty)
            {
                if (!Month.HasValue || !Value.HasValue)
                    return null;
                long ticks = new DateTime(Value.Value.Year, Month.Value, Math.Min(DateTime.DaysInMonth(Value.Value.Year, Month.Value), Value.Value.Day), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
                return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
            }

            if (property == DayProperty)
            {
                if (!Day.HasValue || !Value.HasValue)
                    return null;
                long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Math.Min(DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month), Day.Value), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
                return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
            }

            if (property == HourProperty)
            {
                if (!Hour.HasValue || !Value.HasValue)
                    return null;
                long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Value.Value.Day, Hour.Value, Value.Value.Minute, 0).Ticks;
                return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
            }

            if (property == MinuteProperty)
            {
                if (!Minute.HasValue || !Value.HasValue)
                    return null;
                long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Minute.Value, 0).Ticks;
                return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
            }
            
            if (property == SecondProperty)
            {
                if (!Second.HasValue || !Value.HasValue)
                    return null;
                long ticks = Value.Value.Ticks - (Value.Value.Ticks % TimeSpan.TicksPerMinute);
                return new DateTime(ticks + (long)(Second.Value * TimeSpan.TicksPerSecond));
            }

            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <summary>
        /// Raised when the <see cref="Value"/> property is modified.
        /// </summary>
        private void OnValueValueChanged()
        {
            var isInitializing = !templateApplied && initializingProperty == null;
            if (isInitializing)
                initializingProperty = ValueProperty;

            if (!interlock)
            {
                interlock = true;
                UpdateComponentsFromValue(Value);
                interlock = false;
            }

            UpdateBinding(ValueProperty);
            if (isInitializing)
                initializingProperty = null;
        }

        /// <summary>
        /// Raised when either of the <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="Hour"/>, <see cref="Minute"/> or <see cref="Second"/> properties are modified.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnComponentPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var isInitializing = !templateApplied && initializingProperty == null;
            if (isInitializing)
                initializingProperty = e.Property;

            if (!interlock)
            {
                interlock = true;
                Value = UpdateValueFromComponent(e.Property);
                UpdateComponentsFromValue(Value);
                interlock = false;
            }

            UpdateBinding(e.Property);
            if (isInitializing)
                initializingProperty = null;
        }

        /// <summary>
        /// Updates the binding of the given dependency property.
        /// </summary>
        /// <param name="dependencyProperty">The dependency property.</param>
        private void UpdateBinding(DependencyProperty dependencyProperty)
        {
            if (dependencyProperty != initializingProperty)
            {
                var expression = GetBindingExpression(dependencyProperty);
                expression?.UpdateSource();
            }
        }

        /// <summary>
        /// Raised by <see cref="YearProperty"/>, <see cref="MonthProperty"/>, <see cref="DayProperty"/>, <see cref="HourProperty"/>, <see cref="MinuteProperty"/> or <see cref="SecondProperty"/> when the <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="Hour"/>, <see cref="Minute"/> or <see cref="Second"/> dependency property is modified.
        /// </summary>
        /// <param name="sender">The dependency object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private static void OnComponentPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var editor = (DateTimeEditor)sender;
            editor.OnComponentPropertyChanged(e);
        }

        /// <summary>
        /// Raised by <see cref="ValueProperty"/> when the <see cref="Value"/> dependency property is modified.
        /// </summary>
        /// <param name="sender">The dependency object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private static void OnValuePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var editor = (DateTimeEditor)sender;
            editor.OnValueValueChanged();
        }
    }
}
