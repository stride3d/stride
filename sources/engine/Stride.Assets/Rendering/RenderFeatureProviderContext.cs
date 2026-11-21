// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Rendering;

/// <summary>
///   Provides context for creating render features, giving access to the various render stages of the graphics compositor.
/// </summary>
public sealed class RenderFeatureProviderContext
{
    private readonly IList<RenderStage> renderStages;

    /// <summary>
    ///   The opaque render stage of the graphics compositor.
    /// </summary>
    public RenderStage OpaqueStage { get; }
    /// <summary>
    ///   The transparent render stage of the graphics compositor.
    /// </summary>
    public RenderStage TransparentStage { get; }

    /// <summary>
    ///   An optional render stage that occurs after post-processing effects in the graphics compositor.
    /// </summary>
    /// <value>
    ///   The render stage that occurs after post-processing effects in the graphics compositor.
    ///   It can be <see langword="null"/> when the compositor has no such stage.
    /// </value>
    public RenderStage AfterPostEffectsStage { get; }

    /// <summary>
    ///   Finds a render stage by its name in the graphics compositor.
    /// </summary>
    /// <param name="name">The name of the render stage to find.</param>
    /// <returns>
    ///   The render stage with the specified name, or <see langword="null"/> if no such stage exists.
    /// </returns>
    public RenderStage FindStage(string name)
    {
        return renderStages.FirstOrDefault(x => x.Name == name);
    }


    /// <summary>
    ///   Initializes a new instance of the <see cref="RenderFeatureProviderContext"/> class
    ///   with the specified graphics compositor.
    /// </summary>
    /// <param name="graphicsCompositor">The graphics compositor asset to provide context for.</param>
    public RenderFeatureProviderContext(GraphicsCompositorAsset graphicsCompositor)
    {
        renderStages = graphicsCompositor.RenderStages;

        OpaqueStage = renderStages.FirstOrDefault(x => x.Name == "Opaque");
        TransparentStage = renderStages.FirstOrDefault(x => x.Name == "Transparent");
        AfterPostEffectsStage = renderStages.FirstOrDefault(x => x.Name == "AfterPostEffects");
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="RenderFeatureProviderContext"/> class
    ///   with the specified graphics compositor.
    /// </summary>
    /// <param name="graphicsCompositor">The graphics compositor to provide context for.</param>
    public RenderFeatureProviderContext(GraphicsCompositor graphicsCompositor)
    {
        renderStages = graphicsCompositor.RenderStages;

        OpaqueStage = renderStages.FirstOrDefault(x => x.Name == "Opaque");
        TransparentStage = renderStages.FirstOrDefault(x => x.Name == "Transparent");
        AfterPostEffectsStage = renderStages.FirstOrDefault(x => x.Name == "AfterPostEffects");
    }
}
