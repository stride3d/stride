// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class PrefabRootViewModel : EntityHierarchyRootViewModel
{
    public PrefabRootViewModel(PrefabViewModel asset)
        : base(asset)
    {
    }
    
    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, Guid.Empty);

    public override string? Name { get => "PrefabRoot"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(PrefabRootViewModel)} object."); }
}
