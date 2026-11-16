// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.ViewModels.Progress;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// A dialog service whose answers the test scripts and whose questions it records, and that shows nothing: the message boxes take
/// the scripted answers (<see cref="Answers"/>, then <see cref="Answer"/>), the dialogs and pickers take what their <c>Pick...</c>
/// function returns (nothing, as a cancel, by default), and the progress windows are recorded without opening.
/// </summary>
/// <remarks>
/// <para>Give it in the services of the session (<see cref="EditorTestSessionOptions.Services"/>); without it, the session's dialog
/// service throws on every question (<see cref="NotSupportedService{T}"/>).</para>
/// <para>Every question of <see cref="IEditorDialogService"/> has an answer: none throws. The questions the interface asks through
/// a message box (<see cref="IEditorDialogService.AddDelayedNotification"/>) take the scripted answers too; the new project and
/// project selection dialogs are cancelled, as the interface's defaults do.</para>
/// <para>It is also the progress window service of the session (<see cref="IProgressWindowService"/>), recording the windows in
/// <see cref="ProgressWindows"/>; a session without it has a <see cref="TestProgressWindowService"/>.</para>
/// </remarks>
public sealed class TestEditorDialogService : IEditorDialogService, IProgressWindowService
{
    /// <summary>
    /// The messages of the message boxes asked so far, checked ones included, in order.
    /// </summary>
    public List<string> Messages { get; } = [];

    /// <summary>
    /// The answers to the next message boxes, in order: each message box takes the first one. Once it is empty, they take
    /// <see cref="Answer"/>.
    /// </summary>
    /// <remarks>A message box with custom buttons takes the button whose result is the answer, or the first button.</remarks>
    public Queue<MessageBoxResult> Answers { get; } = new();

    /// <summary>
    /// The answer to the message boxes once <see cref="Answers"/> is empty; <see cref="MessageBoxResult.OK"/> by default.
    /// </summary>
    public MessageBoxResult Answer { get; set; } = MessageBoxResult.OK;

    /// <summary>
    /// The state of the check box of the next checked message boxes; <c>null</c> keeps the state they open with.
    /// </summary>
    public bool? CheckBoxAnswer { get; set; }

    /// <summary>
    /// Picks the button of the next message boxes with custom buttons; when <c>null</c>, the button of the scripted answer.
    /// </summary>
    public Func<IReadOnlyCollection<DialogButtonInfo>, int>? PickButton { get; set; }

    /// <summary>
    /// The progress windows shown so far, which do not open: the work does not wait for a window to close.
    /// </summary>
    public List<WorkProgressViewModel> ProgressWindows { get; } = [];

    /// <summary>
    /// The delay before each progress window of <see cref="ProgressWindows"/> would open, in milliseconds; 0 when it would open at once.
    /// </summary>
    public List<int> ProgressWindowDelays { get; } = [];

    /// <summary>
    /// The progress shown in the notification bar so far.
    /// </summary>
    public List<WorkProgressViewModel> Notifications { get; } = [];

    /// <summary>
    /// Completes when the main window is in front (<see cref="WaitForMainWindowActiveAsync"/>); at once by default.
    /// </summary>
    public Task MainWindowActive { get; set; } = Task.CompletedTask;

    /// <summary>
    /// The asset pickers opened so far.
    /// </summary>
    public List<AssetPickerViewModel> AssetPickers { get; } = [];

    /// <summary>
    /// Picks the asset of the next asset pickers from the listed ones; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetPickerViewModel, AssetViewModel?> PickAsset { get; set; } = _ => null;

    /// <summary>
    /// The fix-references dialogs opened so far.
    /// </summary>
    public List<FixAssetReferencesViewModel> FixAssetReferencesDialogs { get; } = [];

    /// <summary>
    /// Acts as the user in the next fix-references dialogs (runs its commands) and returns whether the dialog was completed;
    /// cancels them by default.
    /// </summary>
    public Func<FixAssetReferencesViewModel, Task<bool>> FixAssetReferences { get; set; } = _ => Task.FromResult(false);

    /// <summary>
    /// The Add asset dialogs opened so far (their templates).
    /// </summary>
    public List<AddAssetTemplateCollectionViewModel> AddAssetDialogs { get; } = [];

    /// <summary>
    /// Picks the template of the next Add asset dialogs; <c>null</c> cancels them.
    /// </summary>
    public Func<AddAssetTemplateCollectionViewModel, ITemplateDescriptionViewModel?> PickAddAssetTemplate { get; set; } = _ => null;

    /// <summary>
    /// The dialogs listing the templates of a set of files opened so far (their templates).
    /// </summary>
    public List<AssetTemplatesViewModel> AssetTemplatesDialogs { get; } = [];

    /// <summary>
    /// Picks the template of the next asset templates dialogs; <c>null</c> cancels them.
    /// </summary>
    public Func<AssetTemplatesViewModel, TemplateDescriptionViewModel?> PickAssetTemplate { get; set; } = _ => null;

    /// <summary>
    /// The filters of the package pickers opened so far.
    /// </summary>
    public List<Func<PickablePackageViewModel, bool>> PackagePickers { get; } = [];

    /// <summary>
    /// Picks the packages of the next package pickers; <c>null</c> cancels them.
    /// </summary>
    public Func<Func<PickablePackageViewModel, bool>, IReadOnlyCollection<PickablePackageViewModel>?> PickPackages { get; set; } = _ => null;

    /// <summary>
    /// Picks the file of the next file pickers, from their initial folder; <c>null</c> cancels them.
    /// </summary>
    public Func<UDirectory?, UFile?> PickFile { get; set; } = _ => null;

    /// <summary>
    /// Picks the files of the next pickers of several files, from their initial folder; none by default.
    /// </summary>
    public Func<UDirectory?, IReadOnlyList<UFile>> PickFiles { get; set; } = _ => [];

    /// <summary>
    /// Picks the folder of the next folder pickers, from their initial folder; <c>null</c> cancels them.
    /// </summary>
    public Func<UDirectory?, UDirectory?> PickFolder { get; set; } = _ => null;

    /// <summary>
    /// Picks the file of the next save file pickers, from their initial folder and default file name; <c>null</c> cancels them.
    /// </summary>
    public Func<UDirectory?, string?, UFile?> PickSaveFile { get; set; } = (_, _) => null;

    /// <summary>
    /// The exit codes the editor was asked to exit with; the test goes on.
    /// </summary>
    public List<int> ExitCodes { get; } = [];

    /// <inheritdoc/>
    public bool HasMainWindow => false;

    /// <inheritdoc/>
    public Task WaitForMainWindowActiveAsync(CancellationToken token = default) => MainWindowActive.WaitAsync(token);

    /// <inheritdoc/>
    public void Exit(int exitCode = 0) => ExitCodes.Add(exitCode);

    /// <inheritdoc/>
    public void ShowProgressWindow(WorkProgressViewModel workProgress) => ShowProgressWindow(workProgress, 0);

    /// <summary>
    /// Records the progress window without opening it: the work does not wait for a window to close.
    /// </summary>
    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay)
    {
        ProgressWindows.Add(workProgress);
        ProgressWindowDelays.Add(minDelay);
    }

    /// <inheritdoc/>
    public void ShowNotification(WorkProgressViewModel workProgress) => Notifications.Add(workProgress);

    /// <inheritdoc/>
    public Task<AssetViewModel?> OpenAssetPickerAsync(ISessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null, string? message = null)
    {
        var picker = new AssetPickerViewModel(session, acceptedTypes, filter, initialAsset, initialLocation) { Message = message };
        AssetPickers.Add(picker);
        return Task.FromResult(PickAsset(picker));
    }

    /// <inheritdoc/>
    public Task<bool> ShowFixAssetReferencesDialogAsync(FixAssetReferencesViewModel fixReferences)
    {
        FixAssetReferencesDialogs.Add(fixReferences);
        return FixAssetReferences(fixReferences);
    }

    /// <inheritdoc/>
    public Task<ITemplateDescriptionViewModel?> ShowAddAssetDialogAsync(AddAssetTemplateCollectionViewModel templates)
    {
        AddAssetDialogs.Add(templates);
        return Task.FromResult(PickAddAssetTemplate(templates));
    }

    /// <inheritdoc/>
    public Task<TemplateDescriptionViewModel?> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
    {
        AssetTemplatesDialogs.Add(templates);
        return Task.FromResult(PickAssetTemplate(templates));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<PickablePackageViewModel>?> ShowPackagePickerDialogAsync(SessionViewModel session, Func<PickablePackageViewModel, bool> filter)
    {
        PackagePickers.Add(filter);
        return Task.FromResult(PickPackages(filter));
    }

    /// <inheritdoc/>
    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(NextAnswer(), CheckBoxAnswer ?? isChecked));
    }

    /// <inheritdoc/>
    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(new CheckedMessageBoxResult(ButtonResult(buttons), CheckBoxAnswer ?? isChecked));
    }

    /// <inheritdoc/>
    public Task<int> CheckedMessageBoxAsync(string message, IReadOnlyCollection<DialogCheckBoxInfo> checkBoxes, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(ButtonResult(buttons));
    }

    /// <inheritdoc/>
    public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(NextAnswer());
    }

    /// <inheritdoc/>
    public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        Messages.Add(message);
        return Task.FromResult(ButtonResult(buttons));
    }

    /// <inheritdoc/>
    public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult(PickFile(initialPath));

    /// <inheritdoc/>
    public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null) => Task.FromResult(PickFiles(initialPath));

    /// <inheritdoc/>
    public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null) => Task.FromResult(PickFolder(initialPath));

    /// <inheritdoc/>
    public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null) => Task.FromResult(PickSaveFile(initialPath, defaultFileName));

    private MessageBoxResult NextAnswer() => Answers.TryDequeue(out var answer) ? answer : Answer;

    /// <summary>
    /// Answers a custom-button message box with <see cref="PickButton"/>, else the button whose result is the scripted answer, or the
    /// first one.
    /// </summary>
    private int ButtonResult(IReadOnlyCollection<DialogButtonInfo> buttons)
    {
        if (PickButton is not null)
            return PickButton(buttons);
        var answer = NextAnswer();
        var match = buttons.FirstOrDefault(x => x.Result == (int)answer) ?? buttons.First();
        return match.Result;
    }
}

/// <summary>
/// A progress window service that shows nothing and records the windows it was asked to show: the work does not wait for a window to
/// close. A session of the kit has one unless the test gives its own (or a <see cref="TestEditorDialogService"/>, which is one too), so
/// that it can run the session's own save (<see cref="SessionViewModel.SaveSessionAsync"/>).
/// </summary>
public sealed class TestProgressWindowService : IProgressWindowService
{
    /// <summary>
    /// The progress windows shown so far, each with the delay before it would open, in milliseconds.
    /// </summary>
    public List<(WorkProgressViewModel WorkProgress, int MinDelay)> Shown { get; } = [];

    /// <inheritdoc/>
    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay) => Shown.Add((workProgress, minDelay));
}
