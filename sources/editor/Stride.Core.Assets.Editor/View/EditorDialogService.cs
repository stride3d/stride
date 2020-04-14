// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.Components.AddAssets;
using Xenko.Core.Assets.Editor.Components.AddAssets.View;
using Xenko.Core.Assets.Editor.Components.FixAssetReferences;
using Xenko.Core.Assets.Editor.Components.FixAssetReferences.Views;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.Views;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Editor.ViewModel.Progress;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Settings;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Dialogs;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Presentation.Windows;

namespace Xenko.Core.Assets.Editor.View
{
    using MessageBoxButton = Presentation.Services.MessageBoxButton;
    using MessageBoxImage = Presentation.Services.MessageBoxImage;
    using MessageBoxResult = Presentation.Services.MessageBoxResult;

    public class EditorDialogService : DialogService, IEditorDialogService
    {
        private struct PendingWorkProgress
        {
            public PendingWorkProgress(WorkProgressViewModel workProgress)
            {
                WorkProgress = workProgress;
                ReadyToDisplay = new TaskCompletionSource<int>();
                Displayed = new TaskCompletionSource<int>();
            }
            public WorkProgressViewModel WorkProgress { get; }
            public TaskCompletionSource<int> ReadyToDisplay { get; }
            public TaskCompletionSource<int> Displayed { get; }
        }

        private static readonly List<ITemplateProvider> AdditionalProviders = new List<ITemplateProvider>();
        private readonly List<NotificationWindow> notificationWindows = new List<NotificationWindow>();
        private readonly List<PendingWorkProgress> pendingProgressWindows = new List<PendingWorkProgress>();
        private readonly ConcurrentQueue<Tuple<SettingsKey, Action>> delayedNotifications = new ConcurrentQueue<Tuple<SettingsKey, Action>>();

        private SettingsWindow settingsWindow;

        public EditorDialogService(IDispatcherService dispatcher, string applicationName)
            : base(dispatcher, applicationName)
        {
        }

        public IAssetEditorsManager AssetEditorsManager { get; set; }

        public void ShowNotificationWindow(string title, string message, ICommandBase command, object commandParameter)
        {
            var notificationWindow = new NotificationWindow(title, message, command, commandParameter);
            lock (notificationWindows)
            {
                notificationWindows.Add(notificationWindow);
            }
            notificationWindow.Closed += (s, e) => { lock (notificationWindows) { notificationWindows.Remove(notificationWindow); } };
            notificationWindow.Show();
        }

        public void CloseAllNotificationWindows()
        {
            lock (notificationWindows)
            {
                foreach (var notificationWindow in notificationWindows.ToArray())
                {
                    notificationWindow.Close();
                }
                settingsWindow?.Close();
            }
        }

        public void ShowSettingsWindow(IViewModelServiceProvider serviceProvider)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow(serviceProvider);
                settingsWindow.Closed += (sender, e) => settingsWindow = null;
            }

            settingsWindow.ShowModal().Forget();
        }

        public INewProjectDialog CreateNewProjectDialog(SessionViewModel session)
        {
            var newPackageWindow = new NewProjectWindow();
            var templates = new NewProjectTemplateCollectionViewModel(session.ServiceProvider, session);
            newPackageWindow.DataContext = templates;
            return newPackageWindow;
        }

        public IItemTemplateDialog CreateAddAssetDialog(SessionViewModel session, DirectoryBaseViewModel directory)
        {
            // TODO: We can't share session.ActiveAssetView.AddAssetTemplateCollection view model (used for Add asset popup) with the add asset context menu
            // because they are fighting each other, probably due to two-way bindings (on the SearchToken?)
            //var addAssetWindow = new AddItemWindow(session.ServiceProvider, session.ActiveAssetView.AddAssetTemplateCollection);
            var addAssetWindow = new AddItemWindow(session.ServiceProvider, new AddAssetTemplateCollectionViewModel(session));
            return addAssetWindow;
        }

        public IItemTemplateDialog CreateAssetTemplatesDialog(SessionViewModel session, DirectoryBaseViewModel directory, IEnumerable<TemplateAssetDescription> templates)
        {
            var viewModel = new AssetTemplatesViewModel(session.ServiceProvider, templates);
            var window = new ItemTemplatesWindow(viewModel);
            return window;
        }

        public IItemTemplateDialog CreateAssetTemplatesDialog(SessionViewModel session, DirectoryBaseViewModel directory, int fileCount, IEnumerable<KeyValuePair<TemplateAssetDescription, int>> templates)
        {
            var viewModel = new AssetTemplatesViewModel(session.ServiceProvider, fileCount, templates);
            var window = new ItemTemplatesWindow(viewModel);
            return window;
        }

        public IAssetPickerDialog CreateAssetPickerDialog(SessionViewModel session)
        {
            var assetPickerWindow = new AssetPickerWindow(session);
            var assetView = new AssetCollectionViewModel(session.ServiceProvider, session, new[] { FilterCategory.AssetName, FilterCategory.AssetTag });
            assetPickerWindow.AssetView = assetView;
            return assetPickerWindow;
        }

        public IPackagePickerDialog CreatePackagePickerDialog(SessionViewModel session)
        {
            var packagePickerWindow = new PackagePickerWindow(session);
            return packagePickerWindow;
        }

        public IFixReferencesDialog CreateFixAssetReferencesDialog(IViewModelServiceProvider serviceProvider, IReadOnlyCollection<AssetViewModel> assets, IAssetDependencyManager dependencyManager)
        {
            var fixReferencesWindow = new FixAssetReferencesWindow(serviceProvider);
            var viewModel = new FixAssetReferencesViewModel(serviceProvider, assets, dependencyManager, fixReferencesWindow);
            viewModel.Initialize(assets);
            fixReferencesWindow.DataContext = viewModel;
            return fixReferencesWindow;
        }

        public async void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay)
        {
            if (workProgress == null) throw new ArgumentNullException(nameof(workProgress));
            // Tell the work progress view model that a window will be open for it
            workProgress.NotifyWindowWillOpen();

            // Create a container object for the work progress to put it in the queue
            var progress = new PendingWorkProgress(workProgress);

            // As soon as the work is finished, we should display the window, even if the delay is not complete
            workProgress.WorkFinished += (sender, e) => progress.ReadyToDisplay.TrySetResult(0);
            if (workProgress.WorkDone)
                progress.ReadyToDisplay.TrySetResult(0);

            // Compute the list of progress window that should be displayed before this one
            List<PendingWorkProgress> precedingWindows;
            lock (pendingProgressWindows)
            {
                precedingWindows = new List<PendingWorkProgress>(pendingProgressWindows);
                pendingProgressWindows.Add(progress);
            }
            // Enqueue a task to display the window when it's ready.
            DisplayProgressWindow(precedingWindows, progress);

            // Wait the delay before notifying that we're ready to display
            await Task.Delay(minDelay);
            progress.ReadyToDisplay.TrySetResult(0);
        }

        private async void DisplayProgressWindow(List<PendingWorkProgress> precedingWindows, PendingWorkProgress nextWindow)
        {
            // Wait for all preceding windows to be displayed first
            await Task.WhenAll(precedingWindows.Select(x => x.Displayed.Task));
            // Then wait for the next window to be ready to display
            await nextWindow.ReadyToDisplay.Task;

            // Check if we should actually display the window
            if (!nextWindow.WorkProgress.WorkDone || nextWindow.WorkProgress.ShouldStayOpen())
            {
                await nextWindow.WorkProgress.Dispatcher.InvokeAsync(() =>
                {
                    var progressWindow = new WorkProgressWindow(nextWindow.WorkProgress);

                    // Remove this window from the list of pending window
                    lock (pendingProgressWindows)
                    {
                        pendingProgressWindows.Remove(nextWindow);
                    }

                    try
                    {
                        // Notify in the next frame that the window has been displayed
                        nextWindow.WorkProgress.Dispatcher.InvokeAsync(() => nextWindow.Displayed.SetResult(0));
                        WindowManager.ShowBlockingWindow(progressWindow);
                    }
                    catch (Exception e)
                    {
                        // On Windows 8, an exception might occur in the System.Windows.Shell.WindowChromeWorker class
                        // if the progress window is closed programmatically, apparently if this happens too early after
                        // loading it. Let's ignore the exception for the moment, as a workaround.
                        e.Ignore();
                    }
                });
            }
            else
            {
                // Remove this window from the list of pending window
                lock (pendingProgressWindows)
                {
                    pendingProgressWindows.Remove(nextWindow);
                }
                // Notify that the window should be considered as displayed.
                nextWindow.Displayed.SetResult(0);
            }

            nextWindow.WorkProgress.NotifyWindowClosed();
        }

        public void ClearKeyboardFocus()
        {
            if (Application.Current.MainWindow != null)
            {
                var focusScope = FocusManager.GetFocusScope(Application.Current.MainWindow);
                FocusManager.SetFocusedElement(focusScope, Application.Current.MainWindow);
            }
        }

        public void RegisterDefaultTemplateProviders()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Xenko.Core.Assets.Editor;component/View/DefaultPropertyTemplateProviders.xaml", UriKind.RelativeOrAbsolute));
            RegisterResourceDictionary(dictionary);
        }

        public void RegisterDefaultTemplateProvider(ITemplateProvider provider)
        {
            var dependencyObject = provider as DependencyObject;
            if (dependencyObject == null)
                throw new InvalidOperationException("The template provider must be a dependency object in order to be used correctly in the property view.");

            var category = PropertyViewHelper.GetTemplateCategory(dependencyObject);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(provider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO: Move this in PluginService
        public void RegisterAdditionalTemplateProvider(ITemplateProvider provider)
        {
            AdditionalProviders.Add(provider);
            var dependencyObject = provider as DependencyObject;
            if (dependencyObject == null)
                throw new InvalidOperationException("The template provider must be a dependency object in order to be used correctly in the property view.");

            var category = PropertyViewHelper.GetTemplateCategory(dependencyObject);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(provider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO: Move this in PluginService
        public void UnregisterAdditionalTemplateProviders()
        {
            foreach (var provider in AdditionalProviders)
            {
                PropertyViewHelper.HeaderProviders.UnregisterTemplateProvider(provider);
                PropertyViewHelper.FooterProviders.UnregisterTemplateProvider(provider);
                PropertyViewHelper.EditorProviders.UnregisterTemplateProvider(provider);
            }
        }

        public void RegisterResourceDictionary(ResourceDictionary dictionary)
        {
            foreach (object value in dictionary.Values)
            {
                var provider = value as ITemplateProvider;
                if (provider != null)
                {
                    RegisterDefaultTemplateProvider(provider);
                }
            }
        }

        public void AddDelayedNotification(SettingsKey<bool> confirmationSettingsKey, string message, string yesCaption, string noCaption, Action yesAction, Action noAction, SettingsKey<bool> yesNoSettingsKey)
        {
            if (confirmationSettingsKey == null) throw new ArgumentNullException(nameof(confirmationSettingsKey));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (yesCaption == null) throw new ArgumentNullException(nameof(yesCaption));
            if (noCaption == null) throw new ArgumentNullException(nameof(noCaption));

            // Prevent duplicate
            if (delayedNotifications.Any(t => ReferenceEquals(t.Item1, confirmationSettingsKey)))
                return;

            Action action = async () =>
            {
                var yesNo = yesNoSettingsKey?.GetValue() ?? true;
                var ask = confirmationSettingsKey.GetValue();
                if (ask)
                {
                    var buttons = DialogHelper.CreateButtons(new[] { yesCaption, noCaption }, 1, 2);
                    var result = await CheckedMessageBox(message, false, DialogHelper.DontAskAgain, buttons, MessageBoxImage.Question);
                    // Close without clicking on a button
                    if (result.Result == 0)
                        return;

                    yesNo = result.Result == 1;
                    if (result.IsChecked == true && (yesNo || yesNoSettingsKey != null))
                    {
                        confirmationSettingsKey.SetValue(false);
                        yesNoSettingsKey?.SetValue(yesNo);
                        Settings.EditorSettings.Save();
                    }
                }
                if (yesNo)
                    yesAction?.Invoke();
                else
                    noAction?.Invoke();
            };

            var windows = Application.Current.Windows;
            if (windows.Count == 0 || windows.OfType<Window>().Any(w => w.IsActive))
            {
                // Execute immediately
                Dispatcher.Invoke(action);
                return;
            }

            // Add a new question
            delayedNotifications.Enqueue(Tuple.Create((SettingsKey)confirmationSettingsKey, action));
        }

        public void ShowDelayedNotifications()
        {
            Tuple<SettingsKey, Action> notification;
            while (delayedNotifications.TryDequeue(out notification))
            {
                Dispatcher.Invoke(notification.Item2);
            }
        }
    }
}
