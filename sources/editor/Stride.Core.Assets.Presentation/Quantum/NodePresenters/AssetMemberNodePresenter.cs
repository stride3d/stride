// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Presentation.Quantum.NodePresenters;

public class AssetMemberNodePresenter : MemberNodePresenter, IAssetNodePresenter
{
    public AssetMemberNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter parent, IAssetMemberNode member)
        : base(factory, propertyProvider, parent, member)
    {
    }

    public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

    public bool HasBase => Member.BaseNode != null;

    public bool IsInherited => Member.IsContentInherited();

    public bool IsOverridden => Member.IsContentOverridden();

    public AssetViewModel? Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

    public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

    public event EventHandler<EventArgs>? OverrideChanging { add { Member.OverrideChanging += value; } remove { Member.OverrideChanging -= value; } }

    public event EventHandler<EventArgs>? OverrideChanged { add { Member.OverrideChanged += value; } remove { Member.OverrideChanged -= value; } }

    private new IAssetMemberNode Member => (IAssetMemberNode)base.Member;

    public bool IsObjectReference(object value)
    {
        return Member.PropertyGraph?.Definition.IsMemberTargetObjectReference(Member, value) ?? false;
    }

    public void ResetOverride()
    {
        Member.ResetOverrideRecursively();
    }
}
