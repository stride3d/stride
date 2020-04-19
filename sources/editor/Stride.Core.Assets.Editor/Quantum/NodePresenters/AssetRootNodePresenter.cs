// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters
{
    public class AssetRootNodePresenter : RootNodePresenter, IAssetNodePresenter
    {
        public AssetRootNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] IAssetObjectNode rootNode)
            : base(factory, propertyProvider, rootNode)
        {
        }

        public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

        public bool HasBase => RootNode.BaseNode != null;

        public bool IsInherited => false;

        public bool IsOverridden => false;

        public AssetViewModel Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

        public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

        public event EventHandler<EventArgs> OverrideChanging { add { RootNode.OverrideChanging += value; } remove { RootNode.OverrideChanging -= value; } }

        public event EventHandler<EventArgs> OverrideChanged { add { RootNode.OverrideChanged += value; } remove { RootNode.OverrideChanged -= value; } }

        [NotNull]
        private new IAssetObjectNode RootNode => (IAssetObjectNode)base.RootNode;

        public bool IsObjectReference(object value)
        {
            return false;
        }

        public void ResetOverride()
        {
            RootNode.ResetOverrideRecursively();
        }
    }
}
