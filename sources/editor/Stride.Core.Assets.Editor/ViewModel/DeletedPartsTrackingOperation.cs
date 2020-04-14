// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Represents the operation of updating the mapping of deleted part instances in an <see cref="AssetCompositeHierarchyPropertyGraph{TAssetPartDesign,TAssetPart}"/>.
    /// </summary>
    /// <typeparam name="TAssetPartDesign"></typeparam>
    /// <typeparam name="TAssetPart"></typeparam>
    public class DeletedPartsTrackingOperation<TAssetPartDesign, TAssetPart> : DirtyingOperation
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
        where TAssetPart : class, IIdentifiable
    {
        private readonly HashSet<Tuple<Guid, Guid>> deletedPartsMapping;
        private AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> propertyGraph;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeletedPartsTrackingOperation{TAssetPartDesign,TAssetPart}"/> class.
        /// </summary>
        /// <param name="viewmodel"></param>
        /// <param name="deletedPartsMapping">A mapping of the base information (base part id, instance id) of the deleted parts that have a base.</param>
        public DeletedPartsTrackingOperation([NotNull] AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> viewmodel, [NotNull] HashSet<Tuple<Guid, Guid>> deletedPartsMapping)
            : base(viewmodel.SafeArgument(nameof(viewmodel)).Dirtiables)
        {
            if (deletedPartsMapping == null) throw new ArgumentNullException(nameof(deletedPartsMapping));
            this.deletedPartsMapping = deletedPartsMapping;
            propertyGraph = viewmodel.AssetHierarchyPropertyGraph;

        }

        /// <inheritdoc />
        protected override void FreezeContent()
        {
            propertyGraph = null;
        }

        /// <inheritdoc />
        protected override void Undo()
        {
            propertyGraph.UntrackDeletedInstanceParts(deletedPartsMapping);
        }

        /// <inheritdoc />
        protected override void Redo()
        {
            propertyGraph.TrackDeletedInstanceParts(deletedPartsMapping);
        }
    }
}
