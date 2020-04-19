// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Settings;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Settings.ViewModels
{
    /// <summary>
    /// This class is an implementation of the <see cref="CategoryViewModel{TParent,TChildren}"/> class that represents a category of settings.
    /// </summary>
    internal class SettingsCategoryViewModel : DispatcherViewModel, IComparable<SettingsCategoryViewModel>, IPropertyProviderViewModel
    {
        private readonly AssetNodeContainer nodeContainer;
        private readonly SettingsContainerNode settingsList = new SettingsContainerNode();

        [MemberCollection(ReadOnly = true)]
        internal class SettingsContainerNode : Dictionary<string, object>
        {
        }

        public SettingsCategoryViewModel(IViewModelServiceProvider serviceProvider, SettingsProfile profile, string name, SettingsCategoryViewModel parent, AssetNodeContainer nodeContainer)
            : base(serviceProvider)
        {
            this.nodeContainer = nodeContainer;
            Name = name;
            Parent = parent;
            SubCategories = new SortedObservableCollection<SettingsCategoryViewModel>();

            // Get all settings key and sort them by display name
            var settingsKeys = profile.Container.GetAllSettingsKeys();
            var settingsPath = Path;
            if (!settingsPath.EndsWith("/"))
                settingsPath += "/";
            int pathLength = settingsPath.Length;
            settingsKeys.Sort(new AnonymousComparer<SettingsKey>((x, y) => x.DisplayName.CompareTo(y.DisplayName)));

            // We keep settings that starts by settingsPath and does not contains subsequent slashes
            foreach (SettingsKey key in settingsKeys)
            {
                var displayName = key.DisplayName.ToString();
                if (displayName.StartsWith(settingsPath) && !displayName.Substring(pathLength).Contains("/"))
                {
                    var settingsObject = PackageSettingsWrapper.SettingsKeyWrapper.Create(key, profile);
                    settingsList.Add(key.DisplayName.GetFileName(), settingsObject);
                }
            }

            // Add the settings commands
            var commands = EditorSettings.GetAllCommands();
            foreach (SettingsCommand command in commands)
            {
                var displayName = command.DisplayName.ToString();
                if (displayName.StartsWith(settingsPath) && !displayName.Substring(pathLength).Contains("/"))
                {
                    settingsList.Add(command.DisplayName.GetFileName(), command);
                }
            }
        }

        public string Name { get; }

        /// <summary>
        /// Gets the path of this category.
        /// </summary>
        public string Path => Parent != null ? Parent.Path + Name + "/" : Name + "/";

        /// <summary>
        /// Gets the parent of this category.
        /// </summary>
        public SettingsCategoryViewModel Parent { get; }

        /// <summary>
        /// Gets the sub-categories contained in this category.
        /// </summary>
        public SortedObservableCollection<SettingsCategoryViewModel> SubCategories { get; private set; }

        /// <summary>
        /// Gets a dictionary representing the different settings objects (<see cref="SettingsKey"/>
        /// or <see cref="SettingsCommand"/>) indexed by their display name.
        /// </summary>
        internal IReadOnlyDictionary<string, object> SettingsList => settingsList;

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        public int CompareTo(SettingsCategoryViewModel other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            return nodeContainer.GetOrCreateNode(SettingsList);
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }
}
