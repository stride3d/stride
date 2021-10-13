// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum
{
    public class BoxedNode : ObjectNode
    {
        private GraphNodeBase boxedStructureOwner;
        private NodeIndex boxedStructureOwnerIndex;

        public BoxedNode([NotNull] INodeBuilder nodeBuilder, object value, Guid guid, [NotNull] ITypeDescriptor descriptor)
            : base(nodeBuilder, value, guid, descriptor, null)
        {
        }

        protected internal override void UpdateFromMember(object newValue, NodeIndex index)
        {
            Update(newValue, index, true);
        }

        internal void UpdateFromOwner(object newValue)
        {
            Update(newValue, NodeIndex.Empty, false);
        }

        internal void SetOwnerContent(IGraphNode ownerNode, NodeIndex index)
        {
            boxedStructureOwner = (GraphNodeBase)ownerNode;
            boxedStructureOwnerIndex = index;
        }

        private void Update(object newValue, NodeIndex index, bool updateStructureOwner)
        {
            if (!index.IsEmpty)
            {
                if (Descriptor is ListDescriptor listDescriptor)
                {
                    listDescriptor.SetValue(Value, index.Int, newValue);
                }
                else if (Descriptor is DictionaryDescriptor dictionaryDescriptor)
                {
                    dictionaryDescriptor.SetValue(Value, index, newValue);
                }
                else if (Descriptor is SetDescriptor setDescriptor)
                {
                    setDescriptor.SetValue(Value, index.Value, newValue);
                }
                else if (Descriptor is CollectionDescriptor collectionDescriptor)
                {
                    collectionDescriptor.SetValue(Value, index.Int, newValue);
                }
                else
                    throw new NotSupportedException("Unable to set the node value, the collection is unsupported");
            }
            else
            {
                SetValue(newValue);
                if (updateStructureOwner)
                {
                    boxedStructureOwner?.UpdateFromMember(newValue, boxedStructureOwnerIndex);
                }
            }
        }

        public override string ToString()
        {
            return $"{{Node: Boxed {Type.Name} = [{Value}]}}";
        }
    }
}
