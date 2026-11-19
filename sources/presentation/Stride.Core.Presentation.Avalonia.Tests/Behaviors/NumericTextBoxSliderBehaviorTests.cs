// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Behaviors;
using Stride.Core.Presentation.Avalonia.Controls;
using Point = Avalonia.Point;

namespace Stride.Core.Presentation.Avalonia.Tests.Behaviors;

/// <summary>
/// A slider that changes the value of a <see cref="NumericTextBox"/>: the box writes the value once per slide or key press.
/// </summary>
public sealed class NumericTextBoxSliderBehaviorTests
{
    [AvaloniaFact]
    public void ASlideWritesTheValueOnceOnRelease()
    {
        var (window, slider, box, source) = Show(2);
        var left = Point(window, slider, 0.5);

        window.MouseDown(left, MouseButton.Left);
        window.MouseMove(Point(window, slider, 0.6));
        window.MouseMove(Point(window, slider, 0.8));
        Dispatcher.UIThread.RunJobs();

        // The box follows the slider, and nothing is written yet
        Assert.NotEqual(2.0, box.Value);
        Assert.Equal(slider.Value, box.Value);
        Assert.Equal(0, source.Writes);

        window.MouseUp(Point(window, slider, 0.8), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        Assert.Equal(box.Value, source.Value);
        Assert.InRange(source.Value!.Value, 7, 9);
        window.Close();
    }

    [AvaloniaFact]
    public void EachKeyPressWritesOnce()
    {
        var (window, slider, _, source) = Show(2);
        slider.Focus();

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3.0, source.Value);
        Assert.Equal(1, source.Writes);

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(4.0, source.Value);
        Assert.Equal(2, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void AValueFromTheSourceMovesTheSliderWithoutWriting()
    {
        var (window, slider, box, source) = Show(2);

        source.Value = 6;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(6.0, box.Value);
        Assert.Equal(6.0, slider.Value);
        // Only the write of the test
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void ALostCaptureCancelsTheSlide()
    {
        var (window, slider, box, source) = Show(2);

        window.MouseDown(Point(window, slider, 0.5), MouseButton.Left);
        window.MouseMove(Point(window, slider, 0.7));
        Dispatcher.UIThread.RunJobs();
        Assert.NotEqual(2.0, box.Value);

        // The capture is lost before the release (another window took the mouse)
        slider.RaiseEvent(new PointerCaptureLostEventArgs(slider, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true)));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2.0, box.Value);
        Assert.Equal(2.0, slider.Value);
        Assert.Equal(0, source.Writes);
        window.MouseUp(Point(window, slider, 0.7), MouseButton.Left);
        window.Close();
    }

    private static (Window Window, Slider Slider, NumericTextBox Box, Source Source) Show(double value)
    {
        var source = new Source(value);
        var box = new NumericTextBox { Minimum = 0, Maximum = 10, SmallChange = 1, LargeChange = 1, DisplayUpDownButtons = false };
        // The binding of the property grid: written only when the box commits
        box.Bind(NumericTextBox.ValueProperty, new Binding(nameof(Source.Value)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        var slider = new Slider { Minimum = 0, Maximum = 10, SmallChange = 1, LargeChange = 1, IsSnapToTickEnabled = true, TickFrequency = 1, Width = 300 };
        slider.Bind(Slider.ValueProperty, new Binding(nameof(NumericTextBox.Value)) { Source = box, Mode = BindingMode.OneWay });
        Interaction.GetBehaviors(slider).Add(new NumericTextBoxSliderBehavior { NumericTextBox = box });
        var window = new Window { Content = new StackPanel { Children = { slider, box } }, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(value, slider.Value);
        return (window, slider, box, source);
    }

    /// <summary>
    /// A point on the slider, at the given fraction of its width.
    /// </summary>
    private static Point Point(Window window, Slider slider, double fraction)
    {
        return slider.TranslatePoint(new Point(slider.Bounds.Width * fraction, slider.Bounds.Height / 2), window)!.Value;
    }

    private sealed class Source(double value) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The number of times the value was set.
        /// </summary>
        public int Writes { get; private set; }

        public double? Value
        {
            get;
            set
            {
                Writes++;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        } = value;
    }
}
