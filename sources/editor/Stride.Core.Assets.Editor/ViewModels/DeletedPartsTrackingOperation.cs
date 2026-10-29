// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Represents the operation of updating the mapping of deleted part instances in an <see cref="AssetCompositeHierarchyPropertyGraph{TAssetPartDesign,TAssetPart}"/>.
/// </summary>
public sealed class DeletedPartsTrackingOperation<TAssetPartDesign, TAssetPart> : DirtyingOperation
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
{
    private readonly HashSet<Tuple<Guid, Guid>> deletedPartsMapping;
    private AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>? propertyGraph;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletedPartsTrackingOperation{TAssetPartDesign,TAssetPart}"/> class.
    /// </summary>
    /// <param name="viewModel">The asset the parts were deleted from.</param>
    /// <param name="deletedPartsMapping">A mapping of the base information (base part id, instance id) of the deleted parts that have a base.</param>
    public DeletedPartsTrackingOperation(AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> viewModel, HashSet<Tuple<Guid, Guid>> deletedPartsMapping)
        : base(viewModel.Dirtiables)
    {
        ArgumentNullException.ThrowIfNull(deletedPartsMapping);
        this.deletedPartsMapping = deletedPartsMapping;
        propertyGraph = viewModel.AssetHierarchyPropertyGraph;
    }

    /// <inheritdoc />
    protected override void FreezeContent()
    {
        propertyGraph = null;
    }

    /// <inheritdoc />
    protected override void Undo()
    {
        propertyGraph!.UntrackDeletedInstanceParts(deletedPartsMapping);
    }

    /// <inheritdoc />
    protected override void Redo()
    {
        propertyGraph!.TrackDeletedInstanceParts(deletedPartsMapping);
    }
}
