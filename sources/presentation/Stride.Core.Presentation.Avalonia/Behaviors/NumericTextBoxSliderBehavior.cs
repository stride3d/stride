// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// Lets a <see cref="Slider"/> change the value of a <see cref="NumericTextBox"/>, which writes the value to its source once per
/// slide or key press.
/// </summary>
/// <remarks>
/// The slider shows the value of the box through a one-way binding. While the user slides it or presses a key on it, the
/// value of the box follows the slider; the box writes its value when the mouse button is released or the key press ends,
/// and only when the value changed. When the slide is canceled, the box gets back the value it had before it started.
/// The value of the box must be bound with <c>UpdateSourceTrigger=Explicit</c>.
/// </remarks>
public sealed class NumericTextBoxSliderBehavior : Behavior<Slider>
{
    public static readonly StyledProperty<NumericTextBox?> NumericTextBoxProperty =
        AvaloniaProperty.Register<NumericTextBoxSliderBehavior, NumericTextBox?>(nameof(NumericTextBox));

    /// <summary>
    /// The box whose value the slider changes.
    /// </summary>
    public NumericTextBox? NumericTextBox
    {
        get => GetValue(NumericTextBoxProperty);
        set => SetValue(NumericTextBoxProperty, value);
    }

    /// <summary>
    /// How the user is changing the value with the slider, and the value of the box before the change.
    /// </summary>
    private (SliderInput Input, double? StartValue) Interaction { get; set; }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not { } slider)
            return;

        slider.PropertyChanged += OnSliderPropertyChanged;
        slider.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        slider.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        slider.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        slider.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        slider.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } slider)
        {
            slider.PropertyChanged -= OnSliderPropertyChanged;
            slider.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            slider.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            slider.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
            slider.RemoveHandler(InputElement.KeyDownEvent, OnPreviewKeyDown);
            slider.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }
        Interaction = default;
        base.OnDetaching();
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Changes that come from the box through the binding are not the user's
        if (e.Property != RangeBase.ValueProperty || Interaction.Input == SliderInput.None || NumericTextBox is not { } box)
            return;

        box.SetCurrentValue(NumericTextBox.ValueProperty, e.GetNewValue<double>());
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject is not { } slider || !e.GetCurrentPoint(slider).Properties.IsLeftButtonPressed)
            return;

        if (Interaction.Input != SliderInput.Pointer)
            Interaction = (SliderInput.Pointer, NumericTextBox?.Value);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Interaction.Input == SliderInput.Pointer)
            End();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // A release ends the interaction first: a capture lost while it is still active cancels the slide
        if (Interaction.Input == SliderInput.Pointer && NumericTextBox is { } box)
            box.SetCurrentValue(NumericTextBox.ValueProperty, Interaction.StartValue);
        if (Interaction.Input == SliderInput.Pointer)
            Interaction = default;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // The slider changes its value while it handles the key, the bubbling handler below writes it; a key pressed
        // during a slide leaves the slide to end on release
        if (Interaction.Input == SliderInput.None)
            Interaction = (SliderInput.Key, NumericTextBox?.Value);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Interaction.Input == SliderInput.Key)
            End();
    }

    private void End()
    {
        var startValue = Interaction.StartValue;
        Interaction = default;
        if (NumericTextBox is not { } box)
            return;

        if (!Equals(box.Value, startValue))
            box.UpdateValueSource();
    }

    private enum SliderInput
    {
        None,
        Pointer,
        Key,
    }
}
