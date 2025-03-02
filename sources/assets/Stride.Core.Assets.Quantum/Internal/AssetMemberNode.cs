// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Assets.Quantum.Internal;

internal class AssetMemberNode : MemberNode, IAssetMemberNode, IAssetNodeInternal
{
    private AssetPropertyGraph propertyGraph;
    private readonly Dictionary<string, IGraphNode> contents = [];

    private OverrideType contentOverride;

    public AssetMemberNode(INodeBuilder nodeBuilder, Guid guid, IObjectNode parent, IMemberDescriptor memberDescriptor, IReference? reference)
        : base(nodeBuilder, guid, parent, memberDescriptor, reference)
    {
        ValueChanged += ContentChanged;
        IsNonIdentifiableCollectionContent = MemberDescriptor.GetCustomAttributes<NonIdentifiableCollectionItemsAttribute>(true)?.Any() ?? false;
        CanOverride = MemberDescriptor.GetCustomAttributes<NonOverridableAttribute>(true)?.Any() != true;
    }

    public bool IsNonIdentifiableCollectionContent { get; }

    public bool CanOverride { get; }

    internal bool ResettingOverride { get; set; }

    public event EventHandler<EventArgs>? OverrideChanging;

    public event EventHandler<EventArgs>? OverrideChanged;

    public AssetPropertyGraph PropertyGraph { get => propertyGraph; internal set => propertyGraph = value ?? throw new ArgumentNullException(nameof(value)); }

    public IGraphNode BaseNode { get; private set; }

    public new IAssetObjectNode Parent => (IAssetObjectNode)base.Parent;

    public new IAssetObjectNode? Target => (IAssetObjectNode?)base.Target;

    public void SetContent(string key, IGraphNode node)
    {
        contents[key] = node;
    }

    public IGraphNode? GetContent(string key)
    {
        contents.TryGetValue(key, out var node);
        return node;
    }

    public void OverrideContent(bool isOverridden)
    {
        if (CanOverride)
        {
            OverrideChanging?.Invoke(this, EventArgs.Empty);
            contentOverride = isOverridden ? OverrideType.New : OverrideType.Base;
            OverrideChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public void ResetOverrideRecursively()
    {
        PropertyGraph.ResetAllOverridesRecursively(this, NodeIndex.Empty);
    }

    private void ContentChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        var node = (AssetMemberNode)e.Member;
        if (node.IsNonIdentifiableCollectionContent)
            return;

        // Make sure that we have item ids everywhere we're supposed to.
        AssetCollectionItemIdHelper.GenerateMissingItemIds(e.Member.Retrieve());

        // Don't update override if propagation from base is disabled.
        if (PropertyGraph?.Container is null || !PropertyGraph.Container.PropagateChangesFromBase)
            return;

        // Mark it as New if it does not come from the base
        if (BaseNode is not null && !PropertyGraph.UpdatingPropertyFromBase && !ResettingOverride)
        {
            OverrideContent(!ResettingOverride);
        }
    }

    internal void SetContentOverride(OverrideType overrideType)
    {
        if (CanOverride)
        {
            contentOverride = overrideType;
        }
    }

    public OverrideType GetContentOverride()
    {
        return contentOverride;
    }

    public bool IsContentOverridden()
    {
        return (contentOverride & OverrideType.New) == OverrideType.New;
    }

    public bool IsContentInherited()
    {
        return BaseNode is not null && !IsContentOverridden();
    }

    bool IAssetNodeInternal.ResettingOverride { get; set; }

    void IAssetNodeInternal.SetPropertyGraph(AssetPropertyGraph assetPropertyGraph)
    {
        ArgumentNullException.ThrowIfNull(assetPropertyGraph);
        PropertyGraph = assetPropertyGraph;
    }

    void IAssetNodeInternal.SetBaseNode(IGraphNode node)
    {
        BaseNode = node;
    }
}
