// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Editor.ViewModel.Progress;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Annotations;
using Xenko.Core.Settings;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    /// <summary>
    /// This interface represents the dialog service used for the editor. It extends <see cref="IDialogService"/> with some editor-specific dialogs.
    /// </summary>
    public interface IEditorDialogService : IDialogService
    {
        /// <summary>
        /// Gets or sets the <see cref="IAssetEditorsManager"/> instance used to open/close asset editor instances.
        /// </summary>
        IAssetEditorsManager AssetEditorsManager { get; set; }

        /// <summary>
        /// Shows a notification window in the lower right corner of the primary screen.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="message">The message of the window.</param>
        /// <param name="command">The command to execute when the user clicks on the message.</param>
        /// <param name="commandParameter">The parameter of the command to execute when the user clicks on the message.</param>
        void ShowNotificationWindow(string title, string message, ICommandBase command, object commandParameter);

        /// <summary>
        /// Immediately closes all open notification windows.
        /// </summary>
        void CloseAllNotificationWindows();

        /// <summary>
        /// Adds a question that will be shown the next time the editor receive focus or when calling <see cref="ShowDelayedNotifications"/>.
        /// </summary>
        /// <param name="confirmationSettingsKey"></param>
        /// <param name="message"></param>
        /// <param name="yesCaption"></param>
        /// <param name="noCaption"></param>
        /// <param name="yesAction"></param>
        /// <param name="noAction"></param>
        /// <param name="yesNoSettingsKey"></param>
        void AddDelayedNotification([NotNull] SettingsKey<bool> confirmationSettingsKey, [NotNull] string message, [NotNull] string yesCaption, [NotNull] string noCaption,
            Action yesAction = null, Action noAction = null, SettingsKey<bool> yesNoSettingsKey = null);

        /// <summary>
        /// Shows all notifications that were added with <see cref="AddDelayedNotification"/>.
        /// </summary>
        void ShowDelayedNotifications();

        /// <summary>
        /// Shows a window that allows to customize settings.
        /// </summary>
        /// <param name="serviceProvider">The service provider for view models.</param>
        void ShowSettingsWindow(IViewModelServiceProvider serviceProvider);

        /// <summary>
        /// Shows a progress window which can provide feedback and log on the progression of a background task. This method is non-blocking.
        /// </summary>
        /// <param name="workProgress">The <see cref="WorkProgressViewModel"/> to use to provide background task information.</param>
        /// <param name="minDelay">The minimal delay to wait before opening the dialog. If the dialog should stay open under certain conditions and the work is finished before this delay, the window will open immediately. Otherwise, it will open only if the work is not finished when the delay is over.</param>
        void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay);

        /// <summary>
        /// Creates a package instantiation window, allowing to create or open a package.
        /// </summary>
        /// <param name="session">The session in which to load the project.</param>
        /// <returns>An instance of the <see cref="INewProjectDialog"/> interface.</returns>
        INewProjectDialog CreateNewProjectDialog(SessionViewModel session);

        /// <summary>
        /// Creates a asset creation window, allowing to create or import an asset.
        /// </summary>
        /// <param name="session">The session in which to load the project.</param>
        /// <param name="directory">The directory where to create or import the asset.</param>
        /// <returns>An instance of the <see cref="IItemTemplateDialog"/> interface.</returns>
        IItemTemplateDialog CreateAddAssetDialog(SessionViewModel session, DirectoryBaseViewModel directory);

        /// <summary>
        /// Creates a asset creation window from a set of preselected templates, allowing to create or import an asset.
        /// </summary>
        /// <param name="session">The session in which to load the project.</param>
        /// <param name="directory">The directory where to create or import the asset.</param>
        /// <param name="templates">The collection of templates to display in the window.</param>
        /// <returns>An instance of the <see cref="IItemTemplateDialog"/> interface.</returns>
        IItemTemplateDialog CreateAssetTemplatesDialog(SessionViewModel session, DirectoryBaseViewModel directory, IEnumerable<TemplateAssetDescription> templates);

        /// <summary>
        /// Creates a asset creation window from a set of preselected templates, allowing to create or import an asset. Displays counters of file for each template
        /// </summary>
        /// <param name="session">The session in which to load the project.</param>
        /// <param name="directory">The directory where to create or import the asset.</param>
        /// <param name="fileCount">The total number of file</param>
        /// <param name="templates">The collection of templates to display in the window, associated with the number of file supported by this template.</param>
        /// <returns>An instance of the <see cref="IItemTemplateDialog"/> interface.</returns>
        IItemTemplateDialog CreateAssetTemplatesDialog(SessionViewModel session, DirectoryBaseViewModel directory, int fileCount, IEnumerable<KeyValuePair<TemplateAssetDescription, int>> templates);

        /// <summary>
        /// Creates an asset picker dialog.
        /// </summary>
        /// <param name="session">The session view model currently in use.</param>
        /// <returns>An instance of the <see cref="IAssetPickerDialog"/> interface.</returns>
        IAssetPickerDialog CreateAssetPickerDialog(SessionViewModel session);

        /// <summary>
        /// Creates a package picker dialog.
        /// </summary>
        /// <param name="session">The session view model currently in use.</param>
        /// <returns>An instance of the <see cref="IPackagePickerDialog"/> interface.</returns>
        IPackagePickerDialog CreatePackagePickerDialog(SessionViewModel session);

        /// <summary>
        /// Creates a fix references dialog.
        /// </summary>
        /// <param name="serviceProvider">The service provider for view models.</param>
        /// <param name="assets">The list of assets to fix.</param>
        /// <param name="dependencyManager">The dependency manager.</param>
        /// <returns>An instance of the <see cref="IFixReferencesDialog"/> interface.</returns>
        [NotNull]
        IFixReferencesDialog CreateFixAssetReferencesDialog([NotNull] IViewModelServiceProvider serviceProvider, [ItemNotNull, NotNull] IReadOnlyCollection<AssetViewModel> assets, [NotNull] IAssetDependencyManager dependencyManager);

        void ClearKeyboardFocus();

        void RegisterDefaultTemplateProviders();

        void RegisterDefaultTemplateProvider(ITemplateProvider provider);

        void RegisterAdditionalTemplateProvider(ITemplateProvider provider);

        void UnregisterAdditionalTemplateProviders();
    }

    public interface IPackagePickerDialog : IModalDialog
    {
        bool AllowMultiSelection { get; set; }

        Func<PickablePackageViewModel, bool> Filter { get; set; }

        IReadOnlyCollection<PickablePackageViewModel> SelectedPackages { get; }
    }
}
