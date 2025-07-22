// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Extensions;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class UIPageRootViewModel : UIRootViewModel
{
    public UIPageRootViewModel(UIPageViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns().Single().Yield())
    {
    }

    public override string? Name { get => "UIPageRoot"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(UIPageRootViewModel)} object."); }
}
