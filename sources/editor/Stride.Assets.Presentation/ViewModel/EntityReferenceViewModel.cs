// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.NodePresenters.Commands;
using Stride.Assets.Presentation.ViewModel.Commands;

namespace Stride.Assets.Presentation.ViewModel
{
    public class EntityReferenceViewModel : AddReferenceViewModel
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
            var command = TargetNode.GetCommand(SetEntityReferenceCommand.CommandName);
            command.Execute(subEntity);
        }
    }
}
