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
    public class AssetMemberNodePresenter : MemberNodePresenter, IAssetNodePresenter
    {
        public AssetMemberNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, [NotNull] IAssetMemberNode member)
            : base(factory, propertyProvider, parent, member)
        {
        }

        public new IAssetNodePresenter this[string childName] => (IAssetNodePresenter)base[childName];

        public bool HasBase => Member.BaseNode != null;

        public bool IsInherited => Member.IsContentInherited();

        public bool IsOverridden => Member.IsContentOverridden();

        public AssetViewModel Asset => (PropertyProvider as IAssetPropertyProviderViewModel)?.RelatedAsset;

        public new AssetNodePresenterFactory Factory => (AssetNodePresenterFactory)base.Factory;

        public event EventHandler<EventArgs> OverrideChanging { add { Member.OverrideChanging += value; } remove { Member.OverrideChanging -= value; } }

        public event EventHandler<EventArgs> OverrideChanged { add { Member.OverrideChanged += value; } remove { Member.OverrideChanged -= value; } }

        [NotNull]
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
}
