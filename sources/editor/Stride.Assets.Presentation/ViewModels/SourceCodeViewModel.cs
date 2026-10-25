// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class SourceCodeViewModel<TSourceCodeAsset> : AssetViewModel<TSourceCodeAsset>
    where TSourceCodeAsset : SourceCodeAsset
{
    protected SourceCodeViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    /// The full path to the source code asset on disk.
    /// </summary>
    public UFile FullPath => AssetItem.FullPath;
}

/// <summary>
/// View model for <see cref="SourceCodeAsset"/>.
/// </summary>
[AssetViewModel<SourceCodeAsset>]
public class SourceCodeAssetViewModel : SourceCodeViewModel<SourceCodeAsset>
{
    public SourceCodeAssetViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }
}
