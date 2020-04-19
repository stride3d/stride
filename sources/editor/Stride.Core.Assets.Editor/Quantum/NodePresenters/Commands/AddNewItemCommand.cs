// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class AddNewItemCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "AddNewItem";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are in a collection...
            var collectionDescriptor = nodePresenter.Descriptor as CollectionDescriptor;
            if (collectionDescriptor == null)
                return false;

            // ... that is not read-only...
            var memberCollection = (nodePresenter as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                   ?? nodePresenter.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            if (memberCollection?.ReadOnly == true)
                return false;

            // ... supports add...
            if (!collectionDescriptor.HasAdd)
                return false;

            // ... and can construct element
            var elementType = collectionDescriptor.ElementType;
            return CanConstruct(elementType) || elementType.IsAbstract || elementType.IsNullable() || IsReferenceType(elementType);
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var assetNodePresenter = nodePresenter as IAssetNodePresenter;
            var collectionDescriptor = (CollectionDescriptor)nodePresenter.Descriptor;

            object itemToAdd;

            // First, check if parameter is an AbstractNodeEntry
            var abstractNodeEntry = parameter as AbstractNodeEntry;
            if (abstractNodeEntry != null)
            {
                itemToAdd = abstractNodeEntry.GenerateValue(null);
            }
            // Otherwise, assume it's an object
            else
            {
                var elementType = collectionDescriptor.ElementType;
                itemToAdd = parameter;
                if (itemToAdd == null)
                {
                    var instance = ObjectFactoryRegistry.NewInstance(elementType);
                    if (!IsReferenceType(elementType) && (assetNodePresenter == null || !assetNodePresenter.IsObjectReference(instance)))
                        itemToAdd = instance;
                }
            }

            nodePresenter.AddItem(itemToAdd);
        }

        public static bool CanAdd(Type elementType) => CanConstruct(elementType) || elementType.IsAbstract || elementType.IsNullable() || IsReferenceType(elementType);

        public static bool CanConstruct(Type elementType) => !elementType.IsClass || elementType.GetConstructor(Type.EmptyTypes) != null || elementType == typeof(string);

        public static bool IsReferenceType(Type elementType) => AssetRegistry.IsContentType(elementType) || typeof(AssetReference).IsAssignableFrom(elementType);
    }
}
