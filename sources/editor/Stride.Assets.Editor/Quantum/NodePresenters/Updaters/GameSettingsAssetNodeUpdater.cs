// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Data;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class GameSettingsAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is not GameSettingsViewModel)
            return;

        if (node is { Name: nameof(GameSettingsAsset.Defaults), Value: List<Configuration> })
        {
            foreach (var item in node.Children.ToList())
            {
                item.Order = node.Order + item.Index.Int;
                item.AttachedProperties.Add(CategoryData.Key, true);
            }
            if (node.Commands.All(cmd => cmd.Name != AddNewItemCommand.CommandName))
                node.Commands.Add(new AddNewItemCommand());
            if (node.Commands.All(cmd => cmd.Name != RemoveItemCommand.CommandName))
                node.Commands.Add(new RemoveItemCommand());
        }

        if (typeof(Configuration).IsAssignableFrom(node.Type))
        {
            node.DisplayName = DisplayAttribute.GetDisplayName(node.Value?.GetType()) ?? DisplayAttribute.GetDisplayName(typeof(Configuration));
        }

        if (typeof(ConfigurationOverride).IsAssignableFrom(node.Type))
        {
            node.DisplayName = $"Override {node.Index}";
            node.AttachedProperties.Add(CategoryData.Key, true);
        }

        if (node.Parent != null && typeof(ConfigurationOverride).IsAssignableFrom(node.Parent.Type) && node.Name == nameof(ConfigurationOverride.SpecificFilter))
        {
            if (node.Commands.All(x => x.Name != "ClearSelection"))
            {
                node.Commands.Add(new SyncAnonymousNodePresenterCommand("ClearSelection", (n, x) => n.UpdateValue(-1)));
            }
        }

        if (typeof(ICollection<Configuration>).IsAssignableFrom(node.Type))
        {
            var types = node.AttachedProperties.Get(AbstractNodeEntryData.Key);
            types = types.Where(x => ((ICollection<Configuration>)node.Value).All(y => !(x is AbstractNodeType type && y.GetType() == type.Type)));
            node.AttachedProperties.Set(AbstractNodeEntryData.Key, types);
        }
    }
}
