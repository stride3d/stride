// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.AddAssets.View;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Assets.Editor.Components.FixAssetReferences.Views;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.Settings;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dialogs;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.Views;
using Stride.Core.Presentation.Windows;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels.Progress;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View
{
    using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;

    public class EditorDialogService : DialogService, IWpfEditorDialogService
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

        /// <inheritdoc />
        public async Task<NewPackageParameters> ShowNewProjectDialogAsync(SessionViewModel session)
        {
            var newPackageWindow = new NewProjectWindow();
            var templates = new NewProjectTemplateCollectionViewModel(session.ServiceProvider, session);
            newPackageWindow.DataContext = templates;
            newPackageWindow.DefaultOutputDirectory = session.SolutionPath.GetFullDirectory();
            var result = await newPackageWindow.ShowModal();
            return result == DialogResult.Ok ? newPackageWindow.Parameters : null;
        }

        /// <inheritdoc />
        public async Task<ITemplateDescriptionViewModel> ShowAddAssetDialogAsync(AddAssetTemplateCollectionViewModel templates)
        {
            // TODO: We can't share session.AssetCollection.AddAssetTemplateCollection view model (used for Add asset popup) with the add asset context menu
            // because they are fighting each other, probably due to two-way bindings (on the SearchToken?)
            var addAssetWindow = new AddItemWindow(templates.ServiceProvider, templates);
            var result = await addAssetWindow.ShowModal();
            return result == DialogResult.Ok ? addAssetWindow.SelectedTemplate : null;
        }

        /// <inheritdoc />
        public async Task<TemplateDescriptionViewModel> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
        {
            var window = new ItemTemplatesWindow(templates);
            var result = await window.ShowModal();
            return result == DialogResult.Ok ? window.SelectedTemplate as TemplateDescriptionViewModel : null;
        }

        public IAssetPickerDialog CreateAssetPickerDialog(SessionViewModel session)
        {
            var assetPickerWindow = new AssetPickerWindow(session);
            var assetView = new AssetCollectionViewModel(session, filterCategories: new[] { FilterCategory.AssetName, FilterCategory.AssetTag });
            assetPickerWindow.AssetView = assetView;
            return assetPickerWindow;
        }

        /// <inheritdoc />
        public async Task<AssetViewModel> OpenAssetPickerAsync(ISessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool> filter = null, AssetViewModel initialAsset = null, DirectoryBaseViewModel initialLocation = null, string message = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (acceptedTypes == null) throw new ArgumentNullException(nameof(acceptedTypes));

            // The picker window is created on the UI thread: the property grid's commands can ask for it from another one
            var assetPicker = Dispatcher.Invoke(() => CreateAssetPickerDialog((SessionViewModel)session));
            if (message != null)
                assetPicker.Message = message;
            assetPicker.Filter = filter;
            assetPicker.InitialLocation = initialLocation;
            assetPicker.InitialAsset = initialAsset;
            assetPicker.AllowMultiSelection = false;
            assetPicker.AcceptedTypes.AddRange(acceptedTypes);

            var result = await assetPicker.ShowModal();
            return result == Stride.Core.Presentation.Services.DialogResult.Ok ? assetPicker.SelectedAssets.FirstOrDefault() : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<PickablePackageViewModel>> ShowPackagePickerDialogAsync(SessionViewModel session, Func<PickablePackageViewModel, bool> filter)
        {
            var packagePicker = new PackagePickerWindow(session) { Filter = filter };
            var result = await ((IPackagePickerDialog)packagePicker).ShowModal();
            return result == DialogResult.Ok ? ((IPackagePickerDialog)packagePicker).SelectedPackages : null;
        }

        /// <inheritdoc />
        public async Task<bool> ShowFixAssetReferencesDialogAsync(FixAssetReferencesViewModel fixReferences)
        {
            var fixReferencesWindow = new FixAssetReferencesWindow(fixReferences.ServiceProvider);
            // The last reference fixed closes the window
            fixReferences.Completed += (_, _) => fixReferencesWindow.RequestClose(DialogResult.Ok);
            fixReferencesWindow.DataContext = fixReferences;
            var result = await fixReferencesWindow.ShowModal();
            return result != DialogResult.Cancel;
        }

        /// <inheritdoc />
        /// <remarks>As everywhere in the WPF editor, the window opens after half a second: a quick work shows none.</remarks>
        public void ShowProgressWindow(WorkProgressViewModel workProgress)
        {
            ShowProgressWindow(workProgress, 500);
        }

        /// <inheritdoc />
        /// <remarks>The WPF editor has no notification bar: the progress shows in a progress window.</remarks>
        public void ShowNotification(WorkProgressViewModel workProgress)
        {
            ShowProgressWindow(workProgress, 500);
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
            // Then wait for the message boxes that are up (the upgrade question asked while a session loads):
            // the progress window would open on top of them and hide them.
            while (await nextWindow.WorkProgress.Dispatcher.InvokeAsync(() => WindowManager.ModalWindows.Count > 0 || ComponentDispatcher.IsThreadModal))
                await Task.Delay(100);

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
            var dictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Core.Assets.Editor.Wpf;component/View/DefaultPropertyTemplateProviders.xaml", UriKind.RelativeOrAbsolute));
            RegisterResourceDictionary(dictionary);
        }

        public void RegisterDefaultTemplateProvider(ITemplateProvider provider)
        {
            if (provider is not TemplateProviderBase templateProvider)
                throw new InvalidOperationException($"The template provider must be a {nameof(TemplateProviderBase)} in order to be used correctly in the property view.");

            var category = PropertyViewHelper.GetTemplateCategory(templateProvider);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(templateProvider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(templateProvider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(templateProvider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO: Move this in PluginService
        public void RegisterAdditionalTemplateProvider(ITemplateProvider provider)
        {
            AdditionalProviders.Add(provider);
            if (provider is not TemplateProviderBase templateProvider)
                throw new InvalidOperationException($"The template provider must be a {nameof(TemplateProviderBase)} in order to be used correctly in the property view.");

            var category = PropertyViewHelper.GetTemplateCategory(templateProvider);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(templateProvider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(templateProvider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(templateProvider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnregisterAdditionalTemplateProvider(ITemplateProvider provider)
        {
            AdditionalProviders.Remove(provider);
            if (provider is TemplateProviderBase templateProvider)
            {
                PropertyViewHelper.HeaderProviders.UnregisterTemplateProvider(templateProvider);
                PropertyViewHelper.FooterProviders.UnregisterTemplateProvider(templateProvider);
                PropertyViewHelper.EditorProviders.UnregisterTemplateProvider(templateProvider);
            }
        }

        // TODO: Move this in PluginService
        public void UnregisterAdditionalTemplateProviders()
        {
            foreach (var provider in AdditionalProviders.OfType<TemplateProviderBase>())
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

        /// <inheritdoc />
        /// <remarks>The task completes once the question is asked, or queued until <see cref="ShowDelayedNotifications"/>.</remarks>
        public Task AddDelayedNotification(SettingsKey<bool> confirmationSettingsKey, string message, string yesCaption, string noCaption, Action yesAction, Action noAction, SettingsKey<bool> yesNoSettingsKey)
        {
            if (confirmationSettingsKey == null) throw new ArgumentNullException(nameof(confirmationSettingsKey));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (yesCaption == null) throw new ArgumentNullException(nameof(yesCaption));
            if (noCaption == null) throw new ArgumentNullException(nameof(noCaption));

            // Prevent duplicate
            if (delayedNotifications.Any(t => ReferenceEquals(t.Item1, confirmationSettingsKey)))
                return Task.CompletedTask;

            Action action = async () =>
            {
                var yesNo = yesNoSettingsKey?.GetValue() ?? true;
                var ask = confirmationSettingsKey.GetValue();
                if (ask)
                {
                    var buttons = DialogHelper.CreateButtons(new[] { yesCaption, noCaption }, 1, 2);
                    var result = await CheckedMessageBoxAsync(message, false, DialogHelper.DontAskAgain, buttons, MessageBoxImage.Question);
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
            if (!IsBusy && (windows.Count == 0 || windows.OfType<Window>().Any(w => w.IsActive)))
            {
                // Execute immediately
                Dispatcher.Invoke(action);
                return Task.CompletedTask;
            }

            // Add a new question
            delayedNotifications.Enqueue(Tuple.Create((SettingsKey)confirmationSettingsKey, action));
            return Task.CompletedTask;
        }

        public void ShowDelayedNotifications()
        {
            // The questions wait for the window that is up; closing it shows them (MainWindowUnblocked).
            if (IsBusy)
                return;

            Tuple<SettingsKey, Action> notification;
            while (delayedNotifications.TryDequeue(out notification))
            {
                Dispatcher.Invoke(notification.Item2);
            }
        }

        /// <summary>
        /// A blocking or modal window is up: a progress window, a message box. A question asked now would
        /// land on top of it.
        /// </summary>
        private static bool IsBusy => WindowManager.BlockingWindows.Count > 0 || WindowManager.ModalWindows.Count > 0;
    }
}
