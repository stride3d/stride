// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public abstract class ColorEditor<T> : TemplatedControl
{
    public static readonly StyledProperty<T> ValueProperty =
        AvaloniaProperty.Register<ColorEditor<T>, T>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<ColorEditor<T>, string?>(nameof(Watermark));

    public static readonly StyledProperty<IReadOnlyDictionary<string, Color4>> PaletteProperty =
        AvaloniaProperty.Register<ColorEditor<T>, IReadOnlyDictionary<string, Color4>>(nameof(Palette));

    public static readonly StyledProperty<KeyValuePair<string, Color4>?> SelectedPaletteColorProperty =
        AvaloniaProperty.Register<ColorEditor<T>, KeyValuePair<string, Color4>?>(nameof(Palette));

    public T Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public IReadOnlyDictionary<string, Color4> Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public KeyValuePair<string, Color4>? SelectedPaletteColor
    {
        get => GetValue(SelectedPaletteColorProperty);
        set => SetValue(SelectedPaletteColorProperty, value);
    }
}

public sealed class ColorEditor : ColorEditor<Color>;
public sealed class Color3Editor : ColorEditor<Color3>;
public sealed class Color4Editor : ColorEditor<Color4>;
