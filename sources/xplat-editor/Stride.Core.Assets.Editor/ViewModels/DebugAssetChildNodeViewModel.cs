// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public class DebugAssetChildNodeViewModel : DebugAssetNodeViewModel
{
    public const string LinkRoot = "Root";
    public const string LinkChild = "Child";
    public const string LinkRef = "Ref";

    private readonly HashSet<IGraphNode> registeredNodes;

    public DebugAssetChildNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode? node, HashSet<IGraphNode> registeredNodes)
        : this(serviceProvider, node, NodeIndex.Empty, null, LinkRoot, registeredNodes)
    {
    }

    private DebugAssetChildNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode? node, NodeIndex index, ItemId? itemId, string linkFromParent, HashSet<IGraphNode> registeredNodes)
        : base(serviceProvider, node)
    {
        this.registeredNodes = registeredNodes;
        LinkFromParent = linkFromParent;
        Index = index;
        ItemId = itemId;
        Registered = node == null || registeredNodes.Contains(node);
        var assetNode = (IAssetNode?)node;
        var baseNode = assetNode?.BaseNode;
        if (baseNode != null)
            Base = new DebugAssetBaseNodeViewModel(serviceProvider, baseNode);
    }

    public NodeIndex Index { get; }

    public ItemId? ItemId { get; }

    public string LinkFromParent { get; }

    public bool Registered { get; }

    public DebugAssetBaseNodeViewModel? Base { get; }

    public List<DebugAssetNodeViewModel> Children => UpdateChildren();

    protected List<DebugAssetNodeViewModel> UpdateChildren()
    {
        var list = new List<DebugAssetNodeViewModel>();
        if (Node != null && Registered)
        {
            if (Node is IObjectNode objNode)
            {
                foreach (var child in objNode.Members)
                {
                    list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, child, NodeIndex.Empty, null, LinkChild, registeredNodes));
                }
            }
            if (Node.IsReference)
            {
                var objReference = (Node as IMemberNode)?.TargetReference;
                if (objReference != null)
                {
                    list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, objReference.TargetNode, objReference.Index, null, LinkRef, registeredNodes));
                }
                else
                {
                    CollectionItemIdHelper.TryGetCollectionItemIds(Node.Retrieve(), out var itemIds);
                    foreach (var reference in ((IObjectNode)Node).ItemReferences)
                    {
                        ItemId? itemId = null;
                        if (itemIds != null && itemIds.TryGet(reference.Index.Value, out var retrievedItemId))
                            itemId = retrievedItemId;
                        list.Add(new DebugAssetChildNodeViewModel(ServiceProvider, reference.TargetNode, reference.Index, itemId, LinkRef, registeredNodes));

                    }
                }
            }
        }
        return list;
    }
}
