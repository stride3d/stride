// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace Stride.Assets.Presentation.SceneEditor
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

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            foreach (var renderObject in RootRenderFeature.RenderObjects)
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
