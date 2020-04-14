// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters
{
    public class AssetNodePresenterFactory : NodePresenterFactory
    {
        public AssetNodePresenterFactory([NotNull] INodeBuilder nodeBuilder, [NotNull] IReadOnlyCollection<INodePresenterCommand> availableCommands, [NotNull] IReadOnlyCollection<INodePresenterUpdater> availableUpdaters)
            : base(nodeBuilder, availableCommands, availableUpdaters)
        {
        }

        [NotNull]
        public AssetVirtualNodePresenter CreateVirtualNodePresenter([NotNull] INodePresenter parent, string name, Type type, int? order, [NotNull] Func<object> getter, Action<object> setter = null, Func<bool> hasBase = null, Func<bool> isInerited = null, Func<bool> isOverridden = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var node = new AssetVirtualNodePresenter(this, parent.PropertyProvider, parent, name, type, order, getter, setter, hasBase, isInerited, isOverridden);
            node.ChangeParent(parent);
            RunUpdaters(node);
            FinalizeTree(node.Root);
            return node;
        }

        protected override IInitializingNodePresenter CreateRootPresenter(IPropertyProviderViewModel propertyProvider, IObjectNode rootNode)
        {
            var assetNode = rootNode as IAssetObjectNode;
            if (assetNode == null) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
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
            var assetNode = member as IAssetMemberNode;
            if (assetNode == null) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
            return new AssetMemberNodePresenter(this, propertyProvider, parentPresenter, assetNode);
        }

        protected override IInitializingNodePresenter CreateItem(IPropertyProviderViewModel propertyProvider, INodePresenter containerPresenter, IObjectNode containerNode, NodeIndex index)
        {
            var assetNode = containerNode as IAssetObjectNode;
            if (assetNode == null) throw new ArgumentException($"Expected an {nameof(IAssetMemberNode)}.");
            return new AssetItemNodePresenter(this, propertyProvider, containerPresenter, assetNode, index);
        }
    }
}
