// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class ContentReferenceViewModel : AddReferenceViewModel
    {
        public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            AssetViewModel asset = null;
            var singleChild = true;
            foreach (var child in children)
            {
                if (!singleChild)
                {
                    message = "Multiple assets selected";
                    return false;
                }
                asset = child as AssetViewModel;
                if (asset == null)
                {
                    message = "The selection is not an asset";
                    return false;
                }
                if (AssetRegistry.IsContentType(TargetNode.Type) || typeof(AssetReference).IsAssignableFrom(TargetNode.Type))
                {
                    var isCompatible = false;
                    var resolvedAssetTypes = AssetRegistry.GetAssetTypes(TargetNode.Type);
                    foreach (var resolvedAssetType in resolvedAssetTypes)
                    {
                        if (resolvedAssetType.IsAssignableFrom(asset.AssetType))
                        {
                            isCompatible = true;
                            break;
                        }
                    }
                    if (!isCompatible)
                    {
                        message = "Incompatible asset";
                        return false;
                    }
                }
                var command = TargetNode.GetCommand("SetContentReference");
                var param = new SetContentReferenceCommand.Parameter { Asset = asset, Type = TargetNode.Type };
                if (!command.CanExecute(param))
                {
                    message = "The selection is not valid in this context";
                    return false;
                }

                singleChild = false;
            }
            if (asset == null)
            {
                message = "The selection is not an asset";
                return false;
            }
            message = $"Reference {asset.Url}";
            return true;
        }

        public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var asset = (AssetViewModel)children.First();
            var param = new SetContentReferenceCommand.Parameter { Asset = asset, Type = TargetNode.Type };
            var command = TargetNode.GetCommand("SetContentReference");
            command.Execute(param);
        }
    }
}

