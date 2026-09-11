// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// Draws SpriteStudio sheets in the entity previews and thumbnails.
/// </summary>
public sealed class SpriteStudioRenderFeatureProvider : IRenderFeatureProvider
{
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderStage opaqueStage, RenderStage transparentStage)
    {
        yield return new SpriteStudioRenderFeature
        {
            RenderStageSelectors =
            {
                new SimpleGroupToRenderStageSelector
                {
                    EffectName = "SpriteStudio",
                    RenderStage = transparentStage,
                },
            },
        };
    }
}
