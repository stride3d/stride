// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class SettingsViewModel : PropertiesViewModel
{
    private SettingsCategoryViewModel? selectedCategory;

    public SettingsViewModel(IViewModelServiceProvider serviceProvider, SettingsProfile profile)
        : base(serviceProvider, new AssetNodeContainer())
    {
        RegisterNodePresenterUpdater(new SettingsPropertyNodeUpdater());
        Profile = profile;
        ValidateChangesCommand = new AnonymousTaskCommand(serviceProvider, ValidateChanges);
        DiscardChangesCommand = new AnonymousCommand(serviceProvider, DiscardChanges);
        Initialize();
        return;

        void DiscardChanges()
        {
            // Ensure any edition is validated by triggering lost focus, etc.
            SelectedCategory = null;
            Profile.Container.CurrentProfile.DiscardSettingsChanges();
        }

        async Task ValidateChanges()
        {
            // Ensure any edition is validated by triggering lost focus, etc.
            SelectedCategory = null;
            EditorSettings.NeedRestart = false;
            Profile.Container.CurrentProfile.ValidateSettingsChanges();
            EditorSettings.Save();
            if (EditorSettings.NeedRestart)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Some changes will be applied after you restart Game Studio."), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    /// <summary>
    /// The collection of root categories of settings.
    /// </summary>
    public SortedObservableCollection<SettingsCategoryViewModel> Categories { get; } = [];

    public SettingsProfile Profile { get; }

    /// <summary>
    /// The currently selected category.
    /// </summary>
    public SettingsCategoryViewModel? SelectedCategory
    {
        get => selectedCategory;
        set
        {
            if (SetValue(ref selectedCategory, value))
            {
                UpdateSelectedViewModel().Forget();
            }
        }
    }

    /// <summary>
    /// The command that will validate the changes.
    /// </summary>
    public ICommandBase ValidateChangesCommand { get; }

    /// <summary>
    /// The command that will discard the changes.
    /// </summary>
    public ICommandBase DiscardChangesCommand { get; }

    protected override string EmptySelectionFallbackMessage => Tr._p("Properties", "Select a settings category.");

    protected override bool CanDisplaySelectedObjects(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, out string? fallbackMessage)
    {
        fallbackMessage = null;
        return true;
    }

    protected override void FeedbackException(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, Exception exception, out string? fallbackMessage)
    {
        fallbackMessage = Tr._p("Properties", "There was a problem loading the Settings page.");
    }

    private void Initialize()
    {
        var dialogService = ServiceProvider.Get<IDialogService>();
        RegisterNodePresenterCommand(new BrowseDirectoryCommand(dialogService));
        RegisterNodePresenterCommand(new BrowseFileCommand(dialogService));

        var settingsDirectoryNames = Profile.Container.GetAllSettingsKeys().Select(x => x.DisplayName.GetFullDirectory());

        foreach (var name in settingsDirectoryNames)
        {
            SettingsCategoryViewModel? currentCategory = null;
            var categoryNames = name.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var category in categoryNames)
            {
                var nextCategory = (currentCategory != null ? currentCategory.SubCategories : Categories).FirstOrDefault(x => x.Name == category);
                if (nextCategory == null)
                {
                    nextCategory = new SettingsCategoryViewModel(ServiceProvider, Profile, category, currentCategory, (AssetNodeContainer)NodeContainer);
                    (currentCategory is not null ? currentCategory.SubCategories : Categories).Add(nextCategory);
                }
                currentCategory = nextCategory;
            }
        }

        SelectedCategory = Categories.FirstOrDefault();
    }

    private Task UpdateSelectedViewModel()
    {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        return GenerateSelectionPropertiesAsync(SelectedCategory?.Yield() ?? []);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
    }
}
