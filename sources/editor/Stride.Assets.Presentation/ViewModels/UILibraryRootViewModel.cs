// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class UILibraryRootViewModel : UIRootViewModel
{
    public UILibraryRootViewModel(UILibraryViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns())
    {
    }

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, Guid.Empty);

    public override string? Name { get => "UILibraryRoot"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(UILibraryRootViewModel)} object."); }
}
