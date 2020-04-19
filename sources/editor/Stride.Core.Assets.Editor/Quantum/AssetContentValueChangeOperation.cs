// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum
{
    public class AssetContentValueChangeOperation : ContentValueChangeOperation
    {
        private readonly OverrideType newOverride;
        private readonly OverrideType previousOverride;
        private readonly ItemId itemId;

        public AssetContentValueChangeOperation(IAssetNode node, ContentChangeType changeType, NodeIndex index, object oldValue, object newValue, OverrideType previousOverride, OverrideType newOverride, ItemId itemId, IEnumerable<IDirtiable> dirtiables)
            : base(node, changeType, index, oldValue, newValue, dirtiables)
        {
            this.previousOverride = previousOverride;
            this.itemId = itemId;
            this.newOverride = newOverride;
        }

        protected new IAssetNode Node => (IAssetNode)base.Node;

        /// <inheritdoc/>
        public override bool HasEffect => base.HasEffect || !Equals(previousOverride, newOverride);

        /// <inheritdoc/>
        public override bool CanMerge(IMergeableOperation otherOperation)
        {
            var operation = otherOperation as AssetContentValueChangeOperation;
            return newOverride == operation?.previousOverride && base.CanMerge(otherOperation);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsFrozen)
            {
                return $"{{{nameof(AssetContentValueChangeOperation)}: (Frozen)}}";
            }

            try
            {
                var sb = new StringBuilder($"{{{nameof(AssetContentValueChangeOperation)}: {(Node as IMemberNode)?.Name ?? Node.Type.Name}");
                if (previousOverride != newOverride)
                {
                    var previousString = previousOverride != OverrideType.Base ? previousOverride.ToText() : "∅";
                    var newString = newOverride != OverrideType.Base ? newOverride.ToText() : "∅";
                    sb.Append($"({previousString} -> {newString})");
                }
                switch (ChangeType)
                {
                    case ContentChangeType.ValueChange:
                        sb.Append($" = {NewValue}}}");
                        break;
                    case ContentChangeType.CollectionUpdate:
                        sb.Append($"[{Index.Value}] = {NewValue}");
                        break;
                    case ContentChangeType.CollectionAdd:
                        sb.Append($" ++[{Index.Value}] = {NewValue}}}");
                        break;
                    case ContentChangeType.CollectionRemove:
                        sb.Append($" --[{Index.Value}] = {NewValue}}}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            // If we're undoing an add, we need to get the id of the item before removing it.
            var id = ItemId.Empty;
            if (ChangeType == ContentChangeType.CollectionAdd)
            {
                var objectNode = (IAssetObjectNode)Node;
                objectNode.TryIndexToId(Index, out id);
            }
            base.Undo();
            // If this undo restored inheritance from the base, we have extra-work to do to cancel the override information.
            if (!previousOverride.HasFlag(OverrideType.New) && Node.BaseNode != null)
            {
                if (ChangeType != ContentChangeType.CollectionAdd)
                {
                    // For value change or remove, we just need to indicate that the value is not overridden anymore
                    if (Index == NodeIndex.Empty)
                    {
                        ((IAssetMemberNode)Node).OverrideContent(false);
                    }
                    else
                    {
                        ((IAssetObjectNode)Node).OverrideItem(false, Index);
                    }
                }
                else
                {
                    // For add, we must remove the id of the item from the list of deleted ids.
                    if (id != ItemId.Empty)
                    {
                        ((IAssetObjectNode)Node).OverrideDeletedItem(false, id);
                    }
                }
            }
        }

        protected override void Redo()
        {
            base.Redo();
            if (!newOverride.HasFlag(OverrideType.New) && Node.BaseNode != null)
            {
                if (Index != NodeIndex.Empty)
                ((IAssetObjectNode)Node).ResetOverrideRecursively(Index);
                else
                    Node.ResetOverrideRecursively();
            }
        }

        protected override void ApplyUndo(object oldValue, object newValue, ContentChangeType type, bool isUndo)
        {
            var memberNode = Node as IAssetMemberNode;
            var objectNode = Node as IAssetObjectNode;
            switch (type)
            {
                case ContentChangeType.ValueChange:
                    if (memberNode == null) throw new InvalidOperationException($"Expecting an {nameof(IAssetMemberNode)} when the change type is {nameof(ContentChangeType.ValueChange)}");
                    memberNode.Update(oldValue);
                    break;
                case ContentChangeType.CollectionUpdate:
                    if (objectNode == null) throw new InvalidOperationException($"Expecting an {nameof(IAssetObjectNode)} when the change type is {nameof(ContentChangeType.CollectionUpdate)}");
                    objectNode.Update(oldValue, Index);
                    break;
                case ContentChangeType.CollectionAdd:
                    if (objectNode == null) throw new InvalidOperationException($"Expecting an {nameof(IAssetObjectNode)} when the change type is {nameof(ContentChangeType.CollectionAdd)}");
                    // Some Add might have an empty index. In this case we need to retrieve the index from the content to pass it to remove.
                    // TODO: this way of fetching the index is not robust at all! what if there's the same item twice in the collection?
                    var index = !Index.IsEmpty ? Index : objectNode.Indices.First(x => Equals(Node.Retrieve(x), newValue));
                    // When undoing (an add), we don't want to track the item as deleted. But when redoing (a remove) we do!
                    if (isUndo)
                        objectNode.RemoveAndDiscard(newValue, index, itemId);
                    else
                        objectNode.Remove(newValue, index);

                    break;
                case ContentChangeType.CollectionRemove:
                    if (objectNode == null) throw new InvalidOperationException($"Expecting an {nameof(IAssetObjectNode)} when the change type is {nameof(ContentChangeType.CollectionRemove)}");
                    if (!Index.IsEmpty)
                        objectNode.Restore(oldValue, Index, itemId);
                    else
                        objectNode.Restore(oldValue, itemId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
