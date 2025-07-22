// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

public class DebugAssetBaseNodeViewModel : DebugAssetNodeViewModel
{
    public DebugAssetBaseNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode node)
        : base(serviceProvider, node)
    {
        Asset = DebugAssetNodeCollectionViewModel.FindAssetForNode(node.Guid);
    }

    public AssetViewModel? Asset { get; }
}
