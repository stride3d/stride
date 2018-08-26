// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering;
using Xenko.Rendering.Materials;
using Xenko.Shaders;

namespace Xenko.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// Performs material filtering to display only specific material shader streams on the screen, such as specular, normals, etc...
    /// </summary>
    public class MaterialFilterRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        // TODO GRAPHICS REFACTOR: Should be per render stage
        public ShaderSource MaterialFilter { get; set; }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            renderEffectKey = ((RootEffectRenderFeature)rootRenderFeature).RenderEffectKey;
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = rootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)rootRenderFeature).EffectPermutationSlotCount;

            foreach (var renderObject in rootRenderFeature.RenderObjects)
            {
                var staticObjectNode = renderObject.StaticObjectNode;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip effects not used during this frame
                    if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    // TODO GRAPHICS REFACTOR: Merge in MaterialRenderFeature. Filter by effect slots?
                    if (MaterialFilter != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.PixelStageSurfaceFilter, MaterialFilter);
                }
            }
        }
    }
}
