// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class SettingsCategoryViewModel : DispatcherViewModel, IPropertyProviderViewModel, IComparable<SettingsCategoryViewModel>
{
    private readonly AssetNodeContainer nodeContainer;
    private readonly SettingsContainerNode settingsList = [];

    [MemberCollection(ReadOnly = true)]
    internal class SettingsContainerNode : Dictionary<string, object>;

    public SettingsCategoryViewModel(IViewModelServiceProvider serviceProvider, SettingsProfile profile, string name, SettingsCategoryViewModel? parent, AssetNodeContainer nodeContainer)
        : base(serviceProvider)
    {
        this.nodeContainer = nodeContainer;
        Name = name;
        Parent = parent;

        // Get all settings key and sort them by display name
        var settingsKeys = profile.Container.GetAllSettingsKeys();
        var settingsPath = Path;
        if (!settingsPath.EndsWith('/'))
            settingsPath += "/";
        int pathLength = settingsPath.Length;
        settingsKeys.Sort(new AnonymousComparer<SettingsKey>((x, y) => x?.DisplayName.CompareTo(y?.DisplayName) ?? 0));

        // We keep settings that starts by settingsPath and does not contains subsequent slashes
        foreach (SettingsKey key in settingsKeys)
        {
            var displayName = key.DisplayName.ToString();
            if (displayName.StartsWith(settingsPath, StringComparison.Ordinal) && !displayName[pathLength..].Contains('/'))
            {
                var settingsObject = PackageSettingsWrapper.SettingsKeyWrapper.Create(key, profile);
                settingsList.Add(key.DisplayName.GetFileName()!, settingsObject);
            }
        }

    }

    public string Name { get; }

    /// <summary>
    /// The parent of this category.
    /// </summary>
    public SettingsCategoryViewModel? Parent { get; }

    /// <summary>
    /// The path of this category.
    /// </summary>
    public string Path => Parent is not null ? Parent.Path + Name + "/" : Name + "/";

    /// <summary>
    /// Sub-categories contained in this category.
    /// </summary>
    public SortedObservableCollection<SettingsCategoryViewModel> SubCategories { get; } = [];


    /// <summary>
    /// A dictionary representing the different settings objects (<see cref="SettingsKey"/>) indexed by their display name.
    /// </summary>
    internal IReadOnlyDictionary<string, object> SettingsList => settingsList;

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return nodeContainer.GetOrCreateNode(SettingsList);
    }

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;

    int IComparable<SettingsCategoryViewModel>.CompareTo(SettingsCategoryViewModel? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (other is null)
            return 1;

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}
