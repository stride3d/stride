// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Assets.Presentation.View.Behaviors;
using Stride.Core.Presentation.Controls;
using Xunit;

namespace Stride.GameStudio.Tests;

public class TestMorphSliderBehavior
{
    [Fact]
    public async Task DragCommitsOnlyOnSuccessfulCompletion()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                // NumericTextBox styles own dispatcher-bound state: run every case on the same UI thread.
                foreach (var (canceled, initialValue) in new[] { (false, 0.25), (true, 0.25), (false, 2.5), (true, 2.5) })
                {
                    var textBox = new NumericTextBox { Value = initialValue };
                    var slider = new Slider { Minimum = -1, Maximum = 1 };
                    slider.SetBinding(Slider.ValueProperty, new Binding(nameof(NumericTextBox.Value))
                    {
                        Source = textBox,
                        Mode = BindingMode.OneWay,
                    });
                    double preview = initialValue;
                    var behavior = new LiveSliderEditBehavior { TextBox = textBox, Preview = value => preview = value };
                    behavior.Attach(slider);
                    try
                    {
                        slider.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
                        slider.SetCurrentValue(Slider.ValueProperty, 0.75);
                        Assert.Equal(0.75, preview);
                        Assert.Equal(initialValue, textBox.Value);

                        slider.RaiseEvent(new DragCompletedEventArgs(0, 0, canceled) { RoutedEvent = Thumb.DragCompletedEvent });
                        Assert.Equal(canceled ? initialValue : 0.75, textBox.Value);
                        Assert.Equal(canceled ? initialValue : 0.75, preview);
                        Assert.True(BindingOperations.IsDataBound(slider, Slider.ValueProperty));
                        slider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                            { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent });
                        Assert.Equal(canceled ? initialValue : 0.75, textBox.Value);

                        // The next track click must still work after cancellation.
                        slider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                            { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
                        slider.SetCurrentValue(Slider.ValueProperty, -0.5);
                        slider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                            { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent });
                        Assert.Equal(-0.5, textBox.Value);

                        // A property-grid refresh updates the slider without becoming an authored edit or preview.
                        textBox.Value = 2.5;
                        slider.GetBindingExpression(Slider.ValueProperty)!.UpdateTarget();
                        Assert.Equal(1, slider.Value);
                        Assert.Equal(2.5, textBox.Value);
                        Assert.Equal(canceled ? initialValue : 0.75, preview);
                    }
                    finally
                    {
                        behavior.Detach();
                    }
                }
                completed.SetResult();
            }
            catch (Exception exception)
            {
                completed.SetException(exception);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }
}
