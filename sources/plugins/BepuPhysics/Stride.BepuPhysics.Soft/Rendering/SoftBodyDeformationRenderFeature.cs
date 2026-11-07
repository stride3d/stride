// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Threading;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// Mixes the vertex shader of a mesh deformed on the GPU into every effect that draws it (main, shadow, depth, picking passes),
/// whatever its material, and binds the particles it reads.
/// </summary>
/// <remarks>
/// A mesh asks for it with <see cref="StrideEffectBaseKeys.ExtensionPostVertexStageShader"/> in its <see cref="Mesh.Parameters"/>,
/// as <see cref="GpuDeformation.Bind"/> does; <see cref="SoftBodyProcessor"/> adds this feature to the mesh render features of the compositor.
/// </remarks>
internal sealed class SoftBodyDeformationRenderFeature : SubRenderFeature
{
    private struct DeformationInfo
    {
        public ParameterCollection? Parameters;
        public int PermutationCounter;
        public ShaderSource? Shader;
    }

    private StaticObjectPropertyKey<RenderEffect> _renderEffectKey;
    private StaticObjectPropertyKey<DeformationInfo> _deformationInfoKey;
    private LogicalGroupReference _particlesGroup;
    private LogicalGroupReference _ringGroup;

    protected override void InitializeCore()
    {
        var root = (RootEffectRenderFeature)RootRenderFeature;
        _renderEffectKey = root.RenderEffectKey;
        _deformationInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<DeformationInfo>();
        _particlesGroup = root.CreateDrawLogicalGroup("SoftBody");
        _ringGroup = root.CreateDrawLogicalGroup("SoftBodySurface");
    }

    public override void PrepareEffectPermutations(RenderDrawContext context)
    {
        var deformationInfos = RootRenderFeature.RenderData.GetData(_deformationInfoKey);
        var renderEffects = RootRenderFeature.RenderData.GetData(_renderEffectKey);
        int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

        Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, objectNodeReference =>
        {
            var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
            var renderMesh = (RenderMesh)objectNode.RenderObject;
            var staticObjectNode = renderMesh.StaticObjectNode;

            ref var info = ref deformationInfos[staticObjectNode];
            var parameters = renderMesh.Mesh.Parameters;
            if (parameters != info.Parameters || parameters.PermutationCounter != info.PermutationCounter)
            {
                info.Parameters = parameters;
                info.PermutationCounter = parameters.PermutationCounter;
                info.Shader = parameters.Get(StrideEffectBaseKeys.ExtensionPostVertexStageShader);
            }
            if (info.Shader is null)
                return;

            for (int i = 0; i < effectSlotCount; ++i)
            {
                var renderEffect = renderEffects[staticObjectNode * effectSlotCount + i];
                if (renderEffect != null && renderEffect.IsUsedDuringThisFrame(RenderSystem))
                    renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.ExtensionPostVertexStageShader, info.Shader);
            }
        });
    }

    public override void Prepare(RenderDrawContext context)
    {
        var renderNodes = ((RootEffectRenderFeature)RootRenderFeature).RenderNodes;
        Dispatcher.ForBatched(renderNodes.Count, (from, toExclusive) =>
        {
            for (int i = from; i < toExclusive; i++)
            {
                var renderNode = renderNodes[i];
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout is null)
                    continue;

                // Only effects compiled with the deformation shader have the group
                var particlesGroup = perDrawLayout.GetLogicalGroup(_particlesGroup);
                if (particlesGroup.DescriptorEntryStart == -1)
                    continue;

                var parameters = ((RenderMesh)renderNode.RenderObject).Mesh.Parameters;
                renderNode.Resources.DescriptorSet.SetShaderResourceView(particlesGroup.DescriptorEntryStart, parameters.Get(SoftBodyDeformationKeys.Particles));

                var ringGroup = perDrawLayout.GetLogicalGroup(_ringGroup);
                if (ringGroup.DescriptorEntryStart != -1)
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(ringGroup.DescriptorEntryStart, parameters.Get(SoftBodySurfaceDeformationKeys.Ring));
            }
        });
    }
}
