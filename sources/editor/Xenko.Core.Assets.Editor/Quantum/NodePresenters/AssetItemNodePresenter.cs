// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters
{
    public class AssetItemNodePresenter : ItemNodePresenter, IAssetNodePresenter
    {
        public AssetItemNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, [NotNull] IAssetObjectNode container, NodeIndex index)
            : base(factory, propertyProvider, parent, container, index)
        {
        }

        public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

        public bool HasBase => Container.BaseNode != null;

        public bool IsInherited => Container.IsItemInherited(Index);

        public bool IsOverridden => Container.IsItemOverridden(Index);

        public AssetViewModel Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

        public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

        public event EventHandler<EventArgs> OverrideChanging { add { Container.OverrideChanging += value; } remove { Container.OverrideChanging -= value; } }

        public event EventHandler<EventArgs> OverrideChanged { add { Container.OverrideChanged += value; } remove { Container.OverrideChanged -= value; } }

        [NotNull]
        private new IAssetObjectNode Container => (IAssetObjectNode)base.Container;

        public bool IsObjectReference(object value)
        {
            return Container.PropertyGraph?.Definition.IsTargetItemObjectReference(Container, Index, Value) ?? false;
        }

        public void ResetOverride()
        {
            Container.ResetOverrideRecursively(Index);
        }
    }
}
