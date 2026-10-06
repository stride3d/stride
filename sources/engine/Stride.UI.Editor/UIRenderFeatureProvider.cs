// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.Rendering.UI;

namespace Stride.UI.Editor;

/// <summary>
/// The UI render feature, as in the engine's default compositors, for previews, thumbnails and the UI and prefab editors.
/// </summary>
public sealed class UIRenderFeatureProvider : IRenderFeatureProvider
{
    public IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderStage opaqueStage, RenderStage transparentStage)
    {
        yield return new UIRenderFeature
        {
            RenderStageSelectors =
            {
                new SimpleGroupToRenderStageSelector
                {
                    RenderStage = transparentStage,
                    EffectName = "Test",
                },
            },
        };
    }
}
