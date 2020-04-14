// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters
{
    public class AssetVirtualNodePresenter : VirtualNodePresenter, IAssetNodePresenter
    {
        private readonly Func<bool> hasBase;
        private readonly Func<bool> isInerited;
        private readonly Func<bool> isOverridden;

        public AssetVirtualNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, string name, Type type, int? order, [NotNull] Func<object> getter, Action<object> setter, Func<bool> hasBase = null, Func<bool> isInerited = null, Func<bool> isOverridden = null)
            : base(factory, propertyProvider, parent, name, type, order, getter, setter)
        {
            this.hasBase = hasBase;
            this.isInerited = isInerited;
            this.isOverridden = isOverridden;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (AssociatedNode.Node != null)
            {
                ((IAssetNode)AssociatedNode.Node).OverrideChanging -= OnOverrideChanging;
                ((IAssetNode)AssociatedNode.Node).OverrideChanged -= OnOverrideChanged;
            }
        }

        public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

        public bool HasBase => hasBase?.Invoke() ?? (AssociatedNode.Node as IAssetNode)?.BaseNode != null;

        public bool IsInherited => isInerited?.Invoke() ?? IsAssociatedNodeInherited();

        public bool IsOverridden => isOverridden?.Invoke() ?? IsAssociatedNodeOverridden();

        public AssetViewModel Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

        public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

        public event EventHandler<EventArgs> OverrideChanging;

        public event EventHandler<EventArgs> OverrideChanged;

        public override void RegisterAssociatedNode(NodeAccessor associatedNodeAccessor)
        {
            base.RegisterAssociatedNode(associatedNodeAccessor);
            ((IAssetNode)AssociatedNode.Node).OverrideChanging += OnOverrideChanging;
            ((IAssetNode)AssociatedNode.Node).OverrideChanged += OnOverrideChanged;
        }

        public bool IsObjectReference(object value)
        {
            return AssociatedNode.Node != null && (Asset?.PropertyGraph?.Definition.IsObjectReference(AssociatedNode, value) ?? false);
        }

        public void ResetOverride()
        {
            // TODO: for now we cannot reset override if we don't have an AssociatedNode. We could provide a delegate via the constructor for custom reset.
            var memberNode = AssociatedNode.Node as IAssetMemberNode;
            memberNode?.ResetOverrideRecursively();

            var objectNode = AssociatedNode.Node as IAssetObjectNode;
            objectNode?.ResetOverrideRecursively(AssociatedNode.Index);
        }

        private bool IsAssociatedNodeInherited()
        {
            var memberNode = AssociatedNode.Node as IAssetMemberNode;
            if (memberNode != null)
            {
                return memberNode.IsContentInherited();
            }
            var objectNode = AssociatedNode.Node as IAssetObjectNode;
            if (objectNode != null)
            {
                return objectNode.IsItemInherited(AssociatedNode.Index);
            }
            return false;
        }

        private bool IsAssociatedNodeOverridden()
        {
            var memberNode = AssociatedNode.Node as IAssetMemberNode;
            if (memberNode != null)
            {
                return memberNode.IsContentOverridden();
            }
            var objectNode = AssociatedNode.Node as IAssetObjectNode;
            if (objectNode != null)
            {
                return objectNode.IsItemOverridden(AssociatedNode.Index);
            }
            return false;
        }

        private void OnOverrideChanging(object sender, EventArgs e)
        {
            OverrideChanging?.Invoke(sender, e);
        }

        private void OnOverrideChanged(object sender, EventArgs e)
        {
            OverrideChanged?.Invoke(sender, e);
        }
    }
}
