// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// The time scale above the slider of the animation preview: a tick per step of time from <see cref="Minimum"/> to
/// <see cref="Maximum"/>, a label on every major tick. It takes the place of the WPF preview's <c>ScaleBar</c>, which the
/// animation preview used without its zoom and drag.
/// </summary>
/// <remarks>
/// The scale spans the width of the control less <see cref="Inset"/> on each side, the way a slider's values span its track
/// less half its thumb on each side.
/// </remarks>
public sealed class AnimationTimeRuler : Control
{
    /// <summary>
    /// The steps between two major ticks, in units; the ruler takes the first that leaves room for a label.
    /// </summary>
    private static readonly double[] Steps = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 30, 60, 120, 300, 600];

    private const double MinimumMajorTickSpacing = 50;
    private const double MajorTickLength = 8;
    private const double MinorTickLength = 4;
    private const int MinorTicksPerMajorTick = 5;

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<AnimationTimeRuler, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<AnimationTimeRuler, double>(nameof(Maximum));

    public static readonly StyledProperty<double> InsetProperty =
        AvaloniaProperty.Register<AnimationTimeRuler, double>(nameof(Inset));

    public static readonly StyledProperty<string> SymbolProperty =
        AvaloniaProperty.Register<AnimationTimeRuler, string>(nameof(Symbol), "s");

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<AnimationTimeRuler>();

    static AnimationTimeRuler()
    {
        AffectsRender<AnimationTimeRuler>(MinimumProperty, MaximumProperty, InsetProperty, SymbolProperty, ForegroundProperty);
    }

    /// <summary>
    /// The value at the start of the scale.
    /// </summary>
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>
    /// The value at the end of the scale.
    /// </summary>
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>
    /// The space left out of the scale on each side, in pixels.
    /// </summary>
    public double Inset { get => GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    /// <summary>
    /// The symbol of the unit, written after each label.
    /// </summary>
    public string Symbol { get => GetValue(SymbolProperty); set => SetValue(SymbolProperty, value); }

    /// <summary>
    /// The brush of the ticks and labels.
    /// </summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>
    /// The value between two major ticks for the current size and range, or <c>0</c> when the ruler draws nothing.
    /// </summary>
    public double MajorStep
    {
        get
        {
            var range = Maximum - Minimum;
            var width = Bounds.Width - 2 * Inset;
            if (!(range > 0) || double.IsInfinity(range) || width <= 0)
                return 0;

            var pixelsPerUnit = width / range;
            foreach (var step in Steps)
            {
                if (step * pixelsPerUnit >= MinimumMajorTickSpacing)
                    return step;
            }
            return Steps[^1];
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(0, 25);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var step = MajorStep;
        if (step <= 0)
            return;

        var brush = Foreground ?? Brushes.Gray;
        var pen = new Pen(brush, 1);
        var width = Bounds.Width - 2 * Inset;
        var height = Bounds.Height;
        var pixelsPerUnit = width / (Maximum - Minimum);
        var minorStep = step / MinorTicksPerMajorTick;
        var typeface = new Typeface(TextElement.GetFontFamily(this));
        var fontSize = TextElement.GetFontSize(this) * 0.85;

        // The ticks start at the first multiple of the minor step from the minimum; index arithmetic keeps them exact
        var first = (long)Math.Ceiling(Minimum / minorStep - 1e-9);
        var last = (long)Math.Floor(Maximum / minorStep + 1e-9);
        for (var i = first; i <= last; ++i)
        {
            var value = i * minorStep;
            var x = Math.Round(Inset + (value - Minimum) * pixelsPerUnit) + 0.5;
            var isMajor = i % MinorTicksPerMajorTick == 0;
            var length = isMajor ? MajorTickLength : MinorTickLength;
            context.DrawLine(pen, new Point(x, height - length), new Point(x, height));
            if (!isMajor)
                continue;

            var text = new FormattedText(value.ToString("0.##", CultureInfo.CurrentCulture) + Symbol, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, fontSize, brush);
            var left = Math.Clamp(x - text.Width / 2, 0, Math.Max(0, Bounds.Width - text.Width));
            context.DrawText(text, new Point(left, height - MajorTickLength - text.Height));
        }
    }
}
