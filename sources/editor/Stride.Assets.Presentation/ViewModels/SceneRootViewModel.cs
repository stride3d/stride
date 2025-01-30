// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class SceneRootViewModel : EntityHierarchyRootViewModel
{
    /// <summary>
    /// Identifier of the game-side scene.
    /// </summary>
    private readonly Guid sceneId;

    public SceneRootViewModel(SceneViewModel asset)
        : base(asset)
    {
        sceneId = Guid.NewGuid();
    }

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, sceneId);

    public override string? Name { get => "SceneRoot"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(SceneRootViewModel)} object."); }
}
