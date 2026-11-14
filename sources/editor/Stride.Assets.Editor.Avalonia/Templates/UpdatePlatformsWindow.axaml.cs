// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Templates;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using MessageBox = Stride.Core.Presentation.Avalonia.Windows.MessageBox;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The platforms dialog of the Update platforms template (<see cref="UpdatePlatformsViewModel"/>). It closes with <c>true</c> when the
/// user accepts with at least one platform checked, and confirms the removal of the platforms they unchecked.
/// </summary>
public sealed partial class UpdatePlatformsWindow : Window
{
    public UpdatePlatformsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the platforms asked.
    /// </summary>
    public UpdatePlatformsViewModel? Platforms => DataContext as UpdatePlatformsViewModel;

    /// <summary>
    /// Asks whether to remove the projects of the platforms the user unchecked; <c>true</c> removes them. Tests replace it.
    /// </summary>
    public Func<string, Task<bool>>? ConfirmRemoval { get; set; }

    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (Platforms is not { } platforms)
            return;
        if (!platforms.HasSelection)
        {
            ErrorLabel.IsVisible = true;
            return;
        }
        ErrorLabel.IsVisible = false;

        if (platforms.RemovedCount > 0)
        {
            var message = string.Format(Tr._p("Message", "Are you sure you want to remove these {0} platform(s) from the package?"), platforms.RemovedCount);
            if (!await (ConfirmRemoval ?? AskRemovalAsync)(message))
                return;
        }
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async Task<bool> AskRemovalAsync(string message)
    {
        IReadOnlyCollection<DialogButtonInfo> buttons =
        [
            new() { Content = Tr._p("Button", "Remove"), Result = 1, IsDefault = true },
            new() { Content = Tr._p("Button", "Cancel"), Result = 2, IsCancel = true },
        ];
        return await MessageBox.ShowAsync(Title ?? string.Empty, message, buttons, MessageBoxImage.Question, this) == 1;
    }
}
