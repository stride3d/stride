// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Assets.Rendering;
using Stride.Particles.Rendering;
using Stride.Rendering;

namespace Stride.Particles.Editor;

/// <summary>
///   Exposes the particles render feature to the rendering system so it can be registered in the graphics compositor
///   when the particles package is loaded.
///   Draws particle systems: in the entity previews and thumbnails, and in a game compositor on request.
/// </summary>
public sealed class ParticleRenderFeatureProvider : IRenderFeatureProvider
{
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderFeatureProviderContext context)
    {
        yield return new ParticleEmitterRenderFeature
        {
            RenderStageSelectors =
            {
                new ParticleEmitterTransparentRenderStageSelector
                {
                    OpaqueRenderStage = context.OpaqueStage,
                    TransparentRenderStage = context.TransparentStage
                }
            }
        };
    }
}
