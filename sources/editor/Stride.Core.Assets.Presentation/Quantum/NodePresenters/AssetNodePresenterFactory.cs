// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Presentation.Quantum.NodePresenters;

public class AssetNodePresenterFactory : NodePresenterFactory
{
    public AssetNodePresenterFactory(INodeBuilder nodeBuilder, IReadOnlyCollection<INodePresenterCommand> availableCommands, IReadOnlyCollection<INodePresenterUpdater> availableUpdaters)
        : base(nodeBuilder, availableCommands, availableUpdaters)
    {
    }

    public AssetVirtualNodePresenter CreateVirtualNodePresenter(INodePresenter parent, string name, Type type, int? order, Func<object> getter, Action<object>? setter = null, Func<bool>? hasBase = null, Func<bool>? isInherited = null, Func<bool>? isOverridden = null)
    {
        var node = new AssetVirtualNodePresenter(this, parent.PropertyProvider, parent, name, type, order, getter, setter, hasBase, isInherited, isOverridden);
        node.ChangeParent(parent);
        RunUpdaters(node);
        FinalizeTree(node.Root);
        return node;
    }

    protected override IInitializingNodePresenter CreateRootPresenter(IPropertyProviderViewModel propertyProvider, IObjectNode rootNode)
    {
        if (rootNode is not IAssetObjectNode assetNode) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
        return new AssetRootNodePresenter(this, propertyProvider, assetNode);
    }

    protected override bool ShouldCreateMemberPresenter(INodePresenter parent, IMemberNode member, IPropertyProviderViewModel propertyProvider)
    {
        // Don't construct members of object references
        if (((IAssetNodePresenter)parent).IsObjectReference(parent.Value))
            return false;

        return base.ShouldCreateMemberPresenter(parent, member, propertyProvider);
    }

    protected override IInitializingNodePresenter CreateMember(IPropertyProviderViewModel propertyProvider, INodePresenter parentPresenter, IMemberNode member)
    {
        if (member is not IAssetMemberNode assetNode) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
        return new AssetMemberNodePresenter(this, propertyProvider, parentPresenter, assetNode);
    }

    protected override IInitializingNodePresenter CreateItem(IPropertyProviderViewModel propertyProvider, INodePresenter containerPresenter, IObjectNode containerNode, NodeIndex index)
    {
        if (containerNode is not IAssetObjectNode assetNode) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
        return new AssetItemNodePresenter(this, propertyProvider, containerPresenter, assetNode, index);
    }
}
