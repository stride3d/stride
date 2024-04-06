// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using System.Diagnostics.CodeAnalysis;
using NotNullAttribute = Stride.Core.Annotations.NotNullAttribute;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public interface INodePresenter : IDisposable
    {
        [NotNull]
        INodePresenter this[string childName] { get; }

        string DisplayName { get; set; }

        string Name { get; }

        [NotNull]
        INodePresenter Root { get; }

        [CanBeNull]
        INodePresenter Parent { get; }

        IReadOnlyList<INodePresenter> Children { get; }

        List<INodePresenterCommand> Commands { get; }

        PropertyContainerClass AttachedProperties { get; }

        [NotNull]
        Type Type { get; }

        bool IsEnumerable { get; }

        bool IsReadOnly { get; set; }

        bool IsVisible { get; set; }

        NodeIndex Index { get; }

        ITypeDescriptor Descriptor { get; }

        int? Order { get; set; }

        object Value { get; }

        string CombineKey { get; set; }

        IPropertyProviderViewModel PropertyProvider { get; }

        INodePresenterFactory Factory { get; }

        event EventHandler<ValueChangingEventArgs> ValueChanging;

        event EventHandler<ValueChangedEventArgs> ValueChanged;

        void UpdateValue(object newValue);

        void AddItem(object value);

        void AddItem(object value, NodeIndex index);

        void RemoveItem(object value, NodeIndex index);

        // TODO: this should probably be removed, UpdateValue should be called on the corresponding child node presenter itself

        NodeAccessor GetNodeAccessor();

        /// <summary>
        /// Adds a dependency to the given node.
        /// </summary>
        /// <param name="node">The node that should be a dependency of this node.</param>
        /// <param name="refreshOnNestedNodeChanges">If true, this node will also be refreshed when one of the child node of the dependency node changes.</param>
        /// <remarks>A node that is a dependency to this node will trigger a refresh of this node each time its value is modified (or the value of one of its parent).</remarks>
        void AddDependency(INodePresenter node, bool refreshOnNestedNodeChanges);

        void ChangeParent([NotNull] INodePresenter newParent);

        void Rename(string newName, bool overwriteCombineKey = true);

        [CanBeNull]
        INodePresenter TryGetChild(string childName);

        /// <summary>
        /// Returns true if this <see cref="Descriptor"/> or <see cref="Value"/> is an array, dictionary or collection
        /// </summary>
        /// <param name="hasAdd">Whether this collection supports adding items</param>
        /// <param name="itemType">What the collection is composed of, for dictionaries this would be the value type</param>
        /// <param name="descriptor"><see cref="Descriptor"/> or <see cref="Value"/> if <see cref="Descriptor"/> is not a collection </param>
        public bool ValueIsAnyCollection(out bool hasAdd, [MaybeNullWhen(false)] out Type itemType, [MaybeNullWhen(false)] out ITypeDescriptor descriptor)
        {
            if (DescriptorIsAnyCollection(Descriptor, out hasAdd, out itemType))
            {
                descriptor = Descriptor;
                return true;
            }
            if (Value is { } val 
                && TypeDescriptorFactory.Default.Find(val.GetType()) is { } valueDescriptor 
                && DescriptorIsAnyCollection(valueDescriptor, out hasAdd, out itemType))
            {
                descriptor = valueDescriptor;
                return true;
            }
            descriptor = null;
            return false;
        }

        private static bool DescriptorIsAnyCollection(ITypeDescriptor descriptor, out bool hasAdd, [MaybeNullWhen(false)] out Type itemType)
        {
            if (descriptor is DictionaryDescriptor dd)
            {
                itemType = dd.ValueType;
                hasAdd = true;
                return true;
            }
            else if (descriptor is CollectionDescriptor cd)
            {
                itemType = cd.ElementType;
                hasAdd = cd.HasAdd;
                return true;
            }
            else if (descriptor is ArrayDescriptor arr)
            {
                itemType = arr.ElementType;
                hasAdd = false;
                return true;
            }
            itemType = null;
            hasAdd = false;
            return false;
        }
    }
}
