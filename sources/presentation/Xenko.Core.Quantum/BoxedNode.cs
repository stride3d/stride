// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;

namespace Xenko.Core.Quantum
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
                var collectionDescriptor = Descriptor as CollectionDescriptor;
                var dictionaryDescriptor = Descriptor as DictionaryDescriptor;
                if (collectionDescriptor != null)
                {
                    collectionDescriptor.SetValue(Value, index.Int, newValue);
                }
                else if (dictionaryDescriptor != null)
                {
                    dictionaryDescriptor.SetValue(Value, index, newValue);
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
