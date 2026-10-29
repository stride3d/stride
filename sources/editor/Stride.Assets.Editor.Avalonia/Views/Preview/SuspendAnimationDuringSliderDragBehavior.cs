// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels.Preview;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// Pauses an animated preview while the user drags (or clicks) its time slider, and plays it again when the button is
/// released if it was playing, as the WPF behavior of the same name does.
/// </summary>
public sealed class SuspendAnimationDuringSliderDragBehavior : Behavior<Slider>
{
    public static readonly StyledProperty<IAnimatedPreviewViewModel?> AnimatedPreviewViewModelProperty =
        AvaloniaProperty.Register<SuspendAnimationDuringSliderDragBehavior, IAnimatedPreviewViewModel?>(nameof(AnimatedPreviewViewModel));

    private bool wasPlaying;

    /// <summary>
    /// The view model of the preview whose animation is paused.
    /// </summary>
    public IAnimatedPreviewViewModel? AnimatedPreviewViewModel
    {
        get => GetValue(AnimatedPreviewViewModelProperty);
        set => SetValue(AnimatedPreviewViewModelProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } slider)
        {
            // The slider handles the press itself (it moves to the point and starts the drag): listen first, and to handled events
            slider.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            slider.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            slider.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } slider)
        {
            slider.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            slider.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            slider.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
        }
        base.OnDetaching();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed || AnimatedPreviewViewModel is not { } viewModel)
            return;

        wasPlaying = viewModel.IsPlaying;
        if (wasPlaying)
            viewModel.PauseCommand.Execute();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
            Resume();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Resume();
    }

    private void Resume()
    {
        if (!wasPlaying)
            return;

        wasPlaying = false;
        AnimatedPreviewViewModel?.PlayCommand.Execute();
    }
}
