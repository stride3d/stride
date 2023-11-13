// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class SourceCodeViewModel<TSourceCodeAsset> : AssetViewModel<TSourceCodeAsset>
    where TSourceCodeAsset : SourceCodeAsset
{
    protected SourceCodeViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }
}

/// <summary>
/// View model for <see cref="SourceCodeAsset"/>.
/// </summary>
[AssetViewModel<SourceCodeAsset>]
public class SourceCodeAssetViewModel : SourceCodeViewModel<SourceCodeAsset>
{
    public SourceCodeAssetViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }
}
