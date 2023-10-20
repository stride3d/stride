// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Presentation.ViewModels;

public sealed class SceneRootViewModel : EntityHierarchyRootViewModel
{
    public SceneRootViewModel(SceneViewModel asset)
        : base(asset)
    {
    }

    public override string? Name { get => "SceneRoot"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(SceneRootViewModel)} object."); }
}
