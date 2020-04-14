// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.NodePresenters.Commands;
using Xenko.Assets.Presentation.ViewModel.Commands;

namespace Xenko.Assets.Presentation.ViewModel
{
    public class ComponentReferenceViewModel : AddReferenceViewModel
    {
        public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            EntityViewModel entity = null;
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

                bool isCompatible = entity.AssetSideEntity.Components.Any(x => TargetNode.Type.IsInstanceOfType(x));
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
            var index = subEntity.Components.IndexOf(x => TargetNode.Type.IsInstanceOfType(x));
            if (index >= 0)
            {
                var command = TargetNode.GetCommand(SetComponentReferenceCommand.CommandName);
                command.Execute(new SetComponentReferenceCommand.Parameter { Entity = subEntity, Index = index });
            }
        }
    }
}
