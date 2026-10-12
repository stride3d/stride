using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace MorphTargets;

/// <summary>Shared look for the sample's overlay panels: dark panels, headings, buttons, sliders and name/value tables.</summary>
sealed class SampleUI
{
    public static readonly Color Accent = new(70, 140, 220);
    private static readonly Color PanelColor = new(16, 18, 22, 210);
    public static readonly Color Muted = new(170, 175, 185);
    private const int SliderHeight = 20;

    private readonly SpriteFont font;
    private readonly ISpriteProvider button;
    private readonly ISpriteProvider buttonHover;
    private readonly ISpriteProvider buttonPressed;
    private readonly ISpriteProvider current;
    private readonly ISpriteProvider track;
    private readonly ISpriteProvider fill;
    private readonly ISpriteProvider thumb;
    private readonly ISpriteProvider thumbHover;

    public SampleUI(ScriptComponent script, SpriteFont font)
    {
        this.font = font;
        var device = script.GraphicsDevice;
        button = Solid(device, new Color(52, 56, 64), 4, 4);
        buttonHover = Solid(device, new Color(72, 78, 90), 4, 4);
        buttonPressed = Solid(device, Accent, 4, 4);
        current = Solid(device, new Color(45, 95, 155), 4, 4);
        // Stride scales the thumb by slider height / track image height, so the track image is as tall as the
        // slider, with a thin opaque band in the middle.
        track = Band(device, new Color(60, 64, 72), 6);
        fill = Band(device, Accent, 6);
        thumb = Solid(device, new Color(225, 228, 235), 8, SliderHeight);
        thumbHover = Solid(device, Color.White, 8, SliderHeight);
    }

    /// <summary>A separate timings panel for the bottom-right corner; returns the value cells for <see cref="FrameTimings.Report"/>.</summary>
    public Border TimingsPanel(out TextBlock[] values)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Width = 280 };
        panel.Children.Add(Section("Timings"));
        values = Table(panel, FrameTimings.Rows);
        return Panel(panel, HorizontalAlignment.Right, VerticalAlignment.Bottom);
    }

    public Border Panel(UIElement content, HorizontalAlignment horizontal, VerticalAlignment vertical = VerticalAlignment.Top) => new()
    {
        Content = content,
        HorizontalAlignment = horizontal,
        VerticalAlignment = vertical,
        Margin = new Thickness(16, 16, 16, 16),
        Padding = new Thickness(14, 10, 14, 14),
        BackgroundColor = PanelColor,
    };

    public TextBlock Heading(string text) => Text(text, 20, Color.White, new Thickness(0, 4, 0, 4));

    public TextBlock Section(string text) => Text(text.ToUpperInvariant(), 13, Muted, new Thickness(0, 12, 0, 2));

    public TextBlock Label(string text = "", float size = 16) => Text(text, size, Color.White, new Thickness(0, 6, 0, 2));

    public TextBlock Text(string text, float size, Color color, Thickness margin) => new()
    {
        Text = text,
        Font = font,
        TextSize = size,
        TextColor = color,
        Margin = margin,
        VerticalAlignment = VerticalAlignment.Center,
    };

    public Button Button(TextBlock label, Action click, bool isCurrent = false)
    {
        var result = new Button
        {
            Content = label,
            NotPressedImage = isCurrent ? current : button,
            MouseOverImage = isCurrent ? current : buttonHover,
            PressedImage = buttonPressed,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 6, 6, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            CanBeHitByUser = !isCurrent,
        };
        label.Margin = Thickness.UniformCuboid(0);
        result.Click += (_, _) => click();
        return result;
    }

    /// <summary>
    /// A labelled row of mutually exclusive options; the selected one is highlighted and not clickable.
    /// Returns a setter that changes the selection without raising <paramref name="changed"/>.
    /// </summary>
    public Action<int> RadioGroup(StackPanel panel, string title, IReadOnlyList<string> options, int selected, Action<int> changed, int columns = int.MaxValue)
    {
        panel.Children.Add(Section(title));
        columns = Math.Min(columns, options.Count);
        var row = new UniformGrid { Columns = columns, Rows = (options.Count + columns - 1) / columns, HorizontalAlignment = HorizontalAlignment.Left };
        var buttons = new Button[options.Count];
        void Select(int index)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].NotPressedImage = i == index ? current : button;
                buttons[i].MouseOverImage = i == index ? current : buttonHover;
                buttons[i].CanBeHitByUser = i != index;
            }
        }
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var label = Label(options[i], 15);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            buttons[i] = Button(label, () => { Select(index); changed(index); });
            buttons[i].HorizontalAlignment = HorizontalAlignment.Stretch;
            buttons[i].Margin = new Thickness(0, 2, 4, 2);
            buttons[i].DependencyProperties.Set(GridBase.ColumnPropertyKey, i % columns);
            buttons[i].DependencyProperties.Set(GridBase.RowPropertyKey, i / columns);
            row.Children.Add(buttons[i]);
        }
        Select(selected);
        panel.Children.Add(row);
        return Select;
    }

    /// <summary>A slider; <paramref name="bipolar"/> leaves out the fill, which would start at the minimum rather than at zero.</summary>
    public Slider Slider(float minimum, float maximum, float value, float width, bool bipolar = false) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Value = value,
        Width = width,
        Height = SliderHeight,
        TrackBackgroundImage = track,
        TrackForegroundImage = bipolar ? null : fill,
        ThumbImage = thumb,
        MouseOverThumbImage = thumbHover,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 2, 0, 4),
    };

    public Slider IntegerSlider(int minimum, int maximum, int value, Action<int> changed)
    {
        var slider = Slider(minimum, Math.Max(minimum + 1, maximum), value, 320);
        slider.TickFrequency = Math.Max(1, maximum - minimum);
        slider.ShouldSnapToTicks = true;
        slider.Step = 1;
        slider.ValueChanged += (_, _) => changed((int)MathF.Round(slider.Value));
        return slider;
    }

    /// <summary>Two-column table with left-aligned names and right-aligned values; returns the value blocks.</summary>
    public TextBlock[] Table(StackPanel panel, IReadOnlyList<string> names, float valueWidth = 90)
    {
        var grid = new UniformGrid { Columns = 1, Rows = names.Count };
        var values = new TextBlock[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new StripDefinition(StripType.Star));
            row.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, valueWidth));
            var name = Text(names[i], 15, names[i].StartsWith(' ') ? Muted : Color.White, Thickness.UniformCuboid(0));
            var value = values[i] = Text("-", 15, Color.White, Thickness.UniformCuboid(0));
            value.TextAlignment = TextAlignment.Right;
            value.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            row.Children.Add(name);
            row.Children.Add(value);
            row.DependencyProperties.Set(GridBase.RowPropertyKey, i);
            grid.Children.Add(row);
        }
        panel.Children.Add(grid);
        return values;
    }

    /// <summary>Shows the panels on one page; each places itself with its own alignment. F1 toggles them.</summary>
    public static void Show(ScriptComponent script, params UIElement[] panels)
    {
        var root = new Grid();
        foreach (var panel in panels) root.Children.Add(panel);
        // A larger virtual resolution than the 1280x720 default draws the panels smaller; fonts rasterize at the real size.
        var component = new UIComponent { Page = new UIPage { RootElement = root }, Resolution = new Vector3(1920, 1080, 1000) };
        script.Entity.Add(component);
        script.Script.AddTask(async () =>
        {
            while (script.Entity.Scene != null)
            {
                if (script.Input.IsKeyPressed(Keys.F1))
                    component.Enabled = !component.Enabled;
                await script.Script.NextFrame();
            }
        });
    }

    private static ISpriteProvider Band(GraphicsDevice device, Color color, int band)
    {
        const int width = 16;
        var pixels = new Color[width * SliderHeight];
        int top = (SliderHeight - band) / 2;
        for (int y = top; y < top + band; y++)
            Array.Fill(pixels, color, y * width, width);
        return Sprite(device, pixels, width, SliderHeight);
    }

    private static ISpriteProvider Solid(GraphicsDevice device, Color color, int width, int height)
        => Sprite(device, Enumerable.Repeat(color, width * height).ToArray(), width, height);

    private static ISpriteProvider Sprite(GraphicsDevice device, Color[] pixels, int width, int height)
        => new SpriteFromTexture { Texture = Texture.New2D(device, width, height, PixelFormat.R8G8B8A8_UNorm, pixels) };
}
