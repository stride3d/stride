// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class AddPrimitiveKeyCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "AddPrimitiveKey";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are in a dictionary...
            var dictionaryDescriptor = nodePresenter.Descriptor as DictionaryDescriptor;
            if (dictionaryDescriptor == null)
                return false;

            // ... that is not read-only...
            var memberCollection = (nodePresenter as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                   ?? nodePresenter.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            if (memberCollection?.ReadOnly == true)
                return false;

            // ... can construct key type...
            if (!AddNewItemCommand.CanConstruct(dictionaryDescriptor.KeyType))
                return false;

            // ... and can construct value type
            var elementType = dictionaryDescriptor.ValueType;
            return AddNewItemCommand.CanAdd(elementType);
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var assetNodePresenter = nodePresenter as IAssetNodePresenter;
            var dictionaryDescriptor = (DictionaryDescriptor)nodePresenter.Descriptor;
            var value = nodePresenter.Value;
            var newKey = dictionaryDescriptor.KeyType != typeof(string) ? new NodeIndex(Activator.CreateInstance(dictionaryDescriptor.KeyType)) : GenerateStringKey(value, dictionaryDescriptor, parameter as string);

            var newItem = dictionaryDescriptor.ValueType.Default();
            var instance = CreateInstance(dictionaryDescriptor.ValueType);
            if (!AddNewItemCommand.IsReferenceType(dictionaryDescriptor.ValueType) && (assetNodePresenter == null || !assetNodePresenter.IsObjectReference(instance)))
                newItem = instance;

            nodePresenter.AddItem(newItem, newKey);
        }

        /// <summary>
        /// Creates an instance of the specified type using that type's default constructor.
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <returns>A reference to the newly created object.</returns>
        /// <seealso cref="Activator.CreateInstance(Type)"/>
        /// <exception cref="ArgumentNullException">type is null.</exception>
        private static object CreateInstance(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            // abstract type cannot be instantiated
            if (type.IsAbstract)
                return null;

            // string is a special case
            if (type == typeof(string))
                return string.Empty;

            // note:
            //      Type not having a public parameterless constructor will throw a MissingMethodException at this point.
            //      This is intended as YAML serialization requires this constructor.
            return ObjectFactoryRegistry.NewInstance(type);
        }

        internal static NodeIndex GenerateStringKey(object dictionary, ITypeDescriptor descriptor, string baseValue)
        {
            // TODO: use a dialog service and popup a message when the given key is invalid
            const string defaultKey = "Key";

            if (string.IsNullOrWhiteSpace(baseValue))
                baseValue = defaultKey;

            var i = 1;
            string baseName = baseValue;
            var dictionaryDescriptor = (DictionaryDescriptor)descriptor;
            while (dictionaryDescriptor.ContainsKey(dictionary, baseValue))
            {
                baseValue = baseName + " " + ++i;
            }

            return new NodeIndex(baseValue);
        }
    }
}
