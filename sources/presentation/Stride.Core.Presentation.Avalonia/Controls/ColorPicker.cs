// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Extensions;

using Point = Avalonia.Point;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// Picks a color with a hue strip and a saturation and brightness square, or by typing its RGBA or HSB components.
/// </summary>
/// <remarks>
/// <see cref="Color"/> is written only when the user picks a color: when a drag on the square or on the strip ends, or
/// when a component is typed. Showing a color, and the steps of a drag, write nothing.
/// </remarks>
[TemplatePart("PART_ColorSurface", typeof(Panel))]
[TemplatePart("PART_ColorSelector", typeof(Control))]
[TemplatePart("PART_HueSurface", typeof(Control))]
[TemplatePart("PART_HueSelector", typeof(Control))]
[TemplatePart("PART_Preview", typeof(Shape))]
public sealed class ColorPicker : TemplatedControl
{
    // A component changes the others: they show the same color and must not apply it again
    private bool isSyncingComponents;
    // The control writes the color itself: the new value is the shown one
    private bool isCommitting;

    static ColorPicker()
    {
        ColorProperty.Changed.AddClassHandler<ColorPicker>((x, _) => x.OnColorChanged());
        HueProperty.Changed.AddClassHandler<ColorPicker>((x, _) => x.OnHsbComponentChanged());
        SaturationProperty.Changed.AddClassHandler<ColorPicker>((x, _) => x.OnHsbComponentChanged());
        BrightnessProperty.Changed.AddClassHandler<ColorPicker>((x, _) => x.OnHsbComponentChanged());
        RedProperty.Changed.AddClassHandler<ColorPicker>((x, e) => x.OnRgbaComponentChanged(e.Property));
        GreenProperty.Changed.AddClassHandler<ColorPicker>((x, e) => x.OnRgbaComponentChanged(e.Property));
        BlueProperty.Changed.AddClassHandler<ColorPicker>((x, e) => x.OnRgbaComponentChanged(e.Property));
        AlphaProperty.Changed.AddClassHandler<ColorPicker>((x, e) => x.OnRgbaComponentChanged(e.Property));
    }

    public static readonly StyledProperty<Color4> ColorProperty =
        AvaloniaProperty.Register<ColorPicker, Color4>(nameof(Color), new Color4(0f, 0f, 0f, 1f), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> HueProperty =
        AvaloniaProperty.Register<ColorPicker, float>(nameof(Hue), coerce: CoerceHue);

    public static readonly StyledProperty<float> SaturationProperty =
        AvaloniaProperty.Register<ColorPicker, float>(nameof(Saturation), coerce: CoercePercentage);

    public static readonly StyledProperty<float> BrightnessProperty =
        AvaloniaProperty.Register<ColorPicker, float>(nameof(Brightness), coerce: CoercePercentage);

    public static readonly StyledProperty<byte> RedProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(Red));

    public static readonly StyledProperty<byte> GreenProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(Green));

    public static readonly StyledProperty<byte> BlueProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(Blue));

    public static readonly StyledProperty<byte> AlphaProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(Alpha), 255);

    public static readonly StyledProperty<bool> ShowAlphaProperty =
        AvaloniaProperty.Register<ColorPicker, bool>(nameof(ShowAlpha), true);

    /// <summary>
    /// Gets or sets the picked color.
    /// </summary>
    public Color4 Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the hue of the shown color, in degrees from 0 to 360.
    /// </summary>
    public float Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>
    /// Gets or sets the saturation of the shown color, from 0 to 100.
    /// </summary>
    public float Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>
    /// Gets or sets the brightness of the shown color, from 0 to 100.
    /// </summary>
    public float Brightness
    {
        get => GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    /// <summary>
    /// Gets or sets the red component of the shown color, from 0 to 255.
    /// </summary>
    public byte Red
    {
        get => GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    /// <summary>
    /// Gets or sets the green component of the shown color, from 0 to 255.
    /// </summary>
    public byte Green
    {
        get => GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    /// <summary>
    /// Gets or sets the blue component of the shown color, from 0 to 255.
    /// </summary>
    public byte Blue
    {
        get => GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    /// <summary>
    /// Gets or sets the alpha component of the shown color, from 0 to 255.
    /// </summary>
    public byte Alpha
    {
        get => GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the alpha component can be edited.
    /// </summary>
    public bool ShowAlpha
    {
        get => GetValue(ShowAlphaProperty);
        set => SetValue(ShowAlphaProperty, value);
    }

    /// <summary>
    /// Gets the shown color, with its full precision: the components round it to a byte or a percent.
    /// </summary>
    public Color4 ShownColor { get; private set; } = new(0f, 0f, 0f, 1f);

    private Panel? ColorSurface { get; set; }

    private Control? ColorSelector { get; set; }

    private Control? HueSurface { get; set; }

    private Control? HueSelector { get; set; }

    private Shape? Preview { get; set; }

    /// <summary>
    /// Gets the surface the pointer drags on, or <c>null</c> if no drag is in progress.
    /// </summary>
    private Control? DraggedSurface { get; set; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        foreach (var surface in new[] { ColorSurface, HueSurface })
        {
            if (surface is null)
                continue;
            surface.PointerPressed -= OnSurfacePointerPressed;
            surface.PointerMoved -= OnSurfacePointerMoved;
            surface.PointerReleased -= OnSurfacePointerReleased;
            surface.PointerCaptureLost -= OnSurfacePointerCaptureLost;
            surface.SizeChanged -= OnSurfaceSizeChanged;
        }

        ColorSurface = e.NameScope.Find<Panel>("PART_ColorSurface");
        ColorSelector = e.NameScope.Find<Control>("PART_ColorSelector");
        HueSurface = e.NameScope.Find<Control>("PART_HueSurface");
        HueSelector = e.NameScope.Find<Control>("PART_HueSelector");
        Preview = e.NameScope.Find<Shape>("PART_Preview");

        foreach (var surface in new[] { ColorSurface, HueSurface })
        {
            if (surface is null)
                continue;
            surface.PointerPressed += OnSurfacePointerPressed;
            surface.PointerMoved += OnSurfacePointerMoved;
            surface.PointerReleased += OnSurfacePointerReleased;
            surface.PointerCaptureLost += OnSurfacePointerCaptureLost;
            surface.SizeChanged += OnSurfaceSizeChanged;
        }

        UpdateVisuals();
    }

    private void OnColorChanged()
    {
        if (isCommitting)
            return;

        ShownColor = Color;
        isSyncingComponents = true;
        try
        {
            SetHsbComponents();
            SetRgbaComponents();
        }
        finally
        {
            isSyncingComponents = false;
        }
        UpdateVisuals();
    }

    private void OnHsbComponentChanged()
    {
        if (!isSyncingComponents)
            ApplyHsbComponents();
    }

    private void ApplyHsbComponents()
    {
        ShownColor = new ColorHSV(Hue, Saturation / 100f, Brightness / 100f, ShownColor.A).ToColor();
        isSyncingComponents = true;
        try
        {
            SetRgbaComponents();
        }
        finally
        {
            isSyncingComponents = false;
        }
        UpdateVisuals();
        if (DraggedSurface is null)
            Commit();
    }

    private void OnRgbaComponentChanged(AvaloniaProperty property)
    {
        if (isSyncingComponents)
            return;

        // Only the typed component changes: the others keep their full precision
        var color = ShownColor;
        if (property == RedProperty)
            color.R = Red / 255f;
        else if (property == GreenProperty)
            color.G = Green / 255f;
        else if (property == BlueProperty)
            color.B = Blue / 255f;
        else
            color.A = Alpha / 255f;
        ShownColor = color;

        isSyncingComponents = true;
        try
        {
            SetHsbComponents();
        }
        finally
        {
            isSyncingComponents = false;
        }
        UpdateVisuals();
        Commit();
    }

    private void SetHsbComponents()
    {
        var hsv = ColorHSV.FromColor(ShownColor);
        // A gray has no hue, and black no saturation either: keep the ones shown, so that picking a gray on the square
        // and moving back does not lose them
        if (hsv.V > 0f)
        {
            if (hsv.S > 0f)
                SetCurrentValue(HueProperty, hsv.H);
            SetCurrentValue(SaturationProperty, hsv.S * 100f);
        }
        SetCurrentValue(BrightnessProperty, hsv.V * 100f);
    }

    private void SetRgbaComponents()
    {
        SetCurrentValue(RedProperty, ToByte(ShownColor.R));
        SetCurrentValue(GreenProperty, ToByte(ShownColor.G));
        SetCurrentValue(BlueProperty, ToByte(ShownColor.B));
        SetCurrentValue(AlphaProperty, ToByte(ShownColor.A));
    }

    /// <summary>
    /// Writes the shown color to <see cref="Color"/>.
    /// </summary>
    private void Commit()
    {
        if (ShownColor == Color)
            return;

        isCommitting = true;
        try
        {
            SetCurrentValue(ColorProperty, ShownColor);
        }
        finally
        {
            isCommitting = false;
        }
    }

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control surface || !e.GetCurrentPoint(surface).Properties.IsLeftButtonPressed)
            return;

        DraggedSurface = surface;
        e.Pointer.Capture(surface);
        PickAt(surface, e.GetPosition(surface));
        e.Handled = true;
    }

    private void OnSurfacePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control surface && surface == DraggedSurface)
        {
            PickAt(surface, e.GetPosition(surface));
            e.Handled = true;
        }
    }

    private void OnSurfacePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Control surface || surface != DraggedSurface)
            return;

        PickAt(surface, e.GetPosition(surface));
        e.Pointer.Capture(null);
        EndDrag();
        e.Handled = true;
    }

    private void OnSurfacePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is Control surface && surface == DraggedSurface)
            EndDrag();
    }

    private void OnSurfaceSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateVisuals();
    }

    private void EndDrag()
    {
        DraggedSurface = null;
        Commit();
    }

    private void PickAt(Control surface, Point position)
    {
        var size = surface.Bounds.Size;
        var x = size.Width > 0 ? (float)MathUtil.Clamp(position.X / size.Width, 0.0, 1.0) : 0f;
        var y = size.Height > 0 ? (float)MathUtil.Clamp(position.Y / size.Height, 0.0, 1.0) : 0f;
        if (surface == HueSurface)
        {
            SetCurrentValue(HueProperty, x * 360f);
        }
        else
        {
            // Both change the color: apply them as one
            isSyncingComponents = true;
            try
            {
                SetCurrentValue(SaturationProperty, x * 100f);
                SetCurrentValue(BrightnessProperty, y * 100f);
            }
            finally
            {
                isSyncingComponents = false;
            }
            ApplyHsbComponents();
        }
    }

    private void UpdateVisuals()
    {
        if (ColorSurface is not null)
        {
            var hue = new ColorHSV(Hue, 1f, 1f, 1f).ToColor().ToAvaloniaColor();
            ColorSurface.Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(hue, 1) },
            };
            if (ColorSelector is not null)
            {
                Canvas.SetLeft(ColorSelector, Saturation / 100.0 * ColorSurface.Bounds.Width);
                Canvas.SetTop(ColorSelector, Brightness / 100.0 * ColorSurface.Bounds.Height);
            }
        }
        if (HueSurface is not null && HueSelector is not null)
        {
            Canvas.SetLeft(HueSelector, Hue / 360.0 * HueSurface.Bounds.Width - HueSelector.Bounds.Width / 2);
        }
        if (Preview is not null)
        {
            Preview.Fill = new SolidColorBrush(ShownColor.ToAvaloniaColor());
        }
    }

    private static byte ToByte(float component) => (byte)MathF.Round(MathUtil.Clamp(component, 0f, 1f) * 255f);

    private static float CoerceHue(AvaloniaObject sender, float value) => (value % 360f + 360f) % 360f;

    private static float CoercePercentage(AvaloniaObject sender, float value) => MathUtil.Clamp(value, 0f, 100f);
}
