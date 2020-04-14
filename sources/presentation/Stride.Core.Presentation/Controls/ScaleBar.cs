// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    public delegate void CustomRenderRoutedEventHandler(object sender, CustomRenderRoutedEventArgs e);

    public class CustomRenderRoutedEventArgs : RoutedEventArgs
    {
        public DrawingContext DrawingContext { get; private set; }

        public CustomRenderRoutedEventArgs(RoutedEvent routedEvent, DrawingContext drawingContext)
        {
            RoutedEvent = routedEvent;
            DrawingContext = drawingContext;
        }
    }

    public delegate void RoutedDependencyPropertyChangedEventHandler(object sender, RoutedDependencyPropertyChangedEventArgs e);

    public class RoutedDependencyPropertyChangedEventArgs : RoutedEventArgs
    {
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }
        public DependencyProperty DependencyProperty { get; private set; }

        public RoutedDependencyPropertyChangedEventArgs(RoutedEvent routedEvent, DependencyProperty dependencyProperty, object oldValue, object newValue)
        {
            RoutedEvent = routedEvent;
            DependencyProperty = dependencyProperty;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class ScaleBar : FrameworkElement
    {
        public static readonly DependencyProperty CustomDrawingContextProperty = DependencyProperty.Register(
            "CustomDrawingContext",
            typeof(DrawingContext),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background",
            typeof(Brush),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LargeTickPenProperty = DependencyProperty.Register(
            "LargeTickPen",
            typeof(Pen),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(new Pen(Brushes.Black, 1.0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SmallTickPenProperty = DependencyProperty.Register(
            "SmallTickPen",
            typeof(Pen),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(new Pen(Brushes.Gray, 1.0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LargeTickTopProperty = DependencyProperty.Register(
            "LargeTickTop",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.625, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LargeTickBottomProperty = DependencyProperty.Register(
            "LargeTickBottom",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SmallTickTopProperty = DependencyProperty.Register(
            "SmallTickTop",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.75, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SmallTickBottomProperty = DependencyProperty.Register(
            "SmallTickBottom",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DecimalCountRoundingProperty = DependencyProperty.Register(
            "DecimalCountRounding",
            typeof(int),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(6, null, CoerceDecimalCountRoundingPropertyValue));

        public static readonly DependencyProperty TextPositionOriginProperty = DependencyProperty.Register(
            "TextPositionOrigin",
            typeof(Point),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(new Point(0.5, 0.0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextPositionProperty = DependencyProperty.Register(
            "TextPosition",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground",
            typeof(Brush),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontProperty = DependencyProperty.Register(
            "Font",
            typeof(Typeface),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(new Typeface("Meiryo"), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            "FontSize",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(9.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StartUnitProperty = DependencyProperty.Register(
            "StartUnit",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MinimumUnitsPerTickProperty = DependencyProperty.Register(
            "MinimumUnitsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1e-12, FrameworkPropertyMetadataOptions.AffectsRender, OnUnitsPerTickPropertyChanged));

        public static readonly DependencyProperty MaximumUnitsPerTickProperty = DependencyProperty.Register(
            "MaximumUnitsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1e12, FrameworkPropertyMetadataOptions.AffectsRender, OnUnitsPerTickPropertyChanged));

        public static readonly DependencyProperty UnitsPerTickProperty = DependencyProperty.Register(
            "UnitsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnUnitsPerTickPropertyChanged, CoerceUnitsPerTickPropertyValue));

        public static readonly DependencyProperty PixelsPerTickProperty = DependencyProperty.Register(
            "PixelsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender, OnPixelsPerTickPropertyChanged, CoercePixelsPerTickPropertyValue));

        private static readonly DependencyPropertyKey AdjustedUnitsPerTickPropertyKey = DependencyProperty.RegisterReadOnly(
            "AdjustedUnitsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new PropertyMetadata());
        public static readonly DependencyProperty AdjustedUnitsPerTickProperty = AdjustedUnitsPerTickPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey AdjustedPixelsPerTickPropertyKey = DependencyProperty.RegisterReadOnly(
            "AdjustedPixelsPerTick",
            typeof(double),
            typeof(ScaleBar),
            new PropertyMetadata());
        public static readonly DependencyProperty AdjustedPixelsPerTickProperty = AdjustedPixelsPerTickPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PixelsPerUnitPropertyKey = DependencyProperty.RegisterReadOnly(
            "PixelsPerUnit",
            typeof(double),
            typeof(ScaleBar),
            new PropertyMetadata());
        public static readonly DependencyProperty PixelsPerUnitProperty = PixelsPerUnitPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey AdjustedPixelsPerUnitPropertyKey = DependencyProperty.RegisterReadOnly(
            "AdjustedPixelsPerUnit",
            typeof(double),
            typeof(ScaleBar),
            new PropertyMetadata());
        public static readonly DependencyProperty AdjustedPixelsPerUnitProperty = AdjustedPixelsPerUnitPropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsAliasedProperty = DependencyProperty.Register(
            "IsAliased",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, FrameworkPropertyMetadataOptions.AffectsRender, OnIsAliasedPropertyChanged));

        public static readonly DependencyProperty IsTextVisibleProperty = DependencyProperty.Register(
            "IsTextVisible",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsSmallTickVisibleProperty = DependencyProperty.Register(
            "IsSmallTickVisible",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsZoomingOnMouseWheelProperty = DependencyProperty.Register(
            "IsZoomingOnMouseWheel",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MouseWheelZoomCoeficientProperty = DependencyProperty.Register(
            "MouseWheelZoomCoeficient",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.1, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsDraggingOnLeftMouseButtonProperty = DependencyProperty.Register(
            "IsDraggingOnLeftMouseButton",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsDraggingOnRightMouseButtonProperty = DependencyProperty.Register(
            "IsDraggingOnRightMouseButton",
            typeof(bool),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitSystemProperty = DependencyProperty.Register(
            "UnitSystem",
            typeof(UnitSystem),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnUnitSystemPropertyChanged));

        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
            "UnitSymbol",
            typeof(string),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TickTextUnitDividerProperty = DependencyProperty.Register(
            "TickTextUnitDivider",
            typeof(double),
            typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));
    
        public static readonly RoutedEvent BeforeRenderEvent = EventManager.RegisterRoutedEvent(
            "BeforeRender",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ScaleBar));

        public static readonly RoutedEvent AfterRenderEvent = EventManager.RegisterRoutedEvent(
            "AfterRender",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ScaleBar));

        public static readonly RoutedEvent BeforeTicksRenderEvent = EventManager.RegisterRoutedEvent(
            "BeforeTicksRender",
            RoutingStrategy.Bubble,
            typeof(CustomRenderRoutedEventHandler),
            typeof(ScaleBar));

        public static readonly RoutedEvent AfterTicksRenderEvent = EventManager.RegisterRoutedEvent(
            "AfterTicksRender",
            RoutingStrategy.Bubble,
            typeof(CustomRenderRoutedEventHandler),
            typeof(ScaleBar));

        public static readonly RoutedEvent ScaleChangingEvent = EventManager.RegisterRoutedEvent(
            "ScaleChanging",
            RoutingStrategy.Bubble,
            typeof(RoutedDependencyPropertyChangedEventHandler),
            typeof(ScaleBar));

        public static readonly RoutedEvent ScaleChangedEvent = EventManager.RegisterRoutedEvent(
            "ScaleChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedDependencyPropertyChangedEventHandler),
            typeof(ScaleBar));

        public ScaleBar()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetValue(AdjustedUnitsPerTickPropertyKey, UnitsPerTick);
            SetValue(AdjustedPixelsPerTickPropertyKey, PixelsPerTick);
            RenderOptions.SetEdgeMode(this, IsAliased ? EdgeMode.Aliased : EdgeMode.Unspecified);
            InvalidateVisual();
        }

        public event RoutedEventHandler BeforeRender
        {
            add { AddHandler(BeforeRenderEvent, value); }
            remove { RemoveHandler(BeforeRenderEvent, value); }
        }

        public event RoutedEventHandler AfterRender
        {
            add { AddHandler(AfterRenderEvent, value); }
            remove { RemoveHandler(AfterRenderEvent, value); }
        }

        public event CustomRenderRoutedEventHandler BeforeTicksRender
        {
            add { AddHandler(BeforeTicksRenderEvent, value); }
            remove { RemoveHandler(BeforeTicksRenderEvent, value); }
        }

        public event CustomRenderRoutedEventHandler AfterTicksRender
        {
            add { AddHandler(AfterTicksRenderEvent, value); }
            remove { RemoveHandler(AfterTicksRenderEvent, value); }
        }

        public event RoutedDependencyPropertyChangedEventHandler ScaleChanging
        {
            add { AddHandler(ScaleChangingEvent, value); }
            remove { RemoveHandler(ScaleChangingEvent, value); }
        }

        public event RoutedDependencyPropertyChangedEventHandler ScaleChanged
        {
            add { AddHandler(ScaleChangedEvent, value); }
            remove { RemoveHandler(ScaleChangedEvent, value); }
        }

        private void RaiseBeforeRenderEvent()
        {
            RaiseEvent(new RoutedEventArgs(BeforeRenderEvent));
        }

        private void RaiseAfterRenderEvent()
        {
            RaiseEvent(new RoutedEventArgs(AfterRenderEvent));
        }

        private void RaiseBeforeTicksRenderEvent(DrawingContext drawingContext)
        {
            RaiseEvent(new CustomRenderRoutedEventArgs(BeforeTicksRenderEvent, drawingContext));
        }

        private void RaiseAfterTicksRenderEvent(DrawingContext drawingContext)
        {
            RaiseEvent(new CustomRenderRoutedEventArgs(AfterTicksRenderEvent, drawingContext));
        }

        private void RaiseScaleChangingEvent(DependencyProperty dependencyProperty, object oldValue, object newValue)
        {
            RaiseEvent(new RoutedDependencyPropertyChangedEventArgs(ScaleChangingEvent, dependencyProperty, oldValue, newValue));
        }

        private void RaiseScaleChangedEvent(DependencyProperty dependencyProperty, object oldValue, object newValue)
        {
            RaiseEvent(new RoutedDependencyPropertyChangedEventArgs(ScaleChangedEvent, dependencyProperty, oldValue, newValue));
        }

        private void SetScaleChangingProperty([NotNull] DependencyProperty dependencyProperty, object value)
        {
            var oldValue = GetValue(dependencyProperty);
            RaiseScaleChangingEvent(dependencyProperty, oldValue, value);
            SetValue(dependencyProperty, value);
            RaiseScaleChangedEvent(dependencyProperty, oldValue, value);
        }

        private void SetScaleChangingProperty([NotNull] DependencyPropertyKey dependencyPropertyKey, [NotNull] DependencyProperty dependencyProperty, object value)
        {
            var oldValue = GetValue(dependencyProperty);
            RaiseScaleChangingEvent(dependencyProperty, oldValue, value);
            SetValue(dependencyPropertyKey, value);
            RaiseScaleChangedEvent(dependencyProperty, oldValue, value);
        }

        public DrawingContext CustomDrawingContext
        {
            get { return (DrawingContext)GetValue(CustomDrawingContextProperty); }
            set { SetValue(CustomDrawingContextProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public Pen LargeTickPen
        {
            get { return (Pen)GetValue(LargeTickPenProperty); }
            set { SetValue(LargeTickPenProperty, value); }
        }

        public Pen SmallTickPen
        {
            get { return (Pen)GetValue(SmallTickPenProperty); }
            set { SetValue(SmallTickPenProperty, value); }
        }

        /// <summary>
        /// Gets or sets the relative top (Y) coordinate of the drawn large ticks. This is a dependency property.
        /// </summary>
        /// <remarks>The coordinate is relative, that means 0.0 is top and 1.0 is bottom.
        /// The coordinate can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public double LargeTickTop
        {
            get { return (double)GetValue(LargeTickTopProperty); }
            set { SetValue(LargeTickTopProperty, value); }
        }

        /// <summary>
        /// Gets or sets the relative bottom (Y) coordinate of the drawn large ticks. This is a dependency property.
        /// </summary>
        /// <remarks>The coordinate is relative, that means 0.0 is top and 1.0 is bottom.
        /// The coordinate can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public double LargeTickBottom
        {
            get { return (double)GetValue(LargeTickBottomProperty); }
            set { SetValue(LargeTickBottomProperty, value); }
        }

        /// <summary>
        /// Gets or sets the relative top (Y) coordinate of the drawn small ticks. This is a dependency property.
        /// </summary>
        /// <remarks>The coordinate is relative, that means 0.0 is top and 1.0 is bottom.
        /// The coordinate can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public double SmallTickTop
        {
            get { return (double)GetValue(SmallTickTopProperty); }
            set { SetValue(SmallTickTopProperty, value); }
        }

        /// <summary>
        /// Gets or sets the relative bottom (Y) coordinate of the drawn small ticks. This is a dependency property.
        /// </summary>
        /// <remarks>The coordinate is relative, that means 0.0 is top and 1.0 is bottom.
        /// The coordinate can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public double SmallTickBottom
        {
            get { return (double)GetValue(SmallTickBottomProperty); }
            set { SetValue(SmallTickBottomProperty, value); }
        }

        public int DecimalCountRounding
        {
            get { return (int)GetValue(DecimalCountRoundingProperty); }
            set { SetValue(DecimalCountRoundingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the center point of drawn text, relative to the bounds of the drawn text itself. This is a dependency property.
        /// </summary>
        /// <remarks>Each coordinate axis can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public Point TextPositionOrigin
        {
            get { return (Point)GetValue(TextPositionOriginProperty); }
            set { SetValue(TextPositionOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the relative top (Y) coordinate of the center of the drawn text. This is a dependency property.
        /// </summary>
        /// <remarks>The coordinate is relative, that means 0.0 is top and 1.0 is bottom.
        /// The coordinate can be set to less than 0.0 or more than 1.0 where additional offset is needed.</remarks>
        public double TextPosition
        {
            get { return (double)GetValue(TextPositionProperty); }
            set { SetValue(TextPositionProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Typeface Font
        {
            get { return (Typeface)GetValue(FontProperty); }
            set { SetValue(FontProperty, value); }
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public double StartUnit
        {
            get { return (double)GetValue(StartUnitProperty); }
            set { SetScaleChangingProperty(StartUnitProperty, value); }
        }

        public double MinimumUnitsPerTick
        {
            get { return (double)GetValue(MinimumUnitsPerTickProperty); }
            set { SetScaleChangingProperty(MinimumUnitsPerTickProperty, value); }
        }

        public double MaximumUnitsPerTick
        {
            get { return (double)GetValue(MaximumUnitsPerTickProperty); }
            set { SetScaleChangingProperty(MaximumUnitsPerTickProperty, value); }
        }

        public double UnitsPerTick
        {
            get { return (double)GetValue(UnitsPerTickProperty); }
            set { SetScaleChangingProperty(UnitsPerTickProperty, value); }
        }

        public double AdjustedUnitsPerTick
        {
            get { return (double)GetValue(AdjustedUnitsPerTickProperty); }
            private set { SetValue(AdjustedUnitsPerTickPropertyKey, value); }
        }

        public double PixelsPerTick
        {
            get { return (double)GetValue(PixelsPerTickProperty); }
            set { SetScaleChangingProperty(PixelsPerTickProperty, value); }
        }

        public double AdjustedPixelsPerTick
        {
            get { return (double)GetValue(AdjustedPixelsPerTickProperty); }
            private set { SetValue(AdjustedPixelsPerTickPropertyKey, value); }
        }

        public double PixelsPerUnit
        {
            get { return (double)GetValue(PixelsPerUnitProperty); }
            private set { SetScaleChangingProperty(PixelsPerUnitPropertyKey, PixelsPerUnitProperty, value); }
        }

        public double AdjustedPixelsPerUnit
        {
            get { return (double)GetValue(AdjustedPixelsPerUnitProperty); }
            private set { SetValue(AdjustedPixelsPerUnitPropertyKey, value); }
        }

        public bool IsAliased
        {
            get { return (bool)GetValue(IsAliasedProperty); }
            set { SetValue(IsAliasedProperty, value); }
        }

        public bool IsTextVisible
        {
            get { return (bool)GetValue(IsTextVisibleProperty); }
            set { SetValue(IsTextVisibleProperty, value); }
        }

        public bool IsSmallTickVisible
        {
            get { return (bool)GetValue(IsSmallTickVisibleProperty); }
            set { SetValue(IsSmallTickVisibleProperty, value); }
        }

        public bool IsZoomingOnMouseWheel
        {
            get { return (bool)GetValue(IsZoomingOnMouseWheelProperty); }
            set { SetValue(IsZoomingOnMouseWheelProperty, value); }
        }

        public double MouseWheelZoomCoeficient
        {
            get { return (double)GetValue(MouseWheelZoomCoeficientProperty); }
            set { SetValue(MouseWheelZoomCoeficientProperty, value); }
        }

        public bool IsDraggingOnLeftMouseButton
        {
            get { return (bool)GetValue(IsDraggingOnLeftMouseButtonProperty); }
            set { SetValue(IsDraggingOnLeftMouseButtonProperty, value); }
        }

        public bool IsDraggingOnRightMouseButton
        {
            get { return (bool)GetValue(IsDraggingOnRightMouseButtonProperty); }
            set { SetValue(IsDraggingOnRightMouseButtonProperty, value); }
        }

        public UnitSystem UnitSystem
        {
            get { return (UnitSystem)GetValue(UnitSystemProperty); }
            set { SetValue(UnitSystemProperty, value); }
        }

        public string UnitSymbol
        {
            get { return (string)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        public double TickTextUnitDivider
        {
            get { return (double)GetValue(TickTextUnitDividerProperty); }
            set { SetValue(TickTextUnitDividerProperty, value); }
        }

        private void UpdatePixelInfo()
        {
            AdjustedPixelsPerTick = PixelsPerTick * AdjustedUnitsPerTick / UnitsPerTick;
            AdjustedPixelsPerUnit = AdjustedPixelsPerTick / AdjustedUnitsPerTick;
            PixelsPerUnit = PixelsPerTick / UnitsPerTick;
        }

        private static void OnUnitSystemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scalebar = (ScaleBar)sender;
            scalebar.AdjustUnitIntervalWithUnitSystem(scalebar.UnitsPerTick);
            scalebar.UpdatePixelInfo();
        }

        private static void OnUnitsPerTickPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scalebar = (ScaleBar)sender;
            scalebar.AdjustUnitIntervalWithUnitSystem((double)e.NewValue);
            scalebar.UpdatePixelInfo();
        }

        private static void OnPixelsPerTickPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scalebar = (ScaleBar)sender;
            scalebar.UpdatePixelInfo();
        }

        private static void OnIsAliasedPropertyChanged([NotNull] DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RenderOptions.SetEdgeMode(sender, (bool)e.NewValue ? EdgeMode.Aliased : EdgeMode.Unspecified);
        }

        private static object CoerceUnitsPerTickPropertyValue(DependencyObject sender, object value)
        {
            var scalebar = (ScaleBar)sender;
            return scalebar.MinimumUnitsPerTick < scalebar.MaximumUnitsPerTick ? Math.Min(scalebar.MaximumUnitsPerTick, Math.Max(scalebar.MinimumUnitsPerTick, (double)value)) : value;
        }

        [NotNull]
        private static object CoercePixelsPerTickPropertyValue(DependencyObject sender, [NotNull] object value)
        {
            return Math.Max(10.0, (double)value);
        }

        [NotNull]
        private static object CoerceDecimalCountRoundingPropertyValue(DependencyObject sender, object value)
        {
            return Math.Max(0, 12);
        }

        public double GetPixelAt(double unit)
        {
            return ((unit - StartUnit) * AdjustedPixelsPerTick) / AdjustedUnitsPerTick;
        }

        public double GetUnitAt(double pixel)
        {
            return StartUnit + (pixel * AdjustedUnitsPerTick) / AdjustedPixelsPerTick;
        }

        public void SetUnitAt(double unit, double pixel)
        {
            StartUnit = unit - (pixel * AdjustedUnitsPerTick) / AdjustedPixelsPerTick;
            InvalidateVisual();
        }

        public void SetUnitsPerTickAt(double unitsPerTick, double pixel)
        {
            var unit = GetUnitAt(pixel);
            UnitsPerTick = unitsPerTick;
            SetUnitAt(unit, pixel);
        }

        public void SetPixelsPerTickAt(double pixelsPerTick, double pixel)
        {
            var unit = GetUnitAt(pixel);
            PixelsPerTick = pixelsPerTick;
            SetUnitAt(unit, pixel);
        }

        private Pen largeTickPen;
        private Pen smallTickPen;

        private Point textPositionOrigin;
        private double textPosition;
        private bool isTextVisible;
        private Brush foreground;
        private Typeface font;
        private double fontSize;

        private double actualWidth;
        private double actualHeight;

        private double largeTickTopPosition;
        private double largeTickBottomPosition;
        private double smallTickTopPosition;
        private double smallTickBottomPosition;

        private int adjustedSmallIntervalPerTick = 10;

        protected override void OnRender(DrawingContext localDrawingContext)
        {
            var drawingContext = CustomDrawingContext ?? localDrawingContext;

            if (AdjustedPixelsPerTick.Equals(0.0))
                SetValue(AdjustedPixelsPerTickPropertyKey, PixelsPerTick);

            actualWidth = ActualWidth;
            actualHeight = ActualHeight;

            largeTickTopPosition = actualHeight * LargeTickTop;
            largeTickBottomPosition = actualHeight * LargeTickBottom;
            smallTickTopPosition = actualHeight * SmallTickTop;
            smallTickBottomPosition = actualHeight * SmallTickBottom;

            largeTickPen = LargeTickPen;
            smallTickPen = SmallTickPen;

            var isSmallTickVisible = IsSmallTickVisible;

            textPositionOrigin = TextPositionOrigin;
            textPosition = TextPosition;

            isTextVisible = IsTextVisible;
            if (isTextVisible)
            {
                foreground = Foreground;
                font = Font;
                fontSize = FontSize;
            }

            var adjustedUnitsPerTick = AdjustedUnitsPerTick;
            var adjustedPixelsPerTick = AdjustedPixelsPerTick;
            var decimalCountRounding = DecimalCountRounding;

            var currentUnit = (int)(StartUnit / adjustedUnitsPerTick) * adjustedUnitsPerTick;
            var currentPixel = ((currentUnit - StartUnit) / adjustedUnitsPerTick) * adjustedPixelsPerTick;

            var smallIntevalLength = (1.0 / adjustedSmallIntervalPerTick);

            //if (StartUnit >= 0.0)
            //{
            //    currentPixel += adjustedPixelsPerTick;
            //    currentUnit += adjustedUnitsPerTick;
            //    currentUnit = Math.Round(currentUnit, decimalCountRounding);
            //}

            RaiseBeforeRenderEvent();

            drawingContext.DrawRectangle(Background, null, new Rect(0.0, 0.0, actualWidth, actualHeight));

            RaiseBeforeTicksRenderEvent(drawingContext);

            if (isSmallTickVisible)
            {
                for (var i = 0; i < adjustedSmallIntervalPerTick - 1; i++)
                {
                    var smallLeft = currentPixel - ((i + 1) * adjustedPixelsPerTick) * smallIntevalLength;
                    if (smallLeft < 0.0)
                        break;
                    DrawSmallTick(drawingContext, smallLeft);
                }
            }

            if (currentPixel < 0.0)
            {
                currentPixel += adjustedPixelsPerTick * Math.Ceiling(Math.Abs(currentPixel) / adjustedPixelsPerTick);
            }

            while (currentPixel < actualWidth)
            {
                DrawLargeTick(drawingContext, currentUnit, currentPixel + 1.0);

                if (isSmallTickVisible)
                {
                    for (var i = 0; i < adjustedSmallIntervalPerTick - 1; i++)
                    {
                        var smallLeft = currentPixel + ((i + 1) * adjustedPixelsPerTick) * smallIntevalLength;
                        if (smallLeft > actualWidth)
                            break;
                        DrawSmallTick(drawingContext, smallLeft + 1.0);
                    }
                }

                currentPixel += adjustedPixelsPerTick;
                currentUnit += adjustedUnitsPerTick;
            }

            RaiseAfterTicksRenderEvent(drawingContext);

            RaiseAfterRenderEvent();
        }

        private static double AdjustUnitInterval(double value)
        {
            // computing cannot be done on negative values
            var negative = (value <= 0.0f);
            if (negative)
                value = -value;

            var log = Math.Log10(value);
            var log0 = Math.Pow(10.0, Math.Floor(log));

            double result;

            log = value / log0;
            if (log < (1.0f + 2.0f) * 0.5f) result = log0;
            else if (log < (2.0f + 5.0f) * 0.5f) result = log0 * 2.0f;
            else if (log < (5.0f + 10.0f) * 0.5f) result = log0 * 5.0f;
            else result = log0 * 10.0f;

            if (negative)
                result = -result;

            return result;
        }

        private static bool IsCloser(double value, double other, double reference)
        {
            return Math.Abs(value - reference) < Math.Abs(other - reference);
        }

        private static bool IsCloseEnoughToMultiply([NotNull] List<double> sortedGroupings, double value, double target)
        {
            var result = true;
            var index = sortedGroupings.FindIndex(x => x.Equals(value));

            if (index > 0 && sortedGroupings[index - 1] > target)
                result = false;

            if (index < sortedGroupings.Count - 1 && sortedGroupings[index + 1] < target)
                result = false;

            return result;
        }

        private void AdjustUnitIntervalWithUnitSystem(double value)
        {
            TickTextUnitDivider = 1.0;
            
            if (UnitSystem == null)
            {
                AdjustedUnitsPerTick = AdjustUnitInterval(value);
                return;
            }

            UnitSymbol = UnitSystem.Symbol;

            var scaledValue = value * 1.5;
            var referenceUnitsPerTick = AdjustedUnitsPerTick;
            var hasResult = false;
            var allGrouping = new List<double>();
            UnitSystem.GetAllGroupingValues(ref allGrouping);
            allGrouping.Sort();

            // Check if there is a grouping matching our value
            foreach (var grouping in UnitSystem.GroupingValues)
            {
                if (!hasResult || IsCloser(grouping.LargeIntervalSize, referenceUnitsPerTick, scaledValue))
                {
                    AdjustedUnitsPerTick = grouping.LargeIntervalSize;
                    referenceUnitsPerTick = AdjustedUnitsPerTick;
                    adjustedSmallIntervalPerTick = grouping.SmallIntervalCount;
                    hasResult = true;
                }

                // If the grouping is multipliable using the default grouping method ({1/2/5}*10^n), check for a better value
                if (grouping.IsMultipliable)
                {
                    var val = AdjustUnitInterval(scaledValue / grouping.LargeIntervalSize) * grouping.LargeIntervalSize;

                    if (IsCloseEnoughToMultiply(allGrouping, grouping.LargeIntervalSize, scaledValue) && IsCloser(val, referenceUnitsPerTick, scaledValue))
                    {
                        AdjustedUnitsPerTick = val;
                        referenceUnitsPerTick = grouping.LargeIntervalSize;
                        adjustedSmallIntervalPerTick = grouping.SmallIntervalCount;
                    }
                }
            }

            // When there is no grouping, use the default grouping method
            if (UnitSystem.GroupingValues.Count == 0)
            {
                AdjustedUnitsPerTick = AdjustUnitInterval(scaledValue);
                referenceUnitsPerTick = 1;
                adjustedSmallIntervalPerTick = 10;
            }

            // Check if a conversion may fit better our scale
            foreach (var conversion in UnitSystem.Conversions)
            {
                // Check if there is a grouping matching our value
                foreach (var grouping in conversion.GroupingValues)
                {
                    var groupingValue = grouping.LargeIntervalSize * conversion.Value;

                    if (IsCloser(groupingValue, referenceUnitsPerTick, scaledValue))
                    {
                        AdjustedUnitsPerTick = groupingValue;
                        referenceUnitsPerTick = groupingValue;
                        adjustedSmallIntervalPerTick = grouping.SmallIntervalCount;
                        TickTextUnitDivider = conversion.Value;
                        UnitSymbol = conversion.Symbol;
                    }

                    // If the grouping is multipliable using the default grouping method ({1/2/5}*10^n), check for a better value
                    if (grouping.IsMultipliable)
                    {
                        var val = AdjustUnitInterval(scaledValue / groupingValue) * groupingValue;

                        if (IsCloseEnoughToMultiply(allGrouping, groupingValue, scaledValue) && IsCloser(val, referenceUnitsPerTick, scaledValue))
                        {
                            AdjustedUnitsPerTick = val;
                            referenceUnitsPerTick = groupingValue;
                            adjustedSmallIntervalPerTick = grouping.SmallIntervalCount;
                            TickTextUnitDivider = conversion.Value;
                            UnitSymbol = conversion.Symbol;
                        }
                    }
                }

                // When there is no grouping, use the default grouping method
                if (conversion.GroupingValues.Count == 0)
                {
                    var val = conversion.Value;
                    var canMultiply = true;
                    if (conversion.IsMultipliable)
                    {
                        canMultiply = IsCloseEnoughToMultiply(allGrouping, conversion.Value, scaledValue);
                        val *= AdjustUnitInterval(scaledValue / conversion.Value);
                    }
                    if (canMultiply && IsCloser(val, referenceUnitsPerTick, scaledValue))
                    {
                        AdjustedUnitsPerTick = val;
                        referenceUnitsPerTick = conversion.Value;
                        adjustedSmallIntervalPerTick = 10;
                        TickTextUnitDivider = conversion.Value;
                        UnitSymbol = conversion.Symbol;
                    }
                }
            }
        }

        protected virtual void DrawLargeTick([NotNull] DrawingContext drawingContext, double unit, double position)
        {
            if (isTextVisible)
            {
                var symbol = UnitSymbol ?? "";
                var dividedUnit = !TickTextUnitDivider.Equals(0.0) ? unit / TickTextUnitDivider : unit;
                dividedUnit = Math.Round(dividedUnit, 6);

                var ft = new FormattedText(dividedUnit + symbol, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, fontSize, foreground);
                drawingContext.DrawText(ft, new Point(position - ft.Width * textPositionOrigin.X, (textPosition * actualHeight) - (ft.Height * textPositionOrigin.Y)));
            }

            drawingContext.DrawLine(largeTickPen, new Point(position, largeTickTopPosition), new Point(position, largeTickBottomPosition));
        }

        protected virtual void DrawSmallTick([NotNull] DrawingContext drawingContext, double position)
        {
            drawingContext.DrawLine(smallTickPen, new Point(position, smallTickTopPosition), new Point(position, smallTickBottomPosition));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (IsZoomingOnMouseWheel)
            {
                var coeficient = e.Delta >= 0.0 ? MouseWheelZoomCoeficient : 1.0 / MouseWheelZoomCoeficient;
                var pos = e.GetPosition(this);

                ZoomAtPosition(pos.X, coeficient, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            }

            e.Handled = true;
        }

        public void ZoomAtPosition(double position, double coeficient, bool affectPixelsPerTick)
        {
            if (affectPixelsPerTick)
                SetPixelsPerTickAt(PixelsPerTick * coeficient, position);
            else
                SetUnitsPerTickAt(UnitsPerTick / coeficient, position);
        }

        private bool isDraggingScale;

        public bool StartDraggingScale()
        {
            if (isDraggingScale)
                return true;

            isDraggingScale = CaptureMouse();

            mouseDelta = Mouse.GetPosition(this);
            return isDraggingScale;
        }

        public bool EndDraggingScale()
        {
            if (!isDraggingScale)
                return true;

            isDraggingScale = !Mouse.Capture(null);
            return !isDraggingScale;
        }

        private Point mouseDelta;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDraggingScale)
            {
                var delta = e.GetPosition(this) - mouseDelta;
                mouseDelta = e.GetPosition(this);
                StartUnit -= delta.X / PixelsPerUnit;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsDraggingOnLeftMouseButton)
            {
                StartDraggingScale();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (IsDraggingOnLeftMouseButton)
            {
                EndDraggingScale();
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (IsDraggingOnRightMouseButton)
            {
                StartDraggingScale();
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (IsDraggingOnRightMouseButton)
            {
                EndDraggingScale();
            }
        }
    }
}
