// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> : AssetCompositeViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>>
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
        where TAssetPart : class, IIdentifiable
    {
        protected AssetCompositeHierarchyViewModel([NotNull] AssetViewModelConstructionParameters parameters) : base(parameters)
        {
            Asset.Hierarchy.Parts.Values.ForEach(TrackBaseChanges);
            var parts = NodeContainer.GetNode(Asset.Hierarchy.Parts);
            parts.ItemChanged += PartsChanged;
            Session.DeletedAssetsChanged += AssetsDeleted;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            Asset.Hierarchy.Parts.Values.ForEach(UntrackBaseChanges);
            var parts = NodeContainer.GetNode(Asset.Hierarchy.Parts);
            parts.ItemChanged -= PartsChanged;
            Session.DeletedAssetsChanged -= AssetsDeleted;
            base.Destroy();
        }

        public AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> AssetHierarchyPropertyGraph => (AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>)PropertyGraph;

        /// <summary>
        /// Gathers all base assets used in the composition of this asset, recursively.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public IEnumerable<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>> GatherAllBasePartAssets()
        {
            var baseAssets = new HashSet<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>();
            var assetToProcess = new Stack<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>();
            assetToProcess.Push(this);

            while (assetToProcess.Count > 0)
            {
                var asset = assetToProcess.Pop();
                if (asset != null)
                {
                    foreach (var basePartAsset in asset.AssetHierarchyPropertyGraph.GetBasePartAssets())
                    {
                        var viewModel = Session.GetAssetById(basePartAsset.Id) as AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>;
                        if (viewModel != null && baseAssets.Add(viewModel))
                        {
                            assetToProcess.Push(viewModel);
                        }
                    }
                }
            }

            return baseAssets;
        }

        /// <inheritdoc />
        protected override bool ShouldConstructPropertyMember(IMemberNode member)
        {
            // Don't construct properties of the Hierarchy object.
            if (member.MemberDescriptor.DeclaringType == typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>))
                return false;

            // Don't construct properties for member referencing child parts.
            if (AssetHierarchyPropertyGraph.IsChildPartReference(member, NodeIndex.Empty))
                return false;

            return base.ShouldConstructPropertyMember(member);
        }

        /// <inheritdoc />
        protected override bool ShouldConstructPropertyItem(IObjectNode collection, NodeIndex index)
        {
            // Don't construct properties for item referencing child parts.
            if (AssetHierarchyPropertyGraph.IsChildPartReference(collection, index))
                return false;

            return base.ShouldConstructPropertyItem(collection, index);
        }

        private void AssetsDeleted(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && !UndoRedoService.UndoRedoInProgress)
            {
                foreach (var assetPropertyGraph in e.NewItems.OfType<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>().Select(x => x.PropertyGraph).OfType<AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>>())
                {
                    var instanceIds = AssetHierarchyPropertyGraph.GetInstanceIds(assetPropertyGraph);
                    AssetHierarchyPropertyGraph?.BreakBasePartLinks(Asset.Hierarchy.Parts.Values.Where(x => x.Base != null && instanceIds.Contains(x.Base.InstanceId)));
                }
            }
        }

        private void TrackBaseChanges(TAssetPartDesign part)
        {
            if (part != null)
            {
                var partNode = NodeContainer.GetNode(part);
                partNode[nameof(IAssetPartDesign.Base)].ValueChanged += BaseChanged;
            }
        }

        private void UntrackBaseChanges(TAssetPartDesign part)
        {
            if (part != null)
            {
                var partNode = NodeContainer.GetNode(part);
                partNode[nameof(IAssetPartDesign.Base)].ValueChanged -= BaseChanged;
            }
        }

        private void PartsChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionUpdate:
                case ContentChangeType.CollectionAdd:
                case ContentChangeType.CollectionRemove:
                    UntrackBaseChanges((TAssetPartDesign)e.OldValue);
                    TrackBaseChanges((TAssetPartDesign)e.NewValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BaseChanged(object sender, MemberNodeChangeEventArgs e)
        {
            var part = NodeContainer.GetNode(((TAssetPartDesign)e.Member.Parent.Retrieve()).Part);
            var baseInfo = (BasePart)e.Member.Retrieve();
            if (baseInfo == null)
            {
                AssetHierarchyPropertyGraph.RefreshBase((IAssetNode)part, null);
            }
            else
            {
                var baseAsset = Session.GetAssetById(baseInfo.BasePartAsset.Id) as AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>;
                TAssetPartDesign basePart;
                if (baseAsset != null && baseAsset.Asset.Hierarchy.Parts.TryGetValue(baseInfo.BasePartId, out basePart))
                {
                    var basePartNode = (IAssetNode)NodeContainer.GetNode(basePart.Part);
                    AssetHierarchyPropertyGraph.RefreshBase((IAssetNode)part, basePartNode);
                }
            }
        }
    }
}
