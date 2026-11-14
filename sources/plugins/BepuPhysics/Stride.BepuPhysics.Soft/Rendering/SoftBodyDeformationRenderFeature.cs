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
    private LogicalGroupReference _framesGroup;
    private volatile bool _requested;

    /// <summary> Whether the feature went through the meshes of the frame being drawn: only when a processor asked, so it costs nothing without a mesh deformed on the GPU </summary>
    internal bool Active { get; private set; }

    /// <summary> Asks the feature to go through the meshes of the next frame drawn, which some are deformed on the GPU for </summary>
    internal void Request() => _requested = true;

    protected override void InitializeCore()
    {
        var root = (RootEffectRenderFeature)RootRenderFeature;
        _renderEffectKey = root.RenderEffectKey;
        _deformationInfoKey = RootRenderFeature.RenderData.CreateStaticObjectKey<DeformationInfo>();
        _particlesGroup = root.CreateDrawLogicalGroup("SoftBody");
        _ringGroup = root.CreateDrawLogicalGroup("SoftBodySurface");
        _framesGroup = root.CreateDrawLogicalGroup("SoftBodyFrames");
    }

    public override void PrepareEffectPermutations(RenderDrawContext context)
    {
        Active = _requested;
        _requested = false;
        if (Active == false)
            return;

        var deformationInfos = RootRenderFeature.RenderData.GetData(_deformationInfoKey);
        var renderEffects = RootRenderFeature.RenderData.GetData(_renderEffectKey);
        int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;
        var state = (self: this, deformationInfos, renderEffects, effectSlotCount);

        Dispatcher.ForEach(RootRenderFeature.ObjectNodeReferences, state, static (s, objectNodeReference) =>
        {
            var objectNode = s.self.RootRenderFeature.GetObjectNode(objectNodeReference);
            var renderMesh = (RenderMesh)objectNode.RenderObject;
            var staticObjectNode = renderMesh.StaticObjectNode;

            ref var info = ref s.deformationInfos[staticObjectNode];
            var parameters = renderMesh.Mesh.Parameters;
            if (parameters != info.Parameters || parameters.PermutationCounter != info.PermutationCounter)
            {
                info.Parameters = parameters;
                info.PermutationCounter = parameters.PermutationCounter;
                info.Shader = parameters.Get(StrideEffectBaseKeys.ExtensionPostVertexStageShader);
            }
            if (info.Shader is null)
                return;

            for (int i = 0; i < s.effectSlotCount; ++i)
            {
                var renderEffect = s.renderEffects[staticObjectNode * s.effectSlotCount + i];
                if (renderEffect != null && renderEffect.IsUsedDuringThisFrame(s.self.RenderSystem))
                    renderEffect.EffectValidator.ValidateParameter(StrideEffectBaseKeys.ExtensionPostVertexStageShader, info.Shader);
            }
        });
    }

    public override void Prepare(RenderDrawContext context)
    {
        if (Active == false)
            return;

        var renderNodes = ((RootEffectRenderFeature)RootRenderFeature).RenderNodes;
        var state = (self: this, renderNodes);

        Dispatcher.ForBatched(renderNodes.Count, state, static (s, from, toExclusive) =>
        {
            for (int i = from; i < toExclusive; i++)
            {
                var renderNode = s.renderNodes[i];
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout is null)
                    continue;

                var particlesGroup = perDrawLayout.GetLogicalGroup(s.self._particlesGroup);
                if (particlesGroup.DescriptorEntryStart == -1)
                    continue;

                var parameters = ((RenderMesh)renderNode.RenderObject).Mesh.Parameters;
                renderNode.Resources.DescriptorSet.SetShaderResourceView(particlesGroup.DescriptorEntryStart, parameters.Get(SoftBodyDeformationKeys.Particles));

                var framesGroup = perDrawLayout.GetLogicalGroup(s.self._framesGroup);
                if (framesGroup.DescriptorEntryStart != -1)
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(framesGroup.DescriptorEntryStart, parameters.Get(SoftBodySmoothLatticeDeformationKeys.Frames));

                var ringGroup = perDrawLayout.GetLogicalGroup(s.self._ringGroup);
                if (ringGroup.DescriptorEntryStart != -1)
                    renderNode.Resources.DescriptorSet.SetShaderResourceView(ringGroup.DescriptorEntryStart, parameters.Get(SoftBodySurfaceDeformationKeys.Ring));
            }
        });
    }
}
