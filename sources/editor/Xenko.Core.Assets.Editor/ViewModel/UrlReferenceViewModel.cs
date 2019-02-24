using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.Quantum;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Serialization;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    public class UrlReferenceViewModel : AddReferenceViewModel
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
                if (UrlReferenceHelper.IsUrlReferenceType(TargetNode.Type))
                {

                    var isCompatible = false;

                    var targetType = UrlReferenceHelper.GetTargetType(TargetNode.Type);

                    if (targetType == null)
                    {
                        isCompatible = true;
                    }
                    else
                    {
                        var resolvedAssetTypes = AssetRegistry.GetAssetTypes(targetType);
                        foreach (var resolvedAssetType in resolvedAssetTypes)
                        {
                            if (resolvedAssetType.IsAssignableFrom(asset.AssetType))
                            {
                                isCompatible = true;
                                break;
                            }
                        }
                    }

                    if (!isCompatible)
                    {
                        message = "Incompatible asset";
                        return false;
                    }
                }
                var command = TargetNode.GetCommand(SetUrlReferenceCommand.CommandName);
                var param = new SetUrlReferenceCommand.Parameter { Asset = asset, Type = TargetNode.Type };
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
            var param = new SetUrlReferenceCommand.Parameter { Asset = asset, Type = TargetNode.Type };
            var command = TargetNode.GetCommand(SetUrlReferenceCommand.CommandName);
            command.Execute(param);
        }
    }

}
