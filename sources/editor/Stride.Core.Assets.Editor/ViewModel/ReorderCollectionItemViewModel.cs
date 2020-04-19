// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class ReorderCollectionItemViewModel : IReorderItemViewModel
    {
       
        public bool CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
        {
            // FIXME: This feature is disabled for now.
            message = "";
            return false;

            //if (children.Count != 1)
            //{
            //    message = "Only a single item can be moved at a time";
            //    return false;
            //}

            //var node = children.First() as NodeViewModel;
            //if (node?.Parent == null || !(TypeDescriptorFactory.Default.Find(node.Parent.Type) is CollectionDescriptor))
            //{
            //    message = "This item cannot be moved because it is not in a collection";
            //    return false;
            //}

            //if (node.Parent.Type != targetNode.Parent.Type)
            //{
            //    message = "Invalid target location";
            //    return false;
            //}

            //object data;
            //if (!node.AssociatedData.TryGetValue("ReorderCollectionItem", out data))
            //    return false;

            //var sourceNode = (NodeViewModel)children.First();
            //var sourceIndex = sourceNode.Index.Int;
            //var targetIndex = targetNode.Index.Int;
            //if (sourceIndex == targetIndex)
            //{
            //    message = "The target position is the same that the current position";
            //    return false;
            //}

            //message = string.Format(position == InsertPosition.Before ? "Insert before {0}" : "Insert after {0}", targetNode.DisplayName);
            //return node.Index.IsInt && data is IReorderItemViewModel;
        }

        public void InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
        {
            // FIXME: This feature is disabled for now.
            //var sourceNode = (NodeViewModel)children.First();
            //var sourceIndex = sourceNode.Index.Int;
            //var targetIndex = targetNode.Index.Int;
            //if (position == InsertPosition.After)
            //    ++targetIndex;

            //if (sourceNode.Parent.NodeValue == targetNode.Parent.NodeValue && sourceIndex < targetIndex)
            //    --targetIndex;

            //var moveCommand = (NodeCommandWrapperBase)sourceNode.Parent.GetCommand(MoveItemCommand.CommandName);
            //if (moveCommand == null)
            //    return;

            //var actionService = sourceNode.ServiceProvider.Get<IUndoRedoService>();
            //using (var transaction = actionService.CreateTransaction())
            //{
            //    moveCommand.Invoke(Tuple.Create(sourceIndex, targetIndex));
            //    actionService.SetName(transaction, $"Move item {sourceIndex}");
            //}
        }

        public void SetTargetNode(NodeViewModel node)
        {
            // FIXME: This feature is disabled for now.
            //targetNode = node;
        }
    }
}
