// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;

namespace Xenko.Assets.Presentation.ViewModel.Commands
{
    // TODO: this command is very similar to RenameStringKeyCommand - try to factorize them
    // TODO (revision): should use the same mechanism as Reorder Collection actually
    public class MoveItemCommand : SyncNodePresenterCommandBase
    {
        public const string CommandName = "MoveItem";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.AlwaysCombine;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are in a collection...
            var collectionNode = (nodePresenter as ItemNodePresenter)?.OwnerCollection;
            if (collectionNode == null)
                return false;

            // ... that is not read-only...
            var memberCollection = (collectionNode as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            if (memberCollection?.ReadOnly == true)
                return false;

            // ... and support remove and insert
            var collectionDescriptor = collectionNode.Descriptor as CollectionDescriptor;
            return collectionDescriptor?.HasRemoveAt == true && collectionDescriptor.HasInsert;
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var itemNode = (ItemNodePresenter)nodePresenter;
            var collectionNode = itemNode.OwnerCollection;
            var indices = (Tuple<int, int>)parameter;
            var sourceIndex = new NodeIndex(indices.Item1);
            var targetIndex = new NodeIndex(indices.Item2);
            var value = itemNode.Value;
            collectionNode.RemoveItem(value, sourceIndex);
            collectionNode.AddItem(value, targetIndex);
        }
    }
}
