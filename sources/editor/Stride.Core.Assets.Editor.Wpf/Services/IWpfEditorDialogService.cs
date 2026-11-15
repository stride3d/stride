// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.Views;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The dialog service of the WPF editor: the shared <see cref="IEditorDialogService"/>, and what only the WPF windows and views
    /// use (notification windows, the delayed notifications queue, the settings window, the asset picker window, the property
    /// grid template providers).
    /// </summary>
    public interface IWpfEditorDialogService : IEditorDialogService, IDialogService2, IProgressWindowService
    {
        /// <summary>
        /// Shows a notification window at the bottom right of the screen, with a button that runs the given command.
        /// </summary>
        void ShowNotificationWindow(string title, string message, ICommandBase command, object commandParameter);

        void CloseAllNotificationWindows();

        /// <summary>
        /// Asks the delayed notifications that wait for the main window, once it is active and no other window is up.
        /// </summary>
        void ShowDelayedNotifications();

        void ShowSettingsWindow(IViewModelServiceProvider serviceProvider);

        IAssetPickerDialog CreateAssetPickerDialog(SessionViewModel session);

        void RegisterDefaultTemplateProviders();

        void RegisterDefaultTemplateProvider(ITemplateProvider provider);

        void RegisterAdditionalTemplateProvider(ITemplateProvider provider);

        void UnregisterAdditionalTemplateProvider(ITemplateProvider provider);

        void UnregisterAdditionalTemplateProviders();
    }

    public interface IPackagePickerDialog : IModalDialog
    {
        bool AllowMultiSelection { get; set; }

        Func<PickablePackageViewModel, bool> Filter { get; set; }

        IReadOnlyCollection<PickablePackageViewModel> SelectedPackages { get; }
    }
}
