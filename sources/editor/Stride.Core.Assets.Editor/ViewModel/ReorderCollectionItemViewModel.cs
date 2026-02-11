// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class ReorderCollectionItemViewModel : IReorderItemViewModel
    {
        private NodeViewModel targetNode;

        public bool CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
        {
            if (children.Count != 1)
            {
                message = "Only a single item can be moved at a time";
                return false;
            }

            var node = children.First() as NodeViewModel;
            if (node?.Parent == null || !(TypeDescriptorFactory.Default.Find(node.Parent.Type) is CollectionDescriptor))
            {
                message = "This item cannot be moved because it is not in a collection";
                return false;
            }

            if (node.Parent != targetNode.Parent)
            {
                message = "Invalid target location";
                return false;
            }

            object data;
            if (!node.AssociatedData.TryGetValue(CollectionData.ReorderCollectionItem, out data))
            {
                message = "This item cannot be reordered";
                return false;
            }

            var sourcePresenter = node.NodePresenters.FirstOrDefault() as ItemNodePresenter;
            var targetPresenter = targetNode.NodePresenters.FirstOrDefault() as ItemNodePresenter;

            if (sourcePresenter == null || targetPresenter == null ||
                !sourcePresenter.Index.IsInt || !targetPresenter.Index.IsInt)
            {
                message = "Items with non-integer indices cannot be reordered";
                return false;
            }

            var sourceIndex = sourcePresenter.Index.Int;
            var targetIndex = targetPresenter.Index.Int;
            if (sourceIndex == targetIndex)
            {
                message = "The target position is the same as the current position";
                return false;
            }

            message = string.Format(position == InsertPosition.Before ? "Insert before {0}" : "Insert after {0}", targetNode.DisplayName);
            return data is IReorderItemViewModel;
        }

        public void InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
        {
            var sourceNode = (NodeViewModel)children.First();
            var sourcePresenter = sourceNode.NodePresenters.FirstOrDefault() as ItemNodePresenter;
            var targetPresenter = targetNode.NodePresenters.FirstOrDefault() as ItemNodePresenter;

            if (sourcePresenter == null || targetPresenter == null)
            {
                return;
            }

            var sourceIndex = sourcePresenter.Index.Int;
            var targetIndex = targetPresenter.Index.Int;

            if (position == InsertPosition.After)
            {
                ++targetIndex;
            }

            if (sourceNode.Parent.NodeValue == targetNode.Parent.NodeValue && sourceIndex < targetIndex)
            {
                --targetIndex;
            }

            var moveCommand = (NodePresenterCommandWrapper)sourceNode.GetCommand(MoveItemCommand.CommandName);
            if (moveCommand == null)
            {
                return;
            }

            var actionService = sourceNode.ServiceProvider.Get<IUndoRedoService>();
            using var transaction = actionService.CreateTransaction();
            moveCommand.Invoke(Tuple.Create(sourceIndex, targetIndex));
            actionService.SetName(transaction, $"Move item {sourceIndex}");
        }

        public void SetTargetNode(NodeViewModel node)
        {
            targetNode = node;
        }
    }
}
