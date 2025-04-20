// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Presentation.Quantum.NodePresenters;

public class AssetItemNodePresenter : ItemNodePresenter, IAssetNodePresenter
{
    public AssetItemNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter parent,  IAssetObjectNode container, NodeIndex index)
        : base(factory, propertyProvider, parent, container, index)
    {
    }

    public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

    public bool HasBase => Container.BaseNode != null;

    public bool IsInherited => Container.IsItemInherited(Index);

    public bool IsOverridden => Container.IsItemOverridden(Index);

    public AssetViewModel? Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

    public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

    public event EventHandler<EventArgs>? OverrideChanging { add { Container.OverrideChanging += value; } remove { Container.OverrideChanging -= value; } }

    public event EventHandler<EventArgs>? OverrideChanged { add { Container.OverrideChanged += value; } remove { Container.OverrideChanged -= value; } }

    private new IAssetObjectNode Container => (IAssetObjectNode)base.Container;

    public bool IsObjectReference(object? value)
    {
        return Container.PropertyGraph?.Definition.IsTargetItemObjectReference(Container, Index, Value) ?? false;
    }

    public void ResetOverride()
    {
        Container.ResetOverrideRecursively(Index);
    }
}
