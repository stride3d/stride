// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Materials;
using SpaceEscape.Effects;

namespace SpaceEscape.Rendering
{
    /// <summary>
    /// Custom render feature, that uploads constants needed by the SpaceEscapeEffectMain effect
    /// </summary>
    public class BendFogRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        private ConstantBufferOffsetReference fog;
        private ConstantBufferOffsetReference bend;
        private ConstantBufferOffsetReference uvChange;

        // Constant buffer layout for FogEffect
        private struct PerDrawFog
        {
            public Color4 FogColor;

            public float fogNearPlaneZ;
            public float fogFarPlaneZ;

            public float fogNearPlaneY;
            public float fogFarPlaneY;
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            renderEffectKey = ((RootEffectRenderFeature)rootRenderFeature).RenderEffectKey;

            fog = ((RootEffectRenderFeature)rootRenderFeature).CreateDrawCBufferOffsetSlot(FogEffectKeys.FogColor.Name);
            bend = ((RootEffectRenderFeature)rootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationBendWorldKeys.DeformFactorX.Name);
            uvChange = ((RootEffectRenderFeature)rootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationTextureUVKeys.TextureRegion.Name);
        }

        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = rootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)rootRenderFeature).EffectPermutationSlotCount;

            foreach (var renderObject in rootRenderFeature.RenderObjects)
            {
                var staticObjectNode = renderObject.StaticObjectNode;
                var renderMesh = (RenderMesh)renderObject;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip effects not used during this frame
                    if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    // Generate shader permuatations
                    renderEffect.EffectValidator.ValidateParameter(GameParameters.EnableBend, renderMesh.Mesh.Parameters.Get(GameParameters.EnableBend));
                    renderEffect.EffectValidator.ValidateParameter(GameParameters.EnableFog, renderMesh.Mesh.Parameters.Get(GameParameters.EnableFog));
                    renderEffect.EffectValidator.ValidateParameter(GameParameters.EnableOnflyTextureUVChange, renderMesh.Mesh.Parameters.Get(GameParameters.EnableOnflyTextureUVChange));
                }
            }
        }

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            foreach (var renderNode in ((RootEffectRenderFeature)rootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection.PerDrawLayout;
                if (perDrawLayout == null)
                    continue;

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var renderMesh = (RenderMesh)renderNode.RenderObject;
                var parameters = renderMesh.Mesh.Parameters;

                // Upload fog parameters
                var fogOffset = perDrawLayout.GetConstantBufferOffset(fog);
                if (fogOffset != -1)
                {
                    var perDraw = (PerDrawFog*)((byte*)mappedCB + fogOffset);
                    *perDraw = new PerDrawFog
                    {
                        FogColor = Color.FromAbgr(0xFF7D02FF),
                        fogNearPlaneZ = 80.0f,
                        fogFarPlaneZ = 250.0f,
                        fogNearPlaneY = 0.0f,
                        fogFarPlaneY = 120.0f
                    };
                }

                // Upload world bending parameters
                var bendOffset = perDrawLayout.GetConstantBufferOffset(bend);
                if (bendOffset != -1)
                {
                    var perDraw = (float*)((byte*)mappedCB + bendOffset);
                    *perDraw++ = parameters.Get(TransformationBendWorldKeys.DeformFactorX);
                    *perDraw = parameters.Get(TransformationBendWorldKeys.DeformFactorX);
                }

                // Updload uv change parameters
                var uvChangeOffset = perDrawLayout.GetConstantBufferOffset(uvChange);
                if (uvChangeOffset != -1)
                {
                    var perDraw = (Vector4*)((byte*)mappedCB + uvChangeOffset);
                    *perDraw = parameters.Get(TransformationTextureUVKeys.TextureRegion);
                }
            }
        }
    }
}
