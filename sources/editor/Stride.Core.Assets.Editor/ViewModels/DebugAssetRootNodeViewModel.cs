// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

public class DebugAssetRootNodeViewModel : DebugAssetChildNodeViewModel
{
    public DebugAssetRootNodeViewModel(IViewModelServiceProvider serviceProvider, string assetName, IGraphNode? node, HashSet<IGraphNode> registeredNodes)
        : base(serviceProvider, node, registeredNodes)
    {
        AssetName = assetName;
    }

    public string AssetName { get; }

    public Type? AssetType => Node?.Type;
}
