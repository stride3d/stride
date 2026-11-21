// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the start and the end of the reference clip of a difference animation, edited in frames.
/// </summary>
public sealed class AnimationFrameBoxTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "AnimationFrameBoxTemplateProvider";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Name is nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox) or nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox);
    }
}
