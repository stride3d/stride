// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
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
public sealed class FakeEditorDialogService : IEditorDialogService, IProgressWindowService
{
    public List<string> Messages { get; } = [];

    public List<WorkProgressViewModel> ProgressWindows { get; } = [];

    /// <summary>
    /// The delay before each progress window of <see cref="ProgressWindows"/> opens, in milliseconds; 0 when it opens at once.
    /// </summary>
    public List<int> ProgressWindowDelays { get; } = [];

    public List<WorkProgressViewModel> Notifications { get; } = [];

    /// <summary>
    /// The answer to the next message boxes.
    /// </summary>
    public MessageBoxResult Answer { get; set; } = MessageBoxResult.OK;

    /// <summary>
    /// The state of the check box of the next checked message boxes; <c>null</c> keeps the state they open with.
    /// </summary>
    public bool? CheckBoxAnswer { get; set; }

    /// <summary>
    /// Completes when the main window is in front (<see cref="WaitForMainWindowActiveAsync"/>); at once by default.
    /// </summary>
    public Task MainWindowActive { get; set; } = Task.CompletedTask;

    public bool HasMainWindow => false;

    public Task WaitForMainWindowActiveAsync(CancellationToken token = default) => MainWindowActive.WaitAsync(token);

    public void Exit(int exitCode = 0)
    {
    }

    public void ShowProgressWindow(WorkProgressViewModel workProgress) => ShowProgressWindow(workProgress, 0);

    /// <summary>
    /// Records the progress window without opening it: the work does not wait for a window to close.
    /// </summary>
    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay)
    {
        ProgressWindows.Add(workProgress);
        ProgressWindowDelays.Add(minDelay);
    }

    public void ShowNotification(WorkProgressViewModel workProgress) => Notifications.Add(workProgress);

    /// <summary>
    /// The pickers opened so far.
    /// </summary>
    public List<AssetPickerViewModel> AssetPickers { get; } = [];

    /// <summary>
    /// Picks the asset of the next asset pickers from the listed ones; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetPickerViewModel, AssetViewModel?> PickAsset { get; set; } = _ => null;

    public Task<AssetViewModel?> OpenAssetPickerAsync(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null, string? message = null)
    {
        var picker = new AssetPickerViewModel(session, acceptedTypes, filter, initialAsset, initialLocation) { Message = message };
        AssetPickers.Add(picker);
        return Task.FromResult(PickAsset(picker));
    }

    /// <summary>
    /// The fix-references dialogs opened so far.
    /// </summary>
    public List<FixAssetReferencesViewModel> FixAssetReferencesDialogs { get; } = [];

    /// <summary>
    /// Acts as the user in the next fix-references dialogs (runs its commands) and returns whether the dialog was completed;
    /// cancels them by default.
    /// </summary>
    public Func<FixAssetReferencesViewModel, Task<bool>> FixAssetReferences { get; set; } = _ => Task.FromResult(false);

    public Task<bool> ShowFixAssetReferencesDialogAsync(FixAssetReferencesViewModel fixReferences)
    {
        FixAssetReferencesDialogs.Add(fixReferences);
        return FixAssetReferences(fixReferences);
    }

    /// <summary>
    /// The Add asset dialogs opened so far (their templates).
    /// </summary>
    public List<AddAssetTemplateCollectionViewModel> AddAssetDialogs { get; } = [];

    /// <summary>
    /// Picks the template of the next Add asset dialogs; <c>null</c> cancels them.
    /// </summary>
    public Func<AddAssetTemplateCollectionViewModel, ITemplateDescriptionViewModel?> PickAddAssetTemplate { get; set; } = _ => null;

    public Task<ITemplateDescriptionViewModel?> ShowAddAssetDialogAsync(AddAssetTemplateCollectionViewModel templates)
    {
        AddAssetDialogs.Add(templates);
        return Task.FromResult(PickAddAssetTemplate(templates));
    }

    /// <summary>
    /// The dialogs listing the templates of a set of files opened so far (their templates).
    /// </summary>
    public List<AssetTemplatesViewModel> AssetTemplatesDialogs { get; } = [];

    /// <summary>
    /// Picks the template of the next asset templates dialogs; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetTemplatesViewModel, TemplateDescriptionViewModel?> PickAssetTemplate { get; set; } = _ => null;

    public Task<TemplateDescriptionViewModel?> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
    {
        AssetTemplatesDialogs.Add(templates);
        return Task.FromResult(PickAssetTemplate(templates));
    }

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(Answer, CheckBoxAnswer ?? isChecked));
    }

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(ButtonResult(buttons), CheckBoxAnswer ?? isChecked));
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
