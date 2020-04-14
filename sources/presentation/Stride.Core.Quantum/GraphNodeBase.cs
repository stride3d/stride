// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.TypeConverters;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A base abstract implementation of the <see cref="IGraphNode"/> interface.
    /// </summary>
    public abstract class GraphNodeBase : IInitializingGraphNode
    {
        protected readonly NodeContainer NodeContainer;

        protected GraphNodeBase(NodeContainer nodeContainer, Guid guid, [NotNull] ITypeDescriptor descriptor)
        {
            if (guid == Guid.Empty) throw new ArgumentException(@"The guid must be different from Guid.Empty.", nameof(guid));
            NodeContainer = nodeContainer;
            Guid = guid;
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public Type Type => Descriptor.Type;

        /// <inheritdoc/>
        public ITypeDescriptor Descriptor { get; }

        /// <inheritdoc/>
        public abstract bool IsReference { get; }

        /// <inheritdoc/>
        public Guid Guid { get; }

        /// <inheritdoc/>
        protected abstract object Value { get; }

        /// <summary>
        /// Gets whether this node has been sealed.
        /// </summary>
        protected bool IsSealed { get; private set; }

        /// <inheritdoc/>
        public object Retrieve() => Retrieve(NodeIndex.Empty);

        /// <inheritdoc/>
        public virtual object Retrieve(NodeIndex index)
        {
            return Content.Retrieve(Value, index, Descriptor);
        }

        /// <summary>
        /// Updates this content from one of its member.
        /// </summary>
        /// <param name="newValue">The new value for this content.</param>
        /// <param name="index">new index of the value to update.</param>
        /// <remarks>
        /// This method is intended to update a boxed content when one of its member changes.
        /// It allows to properly update boxed structs.
        /// </remarks>
        protected internal abstract void UpdateFromMember(object newValue, NodeIndex index);

        public static IEnumerable<NodeIndex> GetIndices([NotNull] IGraphNode node)
        {
            if (node.Descriptor is CollectionDescriptor collectionDescriptor)
            {
                return Enumerable.Range(0, collectionDescriptor.GetCollectionCount(node.Retrieve())).Select(x => new NodeIndex(x));
            }
            var dictionaryDescriptor = node.Descriptor as DictionaryDescriptor;
            return dictionaryDescriptor?.GetKeys(node.Retrieve()).Cast<object>().Select(x => new NodeIndex(x));
        }

        /// <summary>
        /// Seal the node, indicating its construction is finished and that no more children or commands will be added.
        /// </summary>
        public void Seal()
        {
            IsSealed = true;
        }

        protected static object ConvertValue(object value, Type type)
        {
            if (value == null)
                return null;
            object convertedValue;
            if (!TypeConverterHelper.TryConvert(value, type, out convertedValue))
                throw new InvalidOperationException("Can not convert value to the required type");
            return convertedValue;
        }
    }
}
