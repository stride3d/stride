// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Navigation;
using Stride.Navigation;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    public class NavigationNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private readonly SessionViewModel session;
        private AssetViewModel gameSettingsAssetViewModel;

        public NavigationNodeUpdater(SessionViewModel session)
        {
            this.session = session;
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if ((node.Parent?.Value?.GetType() == typeof(NavigationComponent) && node.Name == nameof(NavigationComponent.GroupId)) ||
                (node.Root.Type == typeof(NavigationMeshAsset) && node.Parent?.Name == nameof(NavigationMeshAsset.SelectedGroups) && node.Type == typeof(Guid)))
            {
                var gameSettingsAsset = GetGameSettingsAssetViewModel()?.Asset as GameSettingsAsset;
                if (gameSettingsAsset == null)
                {
                    // Selecting groups is only supported for games
                    node.AttachedProperties.Add(DisplayData.AttributeDisplayNameKey, "Not available");
                    return;
                }

                var navigationSettings = gameSettingsAsset.GetOrDefault<NavigationSettings>();

                foreach (var child in node.Children)
                    child.IsVisible = false;

                // TODO: Add dependency on game settings, so that this value updates automatically

                IEnumerable<AbstractNodeEntry> types = AbstractNodeValue.Null.Yield();

                // Add groups from navigation settings
                types = types.Concat(navigationSettings.Groups.Select(x => new AbstractNodeValue(x, x.ToString(), 0)));
                var selectedId = (Guid)node.Value;
                var selectedGroup = navigationSettings.Groups.FirstOrDefault(x => x.Id == selectedId);
                node.AttachedProperties[AbstractNodeEntryData.Key] = types;

                if (node.Commands.All(x => x.Name != CreateNewInstanceCommand.CommandName))
                    node.Commands.Add(new SyncAnonymousNodePresenterCommand(CreateNewInstanceCommand.CommandName, UpdateNavigationGroup));

                node.AttachedProperties.Add(DisplayData.AttributeDisplayNameKey, selectedGroup?.Name ?? AbstractNodeValue.Null.DisplayValue);
            }
            else if (typeof(NavigationMeshGroup).IsAssignableFrom(node.Type))
            {
                var group = (NavigationMeshGroup)node.Value;

                // Provide a display name for groups to uniquely identify them
                node.AttachedProperties.Add(DisplayData.AttributeDisplayNameKey, group?.ToString() ?? "None");

                // Bypass agent settings to simplify the group settings
                node.Children.First(x => x.Name == nameof(NavigationMeshGroup.AgentSettings)).BypassNode();
            }
        }

        private static void UpdateNavigationGroup([NotNull] INodePresenter node, [NotNull] object value)
        {
            var entry = (AbstractNodeValue)value;
            var group = entry.Value as NavigationMeshGroup;
            var guid = group?.Id ?? Guid.Empty;
            node.UpdateValue(guid);
        }

        private AssetViewModel GetGameSettingsAssetViewModel()
        {
            if (gameSettingsAssetViewModel == null)
            {
                // Find game settings asset
                foreach (var package in session.AllPackages)
                {
                    gameSettingsAssetViewModel = package.Assets.FirstOrDefault(x => x.AssetType == typeof(GameSettingsAsset));
                    if (gameSettingsAssetViewModel != null)
                        break;
                }
            }

            return gameSettingsAssetViewModel;
        }
    }
}
