// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.IO;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

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

    /// <inheritdoc/>
    protected internal override void Initialize()
    {
        base.Initialize();

        // Keeps the text of a project file in memory: an undo writes it back if the file is deleted
        if (Asset is IProjectAsset && !Directory.Package.Package.IsReadOnly)
            _ = Asset.Text;
    }
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
