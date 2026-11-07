// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Settings;
using Stride.Core.Assets.Editor.ViewModels.Progress;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

public interface IEditorDialogService : IDialogService, IAssetPickerService
{
    void ShowProgressWindow(WorkProgressViewModel workProgress);

    /// <summary>
    /// Shows the progress of a background operation in the notification bar (see <see cref="INotificationService"/>), without
    /// blocking the editor as <see cref="ShowProgressWindow"/> does.
    /// </summary>
    /// <param name="workProgress">The progress of the operation.</param>
    void ShowNotification(WorkProgressViewModel workProgress);

    /// <summary>
    /// Moves the keyboard focus away from the control that has it, so that an edit in progress (a text box of the property
    /// grid) is validated, for instance before the session is saved. A host without keyboard focus does nothing.
    /// </summary>
    void ClearKeyboardFocus()
    {
    }

    /// <summary>
    /// Waits until a question can be asked without interrupting the user: the main window of the editor is in front, with no dialog
    /// over it, as the delayed notifications of the WPF editor wait for. A host that cannot tell completes at once.
    /// </summary>
    /// <param name="token">Stops waiting; the task is then cancelled.</param>
    Task WaitForMainWindowActiveAsync(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows the new project window of a session (see <see cref="NewProjectTemplateCollectionViewModel"/>)
    /// and waits for the user to pick a template. A host without the window returns <c>null</c>.
    /// </summary>
    /// <param name="session">The session the project is added to.</param>
    /// <returns>The template and the name and location of the project, or <c>null</c> when the user cancelled.</returns>
    Task<NewPackageParameters?> ShowNewProjectDialogAsync(SessionViewModel session)
    {
        return Task.FromResult<NewPackageParameters?>(null);
    }

    /// <summary>
    /// Shows the project selection window (the recent projects and the templates of a new session, see
    /// <see cref="NewOrOpenSessionTemplateCollectionViewModel"/>). A host without the window
    /// selects nothing.
    /// </summary>
    /// <param name="templates">The recent projects and the templates to show.</param>
    /// <returns><c>true</c> when the user accepted a template or asked to open a project (the host then reads the path of the
    /// existing session or the parameters of the new one from <paramref name="templates"/>); <c>false</c> when they cancelled.</returns>
    Task<bool> ShowProjectSelectionDialogAsync(NewOrOpenSessionTemplateCollectionViewModel templates)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Shows the Add asset dialog (the asset templates of the session, see <see cref="AddAssetTemplateCollectionViewModel"/>) and
    /// waits for the user to pick a template. A host without the dialog picks nothing.
    /// </summary>
    /// <param name="templates">The templates to show.</param>
    /// <returns>The picked template, or <c>null</c> when the user cancelled.</returns>
    Task<ITemplateDescriptionViewModel?> ShowAddAssetDialogAsync(AddAssetTemplateCollectionViewModel templates)
    {
        return Task.FromResult<ITemplateDescriptionViewModel?>(null);
    }

    /// <summary>
    /// Shows the templates that can import a set of files (see <see cref="AssetTemplatesViewModel"/>) and waits for the user to
    /// pick one. A host without the dialog picks nothing.
    /// </summary>
    /// <param name="templates">The templates to show.</param>
    /// <returns>The picked template, or <c>null</c> when the user cancelled.</returns>
    Task<TemplateDescriptionViewModel?> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
    {
        return Task.FromResult<TemplateDescriptionViewModel?>(null);
    }

    /// <summary>
    /// Shows the references to fix before assets are deleted (see <see cref="FixAssetReferencesViewModel"/>) and waits for the
    /// user to decide what to do with each of them. A host without the dialog cancels.
    /// </summary>
    /// <param name="fixReferences">The references to fix, initialized with the deleted assets.</param>
    /// <returns><c>true</c> when the user handled every reference (the caller then applies the fixes with
    /// <see cref="Components.FixReferences.FixReferencesViewModel{T}.ProcessFixes"/>); <c>false</c> when they cancelled.</returns>
    Task<bool> ShowFixAssetReferencesDialogAsync(FixAssetReferencesViewModel fixReferences)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Shows the package picker of the Add dependency command, which lists the loaded and the suggested packages of the session
    /// that pass the given filter. A host without a package picker returns <c>null</c>.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="filter">The packages that can be picked.</param>
    /// <returns>The picked packages, or <c>null</c> when the user cancelled.</returns>
    Task<IReadOnlyCollection<PickablePackageViewModel>?> ShowPackagePickerDialogAsync(SessionViewModel session, Func<PickablePackageViewModel, bool> filter)
    {
        return Task.FromResult<IReadOnlyCollection<PickablePackageViewModel>?>(null);
    }

    /// <summary>
    /// Asks a yes/no question once the main window is active and nothing else is up (see <see cref="WaitForMainWindowActiveAsync"/>),
    /// with a "don't ask again" box, then runs the action of the answer. A question already waiting for the same setting is not
    /// asked twice.
    /// </summary>
    /// <param name="confirmationSettingsKey">Whether to ask; the "don't ask again" box sets it to <c>false</c>.</param>
    /// <param name="message">The question.</param>
    /// <param name="yesCaption">The caption of the yes button.</param>
    /// <param name="noCaption">The caption of the no button.</param>
    /// <param name="yesAction">What a yes does.</param>
    /// <param name="noAction">What a no does.</param>
    /// <param name="yesNoSettingsKey">The answer to use when the question is not asked; "don't ask again" saves the answer there.
    /// Without it, the answer is yes, and only a yes can be remembered.</param>
    /// <returns>A task that completes when the question was answered and its action ran. A host that keeps its own queue of
    /// questions (the WPF editor) may complete it as soon as the question is queued.</returns>
    Task AddDelayedNotification(SettingsKey<bool> confirmationSettingsKey, string message, string yesCaption, string noCaption, Action? yesAction = null, Action? noAction = null, SettingsKey<bool>? yesNoSettingsKey = null)
    {
        return DelayedNotifications.AskAsync(this, confirmationSettingsKey, message, yesCaption, noCaption, yesAction, noAction, yesNoSettingsKey);
    }
}
