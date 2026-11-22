// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;

using Point = Avalonia.Point;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

/// <summary>
/// The color picker shows a color as RGBA and HSB components, and writes a color only when the user picks one: one write
/// for a whole drag on the square or on the hue strip, one for a typed component.
/// </summary>
public sealed class ColorPickerTests
{
    private const float Tolerance = 1e-4f;

    [AvaloniaFact]
    public void ComponentsShowTheColor()
    {
        var (window, picker, _) = Show(new Color4(1f, 0.5f, 0.25f, 0.75f));

        Assert.Equal((byte)255, picker.Red);
        Assert.Equal((byte)128, picker.Green);
        Assert.Equal((byte)64, picker.Blue);
        Assert.Equal((byte)191, picker.Alpha);
        Assert.Equal(20f, picker.Hue, Tolerance);
        Assert.Equal(75f, picker.Saturation, Tolerance);
        Assert.Equal(100f, picker.Brightness, Tolerance);
        Assert.Equal("128", TextBox(picker, "GreenTextBox").Text);
        Assert.Equal("20", TextBox(picker, "HueTextBox").Text);
        window.Close();
    }

    [AvaloniaFact]
    public void ShowingAColorWritesNothingBack()
    {
        var value = new Color4(1f, 0.5f, 0.25f, 0.75f);
        var (window, picker, source) = Show(value);
        // Visiting the boxes without typing writes nothing either
        TextBox(picker, "RedTextBox").Focus();
        TextBox(picker, "HueTextBox").Focus();
        TextBox(picker, "AlphaTextBox").Focus();
        Dispatcher.UIThread.RunJobs();

        source.SetFromCode(new Color4(0.1f, 0.2f, 0.3f, 0.4f));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void TypingAChannelWritesItAndKeepsTheOthers()
    {
        var (window, picker, source) = Show(new Color4(1f, 0.5f, 0.25f, 0.75f));

        Type(window, TextBox(picker, "RedTextBox"), "51");

        // The channels that were not typed keep their precision: 0.5 is not 128 / 255
        Assert.Equal(new Color4(0.2f, 0.5f, 0.25f, 0.75f), source.Value);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void TypingTheHueKeepsSaturationAndBrightness()
    {
        var (window, picker, source) = Show(new ColorHSV(0f, 0.5f, 0.8f, 1f).ToColor());

        Type(window, TextBox(picker, "HueTextBox"), "120");

        AssertColor(new ColorHSV(120f, 0.5f, 0.8f, 1f).ToColor(), (Color4)source.Value);
        Assert.Equal(50f, picker.Saturation, Tolerance);
        Assert.Equal(80f, picker.Brightness, Tolerance);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingOnTheSquareWritesOnceWhenReleased()
    {
        var (window, picker, source) = Show(new ColorHSV(240f, 1f, 1f, 1f).ToColor());
        var surface = Part(picker, "PART_ColorSurface");

        window.MouseDown(At(surface, window, 0.1, 0.1), MouseButton.Left);
        window.MouseMove(At(surface, window, 0.5, 0.5));
        window.MouseMove(At(surface, window, 0.25, 0.75));
        Dispatcher.UIThread.RunJobs();
        // The drag shows the color without writing it
        Assert.Equal(0, source.Writes);
        Assert.Equal(25f, picker.Saturation, 0.5f);
        Assert.Equal(75f, picker.Brightness, 0.5f);

        window.MouseUp(At(surface, window, 0.25, 0.75), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        AssertColor(new ColorHSV(240f, picker.Saturation / 100f, picker.Brightness / 100f, 1f).ToColor(), (Color4)source.Value);
        Assert.Equal(240f, picker.Hue, Tolerance);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingOnTheHueStripChangesOnlyTheHue()
    {
        var (window, picker, source) = Show(new ColorHSV(0f, 0.5f, 0.8f, 0.6f).ToColor());
        var surface = Part(picker, "PART_HueSurface");

        window.MouseDown(At(surface, window, 0.1, 0.5), MouseButton.Left);
        window.MouseMove(At(surface, window, 0.5, 0.5));
        window.MouseUp(At(surface, window, 0.5, 0.5), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        Assert.Equal(180f, picker.Hue, 2f);
        AssertColor(new ColorHSV(picker.Hue, 0.5f, 0.8f, 0.6f).ToColor(), (Color4)source.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingAComponentBoxShowsEachStepAndWritesOnceWhenReleased()
    {
        var (window, picker, source) = Show(new Color4(1f, 1f, 1f, 1f));
        var red = Box(picker, "RedTextBox");
        var start = TextCenter(window, red);

        window.MouseDown(start, MouseButton.Left);
        // The first move past the drag distance starts the drag, then a step every 3 pixels
        window.MouseMove(start - new Point(5, 0));
        window.MouseMove(start - new Point(35, 0));
        Dispatcher.UIThread.RunJobs();

        // The picker shows the step, the other boxes follow, and nothing is written
        Assert.Equal(245.0, red.Value);
        Assert.Equal(0, source.Writes);
        AssertColor(new Color4(245f / 255f, 1f, 1f, 1f), picker.ShownColor);
        Assert.Equal(180f, picker.Hue, Tolerance);
        Assert.Equal((1f - 245f / 255f) * 100f, (float)Box(picker, "SaturationTextBox").Value!.Value, Tolerance);
        Assert.Equal(255.0, Box(picker, "GreenTextBox").Value);
        AssertColor(new Color4(1f, 1f, 1f, 1f), picker.Color);

        window.MouseUp(start - new Point(35, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        AssertColor(new Color4(245f / 255f, 1f, 1f, 1f), (Color4)source.Value);
        Assert.Equal((byte)245, picker.Red);
        window.Close();
    }

    [AvaloniaFact]
    public async Task AHeldSpinButtonOfAComponentShowsEachRepeatAndWritesOnceWhenReleased()
    {
        var (window, picker, source) = Show(new ColorHSV(200f, 0.5f, 0.5f, 1f).ToColor());
        var saturation = Box(picker, "SaturationTextBox");
        var button = saturation.GetVisualDescendants().OfType<RepeatButton>().Single(x => x.Name == "PART_IncreaseButton");
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

        window.MouseDown(center, MouseButton.Left);
        for (var i = 0; i < 100 && saturation.Value < 53; ++i)
        {
            await Task.Delay(20);
            Dispatcher.UIThread.RunJobs();
        }

        var held = (float)saturation.Value!.Value;
        Assert.True(held >= 53f);
        Assert.Equal(0, source.Writes);
        var shown = new ColorHSV(200f, held / 100f, 0.5f, 1f).ToColor();
        AssertColor(shown, picker.ShownColor);
        // The RGBA boxes follow
        Assert.Equal((double)(byte)MathF.Round(shown.R * 255f), Box(picker, "RedTextBox").Value);

        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        var released = (float)saturation.Value!.Value;
        Assert.Equal(1, source.Writes);
        AssertColor(new ColorHSV(200f, released / 100f, 0.5f, 1f).ToColor(), (Color4)source.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void ACancelledDragOnAComponentBoxShowsTheColorAgain()
    {
        var (window, picker, source) = Show(new Color4(1f, 1f, 1f, 1f));
        var red = Box(picker, "RedTextBox");
        var start = TextCenter(window, red);
        var hue = picker.Hue;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start - new Point(5, 0));
        window.MouseMove(start - new Point(35, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.NotEqual(1f, picker.ShownColor.R);

        // The box loses the mouse, as when another window opens
        red.RaiseEvent(new PointerCaptureLostEventArgs(red, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true)));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(255.0, red.Value);
        AssertColor(new Color4(1f, 1f, 1f, 1f), picker.ShownColor);
        // White has no hue: the one shown before comes back
        Assert.Equal(hue, picker.Hue, Tolerance);
        Assert.Equal(0f, picker.Saturation, Tolerance);
        Assert.Equal(0, source.Writes);
        window.MouseUp(start - new Point(35, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void PickingAGrayKeepsTheHue()
    {
        var (window, picker, source) = Show(new ColorHSV(200f, 0.5f, 0.5f, 1f).ToColor());
        var surface = Part(picker, "PART_ColorSurface");

        // The left edge is a gray, the top edge black
        window.MouseDown(At(surface, window, 0, 0), MouseButton.Left);
        window.MouseUp(At(surface, window, 0, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Color4(0f, 0f, 0f, 1f), source.Value);

        source.SetFromCode(new Color4(0.5f, 0.5f, 0.5f, 1f));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(200f, picker.Hue, Tolerance);
        window.Close();
    }

    public static TheoryData<Type, object, bool> EditorsAndValues => new()
    {
        // Not exact on a byte per channel: 0.5 shows as 128 (0.502), 0.25 as 64 (0.251)
        { typeof(Color3Editor), new Color3(1f, 0.5f, 0.25f), false },
        { typeof(Color4Editor), new Color4(1f, 0.5f, 0.25f, 0.75f), true },
        { typeof(ColorEditor), new Color(255, 128, 64, 192), true },
    };

    [AvaloniaTheory]
    [MemberData(nameof(EditorsAndValues))]
    public void OpeningTheEditorPickerWritesNothingBack(Type editorType, object value, bool showsAlpha)
    {
        var (window, picker, source) = ShowEditor(editorType, value);

        Assert.Equal(showsAlpha, picker.ShowAlpha);
        Assert.Equal(showsAlpha, picker.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == "AlphaTextBox").IsEffectivelyVisible);
        Assert.Equal(255, picker.Red);
        Assert.Equal(value, source.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(EditorsAndValues))]
    public void TypingInTheEditorPickerWritesTheEditorValue(Type editorType, object value, bool showsAlpha)
    {
        var (window, picker, source) = ShowEditor(editorType, value);
        var topLevel = TopLevel.GetTopLevel(picker)!;

        Type(topLevel, TextBox(picker, "BlueTextBox"), "255");

        Assert.Equal(1, source.Writes);
        Assert.Equal(value.GetType(), source.Value.GetType());
        var written = source.Value switch
        {
            Color3 c => c.ToColor4(),
            Color c => c.ToColor4(),
            var c => (Color4)c,
        };
        Assert.Equal(1f, written.B, Tolerance);
        Assert.Equal(1f, written.R, Tolerance);
        window.Close();
    }

    private static (Window Window, ColorPicker Picker, ValueSource Source) Show(Color4 value)
    {
        var source = new ValueSource(value);
        var picker = new ColorPicker { DataContext = source };
        picker.Bind(ColorPicker.ColorProperty, new Binding(nameof(ValueSource.Value)) { Mode = BindingMode.TwoWay });
        var window = new Window { Width = 400, Height = 300, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, picker, source);
    }

    private static (Window Window, ColorPicker Picker, ValueSource Source) ShowEditor(Type editorType, object value)
    {
        var source = new ValueSource(value);
        var editor = (TemplatedControl)Activator.CreateInstance(editorType)!;
        editor.DataContext = source;
        var valueProperty = editorType == typeof(ColorEditor)
            ? ColorEditor.ValueProperty
            : editorType == typeof(Color3Editor) ? Color3Editor.ValueProperty : (AvaloniaProperty)Color4Editor.ValueProperty;
        // The binding a property grid uses: two-way, written when the editor's value changes
        editor.Bind(valueProperty, new Binding(nameof(ValueSource.Value)) { Mode = BindingMode.TwoWay });
        var window = new Window { Width = 400, Height = 400, Content = new StackPanel { Children = { editor } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var toggle = editor.GetVisualDescendants().OfType<ToggleButton>().Single(x => x.Name == "PickerToggleButton");
        toggle.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        var popup = editor.GetVisualDescendants().OfType<Popup>().Single();
        var picker = popup.Child!.GetVisualDescendants().OfType<ColorPicker>().Single();
        return (window, picker, source);
    }

    private static void Type(TopLevel topLevel, TextBox textBox, string text)
    {
        textBox.Focus();
        textBox.SelectAll();
        topLevel.KeyTextInput(text);
        topLevel.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBox TextBox(ColorPicker picker, string name)
        => picker.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == name)
            .GetVisualDescendants().OfType<TextBox>().Single();

    private static NumericTextBox Box(ColorPicker picker, string name)
        => picker.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == name);

    private static Point TextCenter(Visual window, NumericTextBox box)
    {
        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();
        // Left of the spin buttons
        return textBox.TranslatePoint(new Point(textBox.Bounds.Width / 3, textBox.Bounds.Height / 2), window)!.Value;
    }

    private static Control Part(ColorPicker picker, string name)
        => picker.GetVisualDescendants().OfType<Control>().Single(x => x.Name == name);

    private static Point At(Control surface, Visual window, double x, double y)
        => surface.TranslatePoint(new Point(x * surface.Bounds.Width, y * surface.Bounds.Height), window)!.Value;

    private static void AssertColor(Color4 expected, Color4 actual)
    {
        Assert.Equal(expected.R, actual.R, Tolerance);
        Assert.Equal(expected.G, actual.G, Tolerance);
        Assert.Equal(expected.B, actual.B, Tolerance);
        Assert.Equal(expected.A, actual.A, Tolerance);
    }

    private sealed class ValueSource(object value) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The number of times the value was written with a different value.
        /// </summary>
        public int Writes { get; private set; }

        public object Value
        {
            get => Current;
            set
            {
                // Like a node of the property grid, which ignores a value equal to its own
                if (!Equals(Current, value))
                    Writes++;
                Current = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        private object Current { get; set; } = value;

        /// <summary>
        /// Changes the value as the model would, without counting it as a write.
        /// </summary>
        public void SetFromCode(object value)
        {
            Current = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}
