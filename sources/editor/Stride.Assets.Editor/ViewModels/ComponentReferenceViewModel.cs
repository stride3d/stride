// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;

namespace Stride.Assets.Editor.ViewModels;

internal sealed class ComponentReferenceViewModel : AddReferenceViewModel
{
    public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        EntityViewModel? entity = null;
        bool singleChild = true;
        foreach (var child in children)
        {
            if (!singleChild)
            {
                message = "Multiple entities selected";
                return false;
            }
            entity = child as EntityViewModel;
            if (entity == null)
            {
                message = "The selection is not an entity";
                return false;
            }

            bool isCompatible = entity.AssetSideEntity.Components.Any(TargetNode.Type.IsInstanceOfType);
            if (!isCompatible)
            {
                message = "The selected entity does not have the required component";
                return false;
            }

            singleChild = false;
        }
        if (entity == null)
        {
            message = "The selection is not an entity";
            return false;
        }
        message = $"Reference {entity.Name}";
        return true;
    }

    public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        var subEntity = (EntityViewModel)children.First();
        var index = subEntity.Components.IndexOf(TargetNode.Type.IsInstanceOfType);
        if (index >= 0)
        {
            var command = TargetNode.GetCommand(SetComponentReferenceCommand.CommandName);
            command?.Execute(new SetComponentReferenceCommand.Parameter { Entity = subEntity, Index = index });
        }
    }
}
