// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
/// Changing the value of a <see cref="NumericTextBox"/> with the mouse: dragging over its text, and its spin buttons. One
/// gesture writes the value once, so that it is one change to undo.
/// </summary>
public sealed class NumericTextBoxMouseTests
{
    [AvaloniaFact]
    public void DraggingRightAddsASmallChangeEveryThreePixels()
    {
        var (window, box, source) = ShowBound(1, x => x.SmallChange = 0.5);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        // The first move past the drag distance starts the drag, and sets its direction
        window.MouseMove(start + new Point(5, 0));
        Assert.Equal(1.0, box.Value);
        window.MouseMove(start + new Point(14, 0));
        Assert.Equal(2.5, box.Value);
        // Two pixels make no step yet; one more makes the step
        window.MouseMove(start + new Point(16, 0));
        Assert.Equal(2.5, box.Value);
        window.MouseMove(start + new Point(17, 0));
        Assert.Equal(3.0, box.Value);
        Assert.Equal("3", box.Text);
        window.MouseUp(start + new Point(17, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(3.0, source.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingLeftRemovesTheSameSteps()
    {
        var (window, box, _) = ShowBound(1);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(-5, 0));
        window.MouseMove(start + new Point(-11, 0));
        window.MouseUp(start + new Point(-11, 0), MouseButton.Left);

        Assert.Equal(-1.0, box.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingUpIncreasesAndDownDecreases()
    {
        var (window, box, _) = ShowBound(0);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        // A vertical first move makes a vertical drag: horizontal moves then change nothing
        window.MouseMove(start + new Point(0, -5));
        window.MouseMove(start + new Point(0, -11));
        Assert.Equal(2.0, box.Value);
        window.MouseMove(start + new Point(30, -11));
        Assert.Equal(2.0, box.Value);
        window.MouseMove(start + new Point(30, 4));
        Assert.Equal(-3.0, box.Value);
        window.MouseUp(start + new Point(30, 4), MouseButton.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void ADragStaysWithinTheBounds()
    {
        var (window, box, source) = ShowBound(1, x =>
        {
            x.Minimum = 0;
            x.Maximum = 3;
        });
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(65, 0));
        Assert.Equal(3.0, box.Value);
        window.MouseMove(start + new Point(-65, 0));
        Assert.Equal(0.0, box.Value);
        window.MouseUp(start + new Point(-65, 0), MouseButton.Left);

        Assert.Equal(0.0, source.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void ADragWritesTheValueOnceOnRelease()
    {
        var (window, box, source) = ShowBound(1);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        for (var x = 8; x <= 35; x += 3)
            window.MouseMove(start + new Point(x, 0));
        Assert.Equal(11.0, box.Value);
        // Nothing is written while the mouse moves
        Assert.Equal(0, source.Writes);

        window.MouseUp(start + new Point(35, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        Assert.Equal(11.0, source.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void ADragBackToTheStartWritesNothing()
    {
        var (window, box, source) = ShowBound(1);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(11, 0));
        window.MouseMove(start + new Point(5, 0));
        window.MouseUp(start + new Point(5, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.0, box.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void ADragValidatedOnMouseMoveWritesEachStep()
    {
        var (window, box, source) = ShowBound(1, x => x.MouseValidationTrigger = MouseValidationTrigger.OnMouseMove);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(8, 0));
        window.MouseMove(start + new Point(11, 0));
        Assert.Equal(2, source.Writes);
        Assert.Equal(3.0, source.Value);
        window.MouseUp(start + new Point(11, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void AClickWithoutADragEditsTheText()
    {
        var (window, box, source) = ShowBound(1);
        var textBox = InnerTextBox(box);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(2, 1));
        window.MouseUp(start + new Point(2, 1), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(textBox.IsKeyboardFocusWithin);
        Assert.Equal(textBox.Text?.Length, textBox.SelectionEnd - textBox.SelectionStart);
        Assert.Equal(1.0, box.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void ABoxWithTheFocusSelectsTextInsteadOfDragging()
    {
        var (window, box, source) = ShowBound(1);
        InnerTextBox(box).Focus();
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(30, 0));
        window.MouseUp(start + new Point(30, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.0, box.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void NoDragWhenTheMouseDragIsNotAllowed()
    {
        var (window, box, _) = ShowBound(1, x => x.AllowMouseDrag = false);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(30, 0));
        window.MouseUp(start + new Point(30, 0), MouseButton.Left);

        Assert.Equal(1.0, box.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void LosingTheMouseCancelsTheDrag()
    {
        var (window, box, source) = ShowBound(1);
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        window.MouseMove(start + new Point(20, 0));
        Assert.Equal(6.0, box.Value);
        // The box loses the mouse, as when another window opens
        box.RaiseEvent(new PointerCaptureLostEventArgs(box, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true)));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.0, box.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void TheCursorShowsThatTheTextCanBeDragged()
    {
        var (window, box, _) = ShowBound(1);
        var presenter = InnerTextBox(box).GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(StandardCursorType.SizeAll.ToString(), presenter.Cursor?.ToString());
        var start = TextCenter(window, box);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        Assert.Equal(StandardCursorType.SizeWestEast.ToString(), presenter.Cursor?.ToString());
        window.MouseUp(start + new Point(5, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        // The box has the focus now: the text is edited with the mouse
        window.MouseDown(start, MouseButton.Left);
        window.MouseUp(start, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(StandardCursorType.Ibeam.ToString(), presenter.Cursor?.ToString());
        window.Close();
    }

    [AvaloniaFact]
    public void TheSpinButtonsAddAndRemoveASmallChange()
    {
        var (window, box, source) = ShowBound(1, x => x.SmallChange = 0.5);

        Click(window, SpinButton(box, "PART_IncreaseButton"));
        Assert.Equal(1.5, box.Value);
        Assert.Equal(1, source.Writes);

        Click(window, SpinButton(box, "PART_DecreaseButton"));
        Click(window, SpinButton(box, "PART_DecreaseButton"));
        Assert.Equal(0.5, box.Value);
        Assert.Equal(0.5, source.Value);
        Assert.Equal(3, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void TheSpinButtonsStayWithinTheBounds()
    {
        var (window, box, _) = ShowBound(1, x =>
        {
            x.Minimum = 0;
            x.Maximum = 1.5;
        });

        Click(window, SpinButton(box, "PART_IncreaseButton"));
        Assert.Equal(1.5, box.Value);
        Click(window, SpinButton(box, "PART_DecreaseButton"));
        Click(window, SpinButton(box, "PART_DecreaseButton"));
        Assert.Equal(0.0, box.Value);
        window.Close();
    }

    [AvaloniaFact]
    public async Task AHeldSpinButtonRepeatsAndWritesOnceOnRelease()
    {
        var (window, box, source) = ShowBound(1);
        var button = SpinButton(box, "PART_IncreaseButton");
        var events = new List<string>();
        box.RepeatButtonPressed += (_, e) => events.Add($"pressed {e.Button}");
        box.RepeatButtonReleased += (_, e) => events.Add($"released {e.Button}");
        var center = Center(window, button);

        window.MouseDown(center, MouseButton.Left);
        // The press adds the first step at once, then the button repeats after its delay
        Assert.Equal(2.0, box.Value);
        await PumpUntilAsync(() => box.Value >= 4);
        Assert.Equal(0, source.Writes);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        var released = box.Value;
        Assert.Equal(1, source.Writes);
        Assert.Equal(released, source.Value);
        Assert.Equal(["pressed IncreaseButton", "released IncreaseButton"], events);
        // and stops repeating
        await Task.Delay(300);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(released, box.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void TheSpinButtonsCanBeHidden()
    {
        var (window, box, _) = ShowBound(1);
        var buttons = SpinButton(box, "PART_IncreaseButton").GetVisualParent()!;
        Assert.True(buttons.IsEffectivelyVisible);

        box.DisplayUpDownButtons = false;
        Dispatcher.UIThread.RunJobs();

        Assert.False(buttons.IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void TheValueRatioFollowsTheValueAndTheBounds()
    {
        var (window, box, _) = ShowBound(5, x =>
        {
            x.Minimum = 0;
            x.Maximum = 10;
        });
        Assert.Equal(0.5, box.ValueRatio);

        box.Maximum = 20;
        Assert.Equal(0.25, box.ValueRatio);

        box.ValueRatio = 0.75;
        Assert.Equal(15.0, box.Value);
        Assert.Equal("15", box.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void TheBoxesOfTheMultiValueEditorsHaveNoSpinButtons()
    {
        var editors = new Control[]
        {
            new Vector2Editor(), new Vector3Editor(), new Vector4Editor(), new Int2Editor(), new Int3Editor(), new Int4Editor(), new RectangleEditor(),
            new RectangleFEditor(), new AngleEditor(), new QuaternionEditor(), new MatrixEditor(), new TimeSpanEditor(), new DateTimeEditor(),
        };
        var panel = new StackPanel();
        panel.Children.AddRange(editors);
        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        foreach (var editor in editors)
        {
            // The date and time editor edits its components in a drop-down
            var popup = editor.GetVisualDescendants().OfType<Popup>().SingleOrDefault();
            if (popup is not null)
            {
                popup.IsOpen = true;
                Dispatcher.UIThread.RunJobs();
            }
            var boxes = (popup?.Child ?? editor).GetVisualDescendants().OfType<NumericTextBox>().ToList();
            Assert.NotEmpty(boxes);
            Assert.All(boxes, x => Assert.False(x.DisplayUpDownButtons, editor.GetType().Name));
        }
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingAComponentOfAVectorWritesTheVectorOnce()
    {
        var source = new VectorSource(new Vector3(1, 2, 3));
        var editor = new Vector3Editor();
        editor.Bind(Vector3Editor.ValueProperty, new Binding(nameof(VectorSource.Value)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        var window = new Window { Content = editor, Width = 600, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var yBox = editor.GetVisualDescendants().OfType<NumericTextBox>().ElementAt(1);
        var start = TextCenter(window, yBox);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(5, 0));
        for (var x = 8; x <= 20; x += 3)
            window.MouseMove(start + new Point(x, 0));
        Assert.Equal(0, source.Writes);
        window.MouseUp(start + new Point(20, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        Assert.Equal(new Vector3(1, 7, 3), source.Value);
        window.Close();
    }

    private static (Window Window, NumericTextBox Box, Source Source) ShowBound(double value, Action<NumericTextBox>? setup = null)
    {
        var source = new Source(value);
        var box = new NumericTextBox();
        setup?.Invoke(box);
        // The binding of the property grid: written only when the box commits
        box.Bind(NumericTextBox.ValueProperty, new Binding(nameof(Source.Value)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        var window = new Window { Content = new StackPanel { Children = { box, new TextBox() } }, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, box, source);
    }

    private static TextBox InnerTextBox(NumericTextBox box) => box.GetVisualDescendants().OfType<TextBox>().Single();

    private static RepeatButton SpinButton(NumericTextBox box, string name) => box.GetVisualDescendants().OfType<RepeatButton>().Single(x => x.Name == name);

    private static Point TextCenter(Window window, NumericTextBox box)
    {
        var textBox = InnerTextBox(box);
        // Left of the spin buttons
        return textBox.TranslatePoint(new Point(textBox.Bounds.Width / 3, textBox.Bounds.Height / 2), window)!.Value;
    }

    private static Point Center(Window window, Visual visual) => visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;

    private static void Click(Window window, Visual visual)
    {
        var center = Center(window, visual);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static async Task PumpUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); ++i)
        {
            await Task.Delay(20);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.True(condition());
    }

    private sealed class Source(double value) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The number of times the binding wrote the value back.
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

    private sealed class VectorSource(Vector3 value) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Writes { get; private set; }

        public Vector3? Value
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
