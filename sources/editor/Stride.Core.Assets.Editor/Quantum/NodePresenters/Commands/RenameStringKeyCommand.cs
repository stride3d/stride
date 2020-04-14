// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class RenameStringKeyCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "RenameStringKey";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.AlwaysCombine;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are in a dictionary...
            var collectionNode = (nodePresenter as ItemNodePresenter)?.OwnerCollection;
            var dictionaryDescriptor = collectionNode?.Descriptor as DictionaryDescriptor;
            if (dictionaryDescriptor == null)
                return false;

            // ... that is not read-only...
            var memberCollection = (collectionNode as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                   ?? collectionNode.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            if (memberCollection?.ReadOnly == true)
                return false;

            // ... and is indexed by strings...
            if (dictionaryDescriptor.KeyType != typeof(string))
                return false;

            // ... and supports remove and insert
            // TODO: ... and can remove items - we don't have this information yet in DictionaryDescriptor
            return true;
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var currentValue = nodePresenter.Value;
            var collectionNode = ((ItemNodePresenter)nodePresenter).OwnerCollection;
            collectionNode.RemoveItem(nodePresenter.Value, nodePresenter.Index);
            var newName = AddPrimitiveKeyCommand.GenerateStringKey(collectionNode.Value, collectionNode.Descriptor, (string)parameter);
            collectionNode.AddItem(currentValue, newName);
        }
    }
}
