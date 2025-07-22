// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Effect;
using Stride.Assets.Materials;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class MaterialAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is not MaterialViewModel)
            return;

        // Bypass the Attributes node to display directly the attribute items
        if (node is { Name: nameof(MaterialAsset.Attributes), Value: MaterialAttributes })
        {
            node.BypassNode();
        }

        if (node is { Name: nameof(ComputeShaderClassBase<ComputeNode>.MixinReference), Parent: not null } && typeof(IComputeNode).IsAssignableFrom(node.Parent.Type))
        {
            // Pick only effect shaders visible from the package in which the related asset is contained
            var asset = node.Asset;
            // FIXME xplat-editor should it use "AllAssets" from PackageViewModel?
            node.AttachedProperties.Add(MaterialData.Key, asset.Directory.Package.Assets.Where(x => x.AssetType == typeof(EffectShaderAsset)).Select(x => x.Name));
        }

        // The node is a material blend layer
        if (typeof(MaterialBlendLayer).IsAssignableFrom(node.Type))
        {
            var layer = (MaterialBlendLayer)node.Value;
            node.DisplayName = !string.IsNullOrWhiteSpace(layer.Name) ? layer.Name : $"Layer {node.Index}";
            node[nameof(MaterialBlendLayer.Name)].IsVisible = false;
            if (node.Commands.All(x => x.Name != RenameStringKeyCommand.CommandName))
            {
                node.Commands.Add(new SyncAnonymousNodePresenterCommand(RenameStringKeyCommand.CommandName, (x, name) => x[nameof(MaterialBlendLayer.Name)].UpdateValue(name)));
            }
            node.AddDependency(node[nameof(MaterialBlendLayer.Name)], false);
        }
    }
}
