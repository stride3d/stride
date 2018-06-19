// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Assets.Editor.Extensions;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Effect;
using Xenko.Assets.Materials;
using Xenko.Assets.Presentation.NodePresenters.Keys;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Rendering.Materials;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class MaterialAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.Asset is MaterialViewModel))
                return;

            // Bypass the Attributes node to display directly the attribute items
            if (node.Name == nameof(MaterialAsset.Attributes) && node.Value is MaterialAttributes)
            {
                node.BypassNode();
            }

            if (node.Name == nameof(ComputeShaderClassBase<ComputeNode>.MixinReference) && node.Parent != null && typeof(IComputeNode).IsAssignableFrom(node.Parent.Type))
            {
                // Pick only effect shaders visible from the package in which the related asset is contained
                var asset = node.Asset;
                node.AttachedProperties.Add(MaterialData.Key, asset.Directory.Package.AllAssets.Where(x => x.AssetType == typeof(EffectShaderAsset)).Select(x => x.Name));
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
}
