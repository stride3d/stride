// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

public sealed class NumericTextBoxTests
{
    private static (Window Window, NumericTextBox Box) Show(NumericTextBox box)
    {
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, box);
    }

    [AvaloniaFact]
    public void TemplateHasTheTextBoxPart()
    {
        var (_, box) = Show(new NumericTextBox());
        var textBox = box.GetVisualDescendants().OfType<TextBox>().SingleOrDefault();
        Assert.NotNull(textBox);
        Assert.Equal("PART_TextBox", textBox.Name);
    }

    [AvaloniaFact]
    public void ValueIsShownAsText()
    {
        var (_, box) = Show(new NumericTextBox { Value = 1.5 });
        Assert.Equal("1.5", box.Text);

        box.Value = 3.25;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("3.25", box.Text);
    }

    [AvaloniaFact]
    public void DecimalPlacesRoundTheText()
    {
        var (_, box) = Show(new NumericTextBox { DecimalPlaces = 2, Value = 1.23456 });
        Assert.Equal("1.23", box.Text);
    }

    [AvaloniaFact]
    public void TextUpdatesTheValueWithinBounds()
    {
        var (_, box) = Show(new NumericTextBox { Minimum = 0, Maximum = 10 });

        box.Text = "4";
        Assert.Equal(4.0, box.Value);

        box.Text = "42";
        Assert.Equal(10.0, box.Value);
    }

    [AvaloniaFact]
    public void InvalidTextKeepsTheValue()
    {
        var (_, box) = Show(new NumericTextBox { Value = 2 });
        box.Text = "not a number";
        Assert.Equal(2.0, box.Value);
    }

    [AvaloniaFact]
    public void BoundsCoerceTheValue()
    {
        var (_, box) = Show(new NumericTextBox { Value = 5 });

        box.Maximum = 3;
        Assert.Equal(3.0, box.Value);
        Assert.Equal("3", box.Text);

        box.Minimum = 4;
        Assert.Equal(4.0, box.Maximum);
        Assert.Equal(4.0, box.Value);
    }

    [AvaloniaFact]
    public void ValueChangedIsRaised()
    {
        var (_, box) = Show(new NumericTextBox { Value = 1 });
        NumericTextBoxValueChangedEventArgs? args = null;
        box.ValueChanged += (_, e) => args = e;

        box.Value = 2;

        Assert.NotNull(args);
        Assert.Equal(1.0, args.OldValue);
        Assert.Equal(2.0, args.NewValue);
    }

    [AvaloniaFact]
    public void TypingInTheTextBoxCommitsOnEnter()
    {
        var (window, box) = Show(new NumericTextBox { Value = 1 });
        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("7");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(7.0, box.Value);
    }

    [AvaloniaFact]
    public void ChangingTheDecimalPlacesRoundsTheShownValue()
    {
        // A float shown as a double: what the property grid gets for 0.65f
        var (_, box) = Show(new NumericTextBox { Value = (double)0.65f });
        Assert.Equal("0.6499999761581421", box.Text);

        box.DecimalPlaces = 6;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("0.65", box.Text);
        // Only the text is rounded
        Assert.Equal((double)0.65f, box.Value);
    }

    [AvaloniaTheory]
    [InlineData(PhysicalKey.ArrowUp, RawInputModifiers.None, 1.5)]
    [InlineData(PhysicalKey.ArrowDown, RawInputModifiers.None, 0.5)]
    [InlineData(PhysicalKey.ArrowUp, RawInputModifiers.Shift, 11)]
    [InlineData(PhysicalKey.ArrowDown, RawInputModifiers.Shift, -9)]
    [InlineData(PhysicalKey.PageUp, RawInputModifiers.None, 11)]
    [InlineData(PhysicalKey.PageDown, RawInputModifiers.None, -9)]
    public void TheArrowsAndPagesChangeTheValueByASmallOrLargeStep(PhysicalKey key, RawInputModifiers modifiers, double expected)
    {
        var (window, box) = Show(new NumericTextBox { Value = 1, SmallChange = 0.5, LargeChange = 10 });
        box.GetVisualDescendants().OfType<TextBox>().Single().Focus();

        window.KeyPressQwerty(key, modifiers);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(expected, box.Value);
        Assert.Equal(expected.ToString(System.Globalization.CultureInfo.InvariantCulture), box.Text);
    }

    [AvaloniaFact]
    public void TheStepsStayWithinTheBounds()
    {
        var (window, box) = Show(new NumericTextBox { Minimum = 0, Maximum = 2, Value = 1.5, SmallChange = 1 });
        box.GetVisualDescendants().OfType<TextBox>().Single().Focus();

        window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Assert.Equal(2.0, box.Value);

        window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Assert.Equal(0.0, box.Value);
    }
}
