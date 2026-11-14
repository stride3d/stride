// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.Rendering.UI;

namespace Stride.UI.Editor;

/// <summary>
///   Exposes the UI render feature to the rendering system so it can be registered in the graphics compositor
///   when the UI package is loaded.
///   Draws UI elements: in previews and thumbnails, and in the UI and prefab editors.
/// </summary>
public sealed class UIRenderFeatureProvider : IRenderFeatureProvider
{
    /// <inheritdoc/>
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderFeatureProviderContext context)
    {
        yield return new UIRenderFeature
        {
            RenderStageSelectors =
            {
                new UIRenderStageSelector
                {
                    TransparentRenderStage = context.TransparentStage,
                    AfterPostEffectsRenderStage = context.AfterPostEffectsStage,
                    EffectName = "UI"
                }
            }
        };
    }
}
