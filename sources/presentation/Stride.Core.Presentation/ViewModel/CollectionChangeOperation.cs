// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Presentation.ViewModel
{
    public class CollectionChangeOperation : DirtyingOperation
    {
        private readonly int index;
        private IList list;
        private IReadOnlyCollection<object> items;

        private CollectionChangeOperation([NotNull] IList list, NotifyCollectionChangedAction actionToUndo, [NotNull] IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (actionToUndo == NotifyCollectionChangedAction.Reset) throw new ArgumentException("Reset is not supported by the undo stack.");
            ActionToUndo = actionToUndo;
            this.list = list;
        }

        public CollectionChangeOperation([NotNull] IList list, NotifyCollectionChangedAction actionToUndo, [NotNull] IReadOnlyCollection<object> items, int index, [NotNull] IEnumerable<IDirtiable> dirtiables)
            : this(list, actionToUndo, dirtiables)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            this.items = items;
            this.index = index;
        }

        public CollectionChangeOperation([NotNull] IList list, [NotNull] NotifyCollectionChangedEventArgs args, [NotNull] IEnumerable<IDirtiable> dirtiables)
            : this(list, args.Action, dirtiables)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    items = args.NewItems.Cast<object>().ToArray();
                    index = args.NewStartingIndex;
                    break;
                case NotifyCollectionChangedAction.Move:
                    // Intentionally ignored, move in collection are not tracked
                    return;
                case NotifyCollectionChangedAction.Remove:
                    items = args.OldItems.Cast<object>().ToArray();
                    index = args.OldStartingIndex;
                    break;
                case NotifyCollectionChangedAction.Replace:
                    items = args.OldItems.Cast<object>().ToArray();
                    index = args.OldStartingIndex;
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset is not supported by the undo stack.");
                default:
                    items = new object[] { };
                    index = -1;
                    break;
            }
        }

        public NotifyCollectionChangedAction ActionToUndo { get; private set; }

        public int ItemCount => items?.Count ?? 0;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{nameof(CollectionChangeOperation)}: {ActionToUndo}}}";
        }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            list = null;
            items = null;
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            int i = 0;
            switch (ActionToUndo)
            {
                case NotifyCollectionChangedAction.Add:
                    ActionToUndo = NotifyCollectionChangedAction.Remove;
                    for (i = 0; i < items.Count; ++i)
                    {
                        list.RemoveAt(index);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ActionToUndo = NotifyCollectionChangedAction.Add;
                    foreach (var item in items)
                    {
                        list.Insert(index + i, item);
                        ++i;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    var replacedItems = new List<object>();
                    foreach (var item in items)
                    {
                        replacedItems.Add(list[index + i]);
                        list[index + i] = item;
                        ++i;
                    }
                    items = replacedItems;
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotSupportedException("Move is not supported by the undo stack.");
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset is not supported by the undo stack.");
            }
        }

        /// <inheritdoc/>
        protected override void Redo()
        {
            // Once we have un-done, the previous value is updated so Redo is just Undoing the Undo
            Undo();
        }
    }
}
