// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum
{
    public class ContentValueChangeOperation : DirtyingOperation, IMergeableOperation
    {
        protected readonly IGraphNode Node;
        protected NodeIndex Index;
        protected object PreviousValue;
        protected object NewValue;

        public ContentValueChangeOperation(IGraphNode node, ContentChangeType changeType, NodeIndex index, object previousValue, object newValue, IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            Node = node;
            ChangeType = changeType;
            PreviousValue = previousValue;
            NewValue = newValue;
            Index = index;
        }

        public ContentChangeType ChangeType { get; protected set; }

        /// <inheritdoc/>
        // note: to allow adding or removing null values in collection, we don't check equality of previous and new values
        public override bool HasEffect => (ChangeType != ContentChangeType.ValueChange && ChangeType != ContentChangeType.CollectionUpdate) || !Equals(PreviousValue, NewValue);

        /// <inheritdoc/>
        public virtual bool CanMerge(IMergeableOperation otherOperation)
        {
            var operation = otherOperation as ContentValueChangeOperation;
            if (operation == null)
                return false;

            if (ChangeType != ContentChangeType.ValueChange && ChangeType != ContentChangeType.CollectionUpdate)
                return false;

            if (Node != operation.Node)
                return false;

            if (!Equals(Index, operation.Index))
                return false;

            if (ChangeType != operation.ChangeType)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public virtual void Merge(Operation otherOperation)
        {
            var operation = (ContentValueChangeOperation)otherOperation;
            NewValue = operation.NewValue;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsFrozen)
            {
                return $"{{{nameof(ContentValueChangeOperation)}: (Frozen)}}";
            }

            try
            {
                var sb = new StringBuilder($"{{{nameof(ContentValueChangeOperation)}: {(Node as IMemberNode)?.Name ?? Node.Type.Name}");
                if (!Index.IsEmpty)
                {
                    sb.Append($"[{Index.Value}]");
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
        protected override void FreezeContent()
        {
            Index = NodeIndex.Empty;
            PreviousValue = null;
            NewValue = null;
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            ApplyUndo(PreviousValue, NewValue, ChangeType, true);
        }

        /// <inheritdoc/>
        protected override void Redo()
        {
            ContentChangeType changeType = ChangeType;
            switch (ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    changeType = ContentChangeType.CollectionRemove;
                    break;
                case ContentChangeType.CollectionRemove:
                    changeType = ContentChangeType.CollectionAdd;
                    break;
            }
            ApplyUndo(NewValue, PreviousValue, changeType, false);
        }

        protected virtual void ApplyUndo(object oldValue, object newValue, ContentChangeType type, bool isUndo)
        {
            var memberNode = Node as IMemberNode;
            var objectNode = Node as IObjectNode;
            switch (type)
            {
                case ContentChangeType.ValueChange:
                    memberNode.Update(oldValue);
                    break;
                case ContentChangeType.CollectionUpdate:
                    objectNode.Update(oldValue, Index);
                    break;
                case ContentChangeType.CollectionAdd:
                    // Some Add might have an empty index. In this case we need to retrieve the index from the content to pass it to remove.
                    var index = !Index.IsEmpty ? Index : objectNode.Indices.First(x => Equals(Node.Retrieve(x), newValue));
                    objectNode.Remove(newValue, index);
                    break;
                case ContentChangeType.CollectionRemove:
                    // Some Add might have an empty index.
                    if (!Index.IsEmpty)
                        objectNode.Add(oldValue, Index);
                    else
                        objectNode.Add(oldValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
