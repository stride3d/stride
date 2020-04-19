// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class ModelAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.Asset is ModelViewModel))
                return;

            if (typeof(ModelMaterial).IsAssignableFrom(node.Type) && node.IsVisible)
            {
                var materialInstance = node[nameof(ModelMaterial.MaterialInstance)];
                node.IsVisible = false;
                var name = ((ModelMaterial)node.Value).Name;
                name = !string.IsNullOrWhiteSpace(name) ? name : $"Material {node.Index}";
                materialInstance.Order = node.Index.Int;
                materialInstance.ChangeParent(node.Parent);
                materialInstance.Rename($"Material {node.Index}");
                materialInstance.DisplayName = name;
            }
            if (typeof(List<ModelMaterial>).IsAssignableFrom(node.Type) && node.IsVisible)
            {
                node.AttachedProperties.Set(DisplayData.AutoExpandRuleKey, ExpandRule.Always);
            }

            // If there is a skeleton, hide ScaleImport and PivotPosition (they are overriden by skeleton values)
            if (typeof(ModelAsset).IsAssignableFrom(node.Type))
            {
                if (node[nameof(ModelAsset.Skeleton)].Value != null)
                {
                    node[nameof(ModelAsset.PivotPosition)].IsVisible = false;
                    node[nameof(ModelAsset.ScaleImport)].IsVisible = false;
                }

                // Add dependency to reevaluate if value changes
                node.AddDependency(node[nameof(ModelAsset.Skeleton)], false);
            }
        }
    }
}
