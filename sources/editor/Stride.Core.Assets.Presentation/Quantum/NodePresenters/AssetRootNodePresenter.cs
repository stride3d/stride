// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Presentation.Quantum.NodePresenters;

public class AssetRootNodePresenter : RootNodePresenter, IAssetNodePresenter
{
    public AssetRootNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, IAssetObjectNode rootNode)
        : base(factory, propertyProvider, rootNode)
    {
    }

    public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

    public bool HasBase => RootNode.BaseNode != null;

    public bool IsInherited => false;

    public bool IsOverridden => false;

    public AssetViewModel? Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

    public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

    public event EventHandler<EventArgs>? OverrideChanging { add { RootNode.OverrideChanging += value; } remove { RootNode.OverrideChanging -= value; } }

    public event EventHandler<EventArgs>? OverrideChanged { add { RootNode.OverrideChanged += value; } remove { RootNode.OverrideChanged -= value; } }

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
