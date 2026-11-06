// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A dialog service that records the message boxes instead of showing them (a modal message box waits for a click that never
/// comes in a headless test), offers no file pickers and answers the asset pickers with <see cref="PickAsset"/>.
/// </summary>
internal sealed class RecordingDialogService : IEditorDialogService, IProgressWindowService
{
    public List<string> Messages { get; } = [];

    public List<WorkProgressViewModel> ProgressWindows { get; } = [];

    public List<WorkProgressViewModel> Notifications { get; } = [];

    /// <summary>
    /// The asset pickers opened so far.
    /// </summary>
    public List<AssetPickerViewModel> AssetPickers { get; } = [];

    /// <summary>
    /// Picks the asset of the next asset pickers; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetPickerViewModel, AssetViewModel?> PickAsset { get; set; } = _ => null;

    public void ShowProgressWindow(WorkProgressViewModel workProgress) => ProgressWindows.Add(workProgress);

    /// <summary>
    /// Records the progress window without opening it: the work does not wait for a window to close.
    /// </summary>
    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay) => ProgressWindows.Add(workProgress);

    public void ShowNotification(WorkProgressViewModel workProgress) => Notifications.Add(workProgress);

    public Task<AssetViewModel?> OpenAssetPickerAsync(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null, string? message = null)
    {
        var picker = new AssetPickerViewModel(session, acceptedTypes, filter, initialAsset, initialLocation) { Message = message };
        AssetPickers.Add(picker);
        return Task.FromResult(PickAsset(picker));
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

    /// <summary>
    /// Picks the button of the next message boxes with custom buttons; the first one by default.
    /// </summary>
    public Func<IReadOnlyCollection<DialogButtonInfo>, int> PickButton { get; set; } = buttons => buttons.First().Result;

    public bool HasMainWindow => false;

    public void Exit(int exitCode = 0) => throw new NotSupportedException();

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
    }

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
    }

    public Task<int> CheckedMessageBoxAsync(string message, IReadOnlyCollection<DialogCheckBoxInfo> checkBoxes, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(PickButton(buttons));
    }

    public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(MessageBoxResult.OK);
    }

    public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(PickButton(buttons));
    }

    public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult<UFile?>(null);

    public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult<IReadOnlyList<UFile>>([]);

    public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null) => Task.FromResult<UDirectory?>(null);

    public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null) => Task.FromResult<UFile?>(null);
}
