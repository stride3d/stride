// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Quantum;

public class BoxedNode : ObjectNode
{
    private GraphNodeBase? boxedStructureOwner;
    private NodeIndex boxedStructureOwnerIndex;

    public BoxedNode(INodeBuilder nodeBuilder, object value, Guid guid, ITypeDescriptor descriptor)
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
        if (index.TryGetValue(out var indexValue))
        {
            if (Descriptor is CollectionDescriptor collectionDescriptor)
            {
                collectionDescriptor.SetValue(Value, indexValue, newValue);
            }
            else if (Descriptor is DictionaryDescriptor dictionaryDescriptor)
            {
                dictionaryDescriptor.SetValue(Value, index, newValue);
            }
            else
            {
                throw new NotSupportedException("Unable to set the node value, the collection is unsupported");
            }
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
