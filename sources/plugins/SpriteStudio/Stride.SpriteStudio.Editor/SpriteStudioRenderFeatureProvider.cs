// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

/// <summary>
///   Exposes the SpriteStudio render feature to the rendering system so it can be registered in the graphics compositor
///   when the SpriteStudio package is loaded.
///   The feature draws SpriteStudio sheets: in the entity previews and thumbnails, and in a game compositor on request.
/// </summary>
public sealed class SpriteStudioRenderFeatureProvider : IRenderFeatureProvider
{
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderFeatureProviderContext context)
    {
        yield return new SpriteStudioRenderFeature
        {
            RenderStageSelectors =
            {
                new SimpleGroupToRenderStageSelector
                {
                    EffectName = "SpriteStudio",
                    RenderStage = context.TransparentStage
                }
            }
        };
    }
}
