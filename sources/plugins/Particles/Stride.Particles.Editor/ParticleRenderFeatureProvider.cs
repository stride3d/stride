// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Rendering;
using Stride.Particles.Rendering;
using Stride.Rendering;

namespace Stride.Particles.Editor;

/// <summary>
/// Draws particle systems: in the entity previews and thumbnails, and in a game compositor on request.
/// </summary>
public sealed class ParticleRenderFeatureProvider : IRenderFeatureProvider
{
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderStage opaqueStage, RenderStage transparentStage)
    {
        yield return new ParticleEmitterRenderFeature
        {
            RenderStageSelectors =
            {
                new ParticleEmitterTransparentRenderStageSelector
                {
                    OpaqueRenderStage = opaqueStage,
                    TransparentRenderStage = transparentStage,
                },
            },
        };
    }
}
