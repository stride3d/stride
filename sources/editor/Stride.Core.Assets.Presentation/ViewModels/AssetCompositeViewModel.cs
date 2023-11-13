// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class AssetCompositeViewModel<TAsset> : AssetViewModel<TAsset> where TAsset : AssetComposite
{
    protected AssetCompositeViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }

    public AssetCompositePropertyGraph AssetCompositePropertyGraph => (AssetCompositePropertyGraph)PropertyGraph;
}
