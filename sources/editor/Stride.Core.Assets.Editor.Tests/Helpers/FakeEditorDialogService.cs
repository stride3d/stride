// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A dialog service that records the message boxes and answers them with <see cref="Answer"/>; the file pickers return nothing.
/// </summary>
public sealed class FakeEditorDialogService : IEditorDialogService
{
    public List<string> Messages { get; } = [];

    public List<WorkProgressViewModel> ProgressWindows { get; } = [];

    public List<WorkProgressViewModel> Notifications { get; } = [];

    /// <summary>
    /// The answer to the next message boxes.
    /// </summary>
    public MessageBoxResult Answer { get; set; } = MessageBoxResult.OK;

    public bool HasMainWindow => false;

    public void Exit(int exitCode = 0)
    {
    }

    public void ShowProgressWindow(WorkProgressViewModel workProgress) => ProgressWindows.Add(workProgress);

    public void ShowNotification(WorkProgressViewModel workProgress) => Notifications.Add(workProgress);

    /// <summary>
    /// The pickers opened so far.
    /// </summary>
    public List<AssetPickerViewModel> AssetPickers { get; } = [];

    /// <summary>
    /// Picks the asset of the next asset pickers from the listed ones; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetPickerViewModel, AssetViewModel?> PickAsset { get; set; } = _ => null;

    public Task<AssetViewModel?> OpenAssetPickerAsync(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null)
    {
        var picker = new AssetPickerViewModel(session, acceptedTypes, filter, initialAsset, initialLocation);
        AssetPickers.Add(picker);
        return Task.FromResult(PickAsset(picker));
    }

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(Answer, isChecked));
    }

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(ButtonResult(buttons), isChecked));
    }

    public Task<int> CheckedMessageBoxAsync(string message, IReadOnlyCollection<DialogCheckBoxInfo> checkBoxes, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(ButtonResult(buttons));
    }

    public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(Answer);
    }

    public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(ButtonResult(buttons));
    }

    public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult<UFile?>(null);

    public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult<IReadOnlyList<UFile>>([]);

    public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null) => Task.FromResult<UDirectory?>(null);

    public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null) => Task.FromResult<UFile?>(null);

    /// <summary>
    /// Picks the button of the next message boxes with custom buttons; when <c>null</c>, the button matching <see cref="Answer"/>.
    /// </summary>
    public Func<IReadOnlyCollection<DialogButtonInfo>, int>? PickButton { get; set; }

    /// <summary>
    /// Answers a custom-button message box with <see cref="PickButton"/>, else the button matching <see cref="Answer"/>, or the
    /// first one.
    /// </summary>
    private int ButtonResult(IReadOnlyCollection<DialogButtonInfo> buttons)
    {
        if (PickButton is not null)
            return PickButton(buttons);
        var match = buttons.FirstOrDefault(x => x.Result == (int)Answer) ?? buttons.First();
        return match.Result;
    }
}
