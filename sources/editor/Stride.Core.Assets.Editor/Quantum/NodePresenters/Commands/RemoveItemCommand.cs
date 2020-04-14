// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class RemoveItemCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "RemoveItem";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are in a collection or dictionary...
            var collectionNode = (nodePresenter as ItemNodePresenter)?.OwnerCollection;
            var collectionDescriptor = collectionNode?.Descriptor as CollectionDescriptor;
            var dictionaryDescriptor = collectionNode?.Descriptor as DictionaryDescriptor;
            if (collectionDescriptor == null && dictionaryDescriptor == null)
                return false;

            // ... that is not read-only...
            var memberCollection = (collectionNode as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                   ?? collectionNode.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            if (memberCollection?.ReadOnly == true)
                return false;

            // ... and supports remove...
            if (collectionDescriptor != null)
            {
                var elementType = collectionDescriptor.ElementType;
                // We also add the same conditions that for AddNewItem
                return collectionDescriptor.HasRemoveAt && AddNewItemCommand.CanAdd(elementType);
            }
            // TODO: add a HasRemove in the dictionary descriptor and test it!
            return true;
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var collectionNode = ((ItemNodePresenter)nodePresenter).OwnerCollection;
            collectionNode.RemoveItem(nodePresenter.Value, nodePresenter.Index);
        }
    }
}
