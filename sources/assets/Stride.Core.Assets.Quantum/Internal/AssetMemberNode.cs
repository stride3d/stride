// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Assets.Quantum.Internal
{
    internal class AssetMemberNode : MemberNode, IAssetMemberNode, IAssetNodeInternal
    {
        private AssetPropertyGraph propertyGraph;
        private readonly Dictionary<string, IGraphNode> contents = new Dictionary<string, IGraphNode>();

        private OverrideType contentOverride;

        public AssetMemberNode([NotNull] INodeBuilder nodeBuilder, Guid guid, [NotNull] IObjectNode parent, [NotNull] IMemberDescriptor memberDescriptor, IReference reference)
            : base(nodeBuilder, guid, parent, memberDescriptor, reference)
        {
            ValueChanged += ContentChanged;
            IsNonIdentifiableCollectionContent = MemberDescriptor.GetCustomAttributes<NonIdentifiableCollectionItemsAttribute>(true)?.Any() ?? false;
            CanOverride = MemberDescriptor.GetCustomAttributes<NonOverridableAttribute>(true)?.Any() != true;
        }

        public bool IsNonIdentifiableCollectionContent { get; }

        public bool CanOverride { get; }

        internal bool ResettingOverride { get; set; }

        public event EventHandler<EventArgs> OverrideChanging;

        public event EventHandler<EventArgs> OverrideChanged;

        public AssetPropertyGraph PropertyGraph { get => propertyGraph; internal set => propertyGraph = value ?? throw new ArgumentNullException(nameof(value)); }

        public IGraphNode BaseNode { get; private set; }

        public new IAssetObjectNode Parent => (IAssetObjectNode)base.Parent;

        [CanBeNull]
        public new IAssetObjectNode Target => (IAssetObjectNode)base.Target;

        public void SetContent([NotNull] string key, IGraphNode node)
        {
            contents[key] = node;
        }

        [CanBeNull]
        public IGraphNode GetContent([NotNull] string key)
        {
            contents.TryGetValue(key, out IGraphNode node);
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

        private void ContentChanged(object sender, [NotNull] MemberNodeChangeEventArgs e)
        {
            // Make sure that we have item ids everywhere we're supposed to.
            AssetCollectionItemIdHelper.GenerateMissingItemIds(e.Member.Retrieve());

            var node = (AssetMemberNode)e.Member;
            if (node.IsNonIdentifiableCollectionContent)
                return;

            // Don't update override if propagation from base is disabled.
            if (PropertyGraph?.Container == null || PropertyGraph?.Container?.PropagateChangesFromBase == false)
                return;

            // Mark it as New if it does not come from the base
            if (BaseNode != null && !PropertyGraph.UpdatingPropertyFromBase && !ResettingOverride)
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
            return BaseNode != null && !IsContentOverridden();
        }

        bool IAssetNodeInternal.ResettingOverride { get; set; }

        void IAssetNodeInternal.SetPropertyGraph(AssetPropertyGraph assetPropertyGraph)
        {
            if (assetPropertyGraph == null) throw new ArgumentNullException(nameof(assetPropertyGraph));
            PropertyGraph = assetPropertyGraph;
        }

        void IAssetNodeInternal.SetBaseNode(IGraphNode node)
        {
            BaseNode = node;
        }
    }
}
