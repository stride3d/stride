// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="AnimationAsset"/>.
/// </summary>
[AssetViewModel<AnimationAsset>]
public sealed class AnimationViewModel : ImportedAssetViewModel<AnimationAsset>
{
    public AnimationViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    protected override IAssetImporter? GetImporter()
    {
        return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
    }

    /// <inheritdoc />
    protected override bool IsMatchingImportedAsset(AnimationAsset importedAsset)
    {
        return importedAsset.AnimationStack == Asset.AnimationStack;
    }

    /// <inheritdoc />
    protected override void UpdateAssetFromSource(AnimationAsset assetToMerge)
    {
        AssetRootNode[nameof(AnimationAsset.AnimationTimeMaximum)].Update(assetToMerge.AnimationTimeMaximum);
        AssetRootNode[nameof(AnimationAsset.AnimationTimeMinimum)].Update(assetToMerge.AnimationTimeMinimum);
    }
}
