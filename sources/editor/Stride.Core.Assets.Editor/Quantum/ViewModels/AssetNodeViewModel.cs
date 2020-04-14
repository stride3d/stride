// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Quantum.ViewModels
{
    public class AssetNodeViewModel : NodeViewModel, IInternalAssetNodeViewModel
    {
        private bool overrideChanging;

        public AssetNodeViewModel(GraphViewModel ownerViewModel, NodeViewModel parent, string baseName, Type nodeType, List<INodePresenter> nodePresenters)
            : base(ownerViewModel, parent, baseName, nodeType, nodePresenters)
        {
            foreach (var nodePresenter in NodePresenters)
            {
                nodePresenter.OverrideChanging += OverrideChanging;
                nodePresenter.OverrideChanged += OverrideChanged;
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            foreach (var nodePresenter in NodePresenters)
            {
                nodePresenter.OverrideChanging -= OverrideChanging;
                nodePresenter.OverrideChanged -= OverrideChanged;
            }
            base.Destroy();
        }

        public bool HasBase => NodePresenters.All(x => x.HasBase);

        public bool IsInherited => NodePresenters.All(x => x.IsInherited);

        public bool IsOverridden => NodePresenters.All(x => x.IsOverridden);

        [Obsolete]
        public bool IsObjectReference => NodePresenters.All(x => x.IsObjectReference(x.Value));

        private new IEnumerable<IAssetNodePresenter> NodePresenters => base.NodePresenters.Cast<IAssetNodePresenter>();

        protected override void SetNodeValue(object newValue)
        {
            using (var transaction = ServiceProvider.TryGet<IUndoRedoService>()?.CreateTransaction())
            {
                base.SetNodeValue(newValue);
                if (transaction != null)
                {
                    ServiceProvider.TryGet<IUndoRedoService>()?.SetName(transaction, $"Update property {DisplayPath}");
                }
            }
        }

        /// <inheritdoc/>
        protected override bool AreValueEqual(object value1, object value2)
        {
            // If we are comparing content references, check if they are equal (ie. match the same target asset), otherwise fallback to default behavior.
            return AreContentReferenceEqual(value1, value2) || base.AreValueEqual(value1, value2);
        }

        /// <inheritdoc/>
        protected override bool AreValueEquivalent(object value1, object value2)
        {
            // If we are comparing object references, check if they are equal (ie. match the same object), otherwise fallback to default behavior.
            return NodePresenters.First().IsObjectReference(value1) && ReferenceEquals(value1, value2) || base.AreValueEquivalent(value1, value2);
        }

        protected override NodePresenterCommandWrapper ConstructCommandWrapper(INodePresenterCommand command)
        {
            return new AssetNodePresenterCommandWrapper(ServiceProvider, base.NodePresenters, command);
        }

        private static bool AreContentReferenceEqual(object reference1, object reference2)
        {
            if (reference1 != null && reference2 != null)
            {
                var type = reference1.GetType();
                if (type == reference2.GetType() && AssetRegistry.IsContentType(type))
                {
                    var target1 = AttachedReferenceManager.GetAttachedReference(reference1);
                    var target2 = AttachedReferenceManager.GetAttachedReference(reference2);
                    if (target1.Id == target2.Id && target1.Url == target2.Url)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OverrideChanging(object sender, EventArgs e)
        {
            if (!overrideChanging)
            {
                (Parent as IInternalAssetNodeViewModel)?.ChildOverrideChanging();
                OnPropertyChanging(nameof(HasBase), nameof(IsOverridden), nameof(IsInherited));
                overrideChanging = true;
            }
        }

        private void OverrideChanged(object sender, EventArgs e)
        {
            if (overrideChanging)
            {
                OnPropertyChanged(nameof(HasBase), nameof(IsOverridden), nameof(IsInherited));
                (Parent as IInternalAssetNodeViewModel)?.ChildOverrideChanged();
                overrideChanging = false;
            }
        }

        void IInternalAssetNodeViewModel.ChildOverrideChanging()
        {
            OverrideChanging(this, EventArgs.Empty);
        }

        void IInternalAssetNodeViewModel.ChildOverrideChanged()
        {
            OverrideChanged(this, EventArgs.Empty);
        }
    }
}
