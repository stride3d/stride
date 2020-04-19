// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Settings;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Settings.ViewModels
{
    /// <summary>
    /// This class is the global view model for editing settings.
    /// </summary>
    internal class SettingsViewModel : PropertiesViewModel
    {
        private SettingsCategoryViewModel selectedCategory;
        protected new AssetNodeContainer NodeContainer => (AssetNodeContainer)base.NodeContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> and an <see cref="IDialogService"/> to use for this view model.</param>
        /// <param name="profile">The profile associated </param>
        public SettingsViewModel([NotNull] IViewModelServiceProvider serviceProvider, SettingsProfile profile)
            : base(serviceProvider, new AssetNodeContainer())
        {
            var viewModelService = ServiceProvider.Get<GraphViewModelService>();
            RegisterNodePresenterUpdater(new SettingsPropertyNodeUpdater());
            ServiceProvider = new ViewModelServiceProvider(serviceProvider, viewModelService.Yield());
            ValidateChangesCommand = new AnonymousTaskCommand(serviceProvider, ValidateChanges);
            DiscardChangesCommand = new AnonymousCommand(serviceProvider, DiscardChanges);
            Profile = profile;
            Initialize();
        }

        /// <summary>
        /// Gets the currently selected <see cref="SettingsProfile"/>.
        /// </summary>
        public SettingsProfile Profile { get; }

        /// <summary>
        /// Gets the collection of root categories of settings.
        /// </summary>
        public SortedObservableCollection<SettingsCategoryViewModel> Categories { get; } = new SortedObservableCollection<SettingsCategoryViewModel>();

        /// <summary>
        /// Gets or sets the currently selected category.
        /// </summary>
        public SettingsCategoryViewModel SelectedCategory { get => selectedCategory; set { SetValue(ref selectedCategory, value, () => UpdateSelectedViewModel().Forget()); } }

        /// <summary>
        /// Gets the command that will validate the changes.
        /// </summary>
        public ICommandBase ValidateChangesCommand { get; }

        /// <summary>
        /// Gets the command that will discard the changes.
        /// </summary>
        public ICommandBase DiscardChangesCommand { get; }

        private void Initialize()
        {
#if DEBUG
            DebugTestSettings.Initialize();
#endif
            var dialogService = ServiceProvider.Get<IDialogService>();
            RegisterNodePresenterCommand(new BrowseDirectoryCommand(dialogService));
            RegisterNodePresenterCommand(new BrowseFileCommand(dialogService));

            var settingsDirectoryNames = Profile.Container.GetAllSettingsKeys().Select(x => x.DisplayName.GetFullDirectory()).Concat(
                                                EditorSettings.GetAllCommands().Select(x => x.DisplayName.GetFullDirectory()));

            foreach (var name in settingsDirectoryNames)
            {
                SettingsCategoryViewModel currentCategory = null;
                var categoryNames = name.ToString().Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var category in categoryNames)
                {
                    var nextCategory = (currentCategory != null ? currentCategory.SubCategories : Categories).FirstOrDefault(x => x.Name == category);
                    if (nextCategory == null)
                    {
                        nextCategory = new SettingsCategoryViewModel(ServiceProvider, Profile, category, currentCategory, NodeContainer);
                        (currentCategory != null ? currentCategory.SubCategories : Categories).Add(nextCategory);
                    }
                    currentCategory = nextCategory;
                }
            }

            SelectedCategory = Categories.First();
        }

        private Task UpdateSelectedViewModel()
        {
            return GenerateSelectionPropertiesAsync(SelectedCategory?.Yield() ?? Enumerable.Empty<SettingsCategoryViewModel>());
        }

        private async Task ValidateChanges()
        {
            // Ensure any edition is validated by triggering lost focus, etc.
            SelectedCategory = null;
            EditorSettings.NeedRestart = false;
            Profile.Container.CurrentProfile.ValidateSettingsChanges();
            EditorSettings.Save();
            if (EditorSettings.NeedRestart)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "Some changes will be applied after you restart Game Studio."), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DiscardChanges()
        {
            // Ensure any edition is validated by triggering lost focus, etc.
            SelectedCategory = null;
            Profile.Container.CurrentProfile.DiscardSettingsChanges();
        }

        protected override string EmptySelectionFallbackMessage => Tr._p("Properties", "Select a settings category.");

        protected override bool CanDisplaySelectedObjects(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, out string fallbackMessage)
        {
            fallbackMessage = null;
            return true;
        }

        protected override void FeedbackException(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, Exception exception, out string fallbackMessage)
        {
            fallbackMessage = Tr._p("Properties", "There was a problem loading the Settings page.");
        }
    }
}
