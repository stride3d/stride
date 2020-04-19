// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core;
using Stride.Core.Threading;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Handles material by permuting shaders and uploading material data.
    /// </summary>
    public class MaterialRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;
        private StaticObjectPropertyKey<TessellationState> tessellationStateKey;

        private EffectDescriptorSetReference perMaterialDescriptorSetSlot;

        private ConcurrentCollector<RenderMesh> renderMeshesToGenerateAEN = new ConcurrentCollector<RenderMesh>();

        // Material instantiated
        private readonly Dictionary<MaterialPass, MaterialInfo> allMaterialInfos = new Dictionary<MaterialPass, MaterialInfo>();

        public class MaterialInfoBase
        {
            public int LastFrameUsed;
            public SpinLock UpdateLock;

            // Any matching effect
            public ResourceGroupLayout PerMaterialLayout;

            /// <summary>
            /// <c>true</c> if MaterialParameters instance was changed
            /// </summary>
            public bool ParametersChanged;

            public ParameterCollection ParameterCollection = new ParameterCollection();
            public ParameterCollectionLayout ParameterCollectionLayout;
            public ParameterCollection.Copier ParameterCollectionCopier;

            // PerMaterial
            public ResourceGroup Resources = new ResourceGroup();
            public int ResourceCount;
            public EffectConstantBufferDescription ConstantBufferReflection;
        }

        /// <summary>
        /// Custom extra info that we want to store per material.
        /// </summary>
        internal class MaterialInfo : MaterialInfoBase
        {
            public MaterialPass MaterialPass;

            // Permutation parameters
            public int PermutationCounter; // Dirty counter against material.Parameters.PermutationCounter
            public ParameterCollection MaterialParameters; // Protect against changes of Material.Parameters instance (happens with editor fast reload)
            public CullMode? CullMode;

            public ShaderSource VertexStageSurfaceShaders;
            public ShaderSource VertexStageStreamInitializer;

            public ShaderSource DomainStageSurfaceShaders;
            public ShaderSource DomainStageStreamInitializer;

            public ShaderSource TessellationShader;

            public ShaderSource PixelStageSurfaceShaders;
            public ShaderSource PixelStageStreamInitializer;

            public bool HasNormalMap;

            /// <summary>
            /// Indicates that material requries using pixel shader stage during depth-only pass (Z prepass or shadow map rendering).
            /// Used by transparent and cut off materials.
            /// </summary>
            public bool UsePixelShaderWithDepthPass;

            public MaterialInfo(MaterialPass materialPass)
            {
                MaterialPass = materialPass;
                CullMode = materialPass.CullMode;
            }
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
            tessellationStateKey = RootRenderFeature.RenderData.CreateStaticObjectKey<TessellationState>();

            perMaterialDescriptorSetSlot = ((RootEffectRenderFeature)RootRenderFeature).GetOrCreateEffectDescriptorSetSlot("PerMaterial");
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            var tessellationStates = RootRenderFeature.RenderData.GetData(tessellationStateKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            Dispatcher.ForEach(RootRenderFeature.RenderObjects, renderObject =>
            {
                var staticObjectNode = renderObject.StaticObjectNode;

                var renderMesh = (RenderMesh)renderObject;
                bool resetPipelineState = false;

                var material = renderMesh.MaterialPass;
                var materialInfo = renderMesh.MaterialInfo;

                // Material use first 16 bits
                var materialHashCode = material != null ? ((uint)material.GetHashCode() & 0x0FFF) | ((uint)material.PassIndex << 12) : 0;
                renderObject.StateSortKey = (renderObject.StateSortKey & 0x0000FFFF) | (materialHashCode << 16);

                ref var tessellationState = ref tessellationStates[staticObjectNode];

                // Update draw data if tessellation is active
                if (material.TessellationMethod != StrideTessellationMethod.None)
                {
                    var tessellationMeshDraw = tessellationState.MeshDraw;

                    if (tessellationState.Method != material.TessellationMethod)
                    {
                        tessellationState.Method = material.TessellationMethod;

                        var oldMeshDraw = renderMesh.ActiveMeshDraw;
                        tessellationMeshDraw = new MeshDraw
                        {
                            VertexBuffers = oldMeshDraw.VertexBuffers,
                            IndexBuffer = oldMeshDraw.IndexBuffer,
                            DrawCount = oldMeshDraw.DrawCount,
                            StartLocation = oldMeshDraw.StartLocation,
                            PrimitiveType = tessellationState.Method.GetPrimitiveType(),
                        };

                        // adapt the primitive type and index buffer to the tessellation used
                        if (tessellationState.Method.PerformsAdjacentEdgeAverage())
                        {
                            renderMeshesToGenerateAEN.Add(renderMesh);
                        }
                        else
                        {
                            // Not using AEN tessellation anymore, dispose AEN indices if they were generated
                            Utilities.Dispose(ref tessellationState.GeneratedIndicesAEN);
                        }
                        tessellationState.MeshDraw = tessellationMeshDraw;

                        // Reset pipeline states
                        resetPipelineState = true;
                    }

                    renderMesh.ActiveMeshDraw = tessellationState.MeshDraw;
                }
                else if (tessellationState.GeneratedIndicesAEN != null)
                {
                    // Not using tessellation anymore, dispose AEN indices if they were generated
                    Utilities.Dispose(ref tessellationState.GeneratedIndicesAEN);
                }

                // Rebuild rasterizer state if culling mode changed
                // TODO GRAPHICS REFACTOR: Negative scaling belongs into TransformationRenderFeature
                if (materialInfo != null && (materialInfo.CullMode != material.CullMode || renderMesh.IsScalingNegative != renderMesh.IsPreviousScalingNegative))
                {
                    materialInfo.CullMode = material.CullMode;
                    renderMesh.IsPreviousScalingNegative = renderMesh.IsScalingNegative;
                    resetPipelineState = true;
                }

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    if (renderEffect == null)
                        continue;

                    // If any pipeline state changed, rebuild it for all effect slots
                    if (resetPipelineState)
                        renderEffect.PipelineState = null;

                    // Skip effects not used during this frame
                    if (!renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    if (materialInfo == null || materialInfo.MaterialPass != material)
                    {
                        // First time this material is initialized, let's create associated info
                        lock (allMaterialInfos)
                        {
                            if (!allMaterialInfos.TryGetValue(material, out materialInfo))
                            {
                                materialInfo = new MaterialInfo(material);
                                allMaterialInfos.Add(material, materialInfo);
                            }
                        }
                        renderMesh.MaterialInfo = materialInfo;
                    }

                    if (materialInfo.MaterialParameters != material.Parameters || materialInfo.PermutationCounter != material.Parameters.PermutationCounter)
                    {
                        lock (materialInfo)
                        {
                            var isMaterialParametersChanged = materialInfo.MaterialParameters != material.Parameters;
                            if (isMaterialParametersChanged // parameter fast reload?
                                || materialInfo.PermutationCounter != material.Parameters.PermutationCounter)
                            {
                                materialInfo.VertexStageSurfaceShaders = material.Parameters.Get(MaterialKeys.VertexStageSurfaceShaders);
                                materialInfo.VertexStageStreamInitializer = material.Parameters.Get(MaterialKeys.VertexStageStreamInitializer);

                                materialInfo.DomainStageSurfaceShaders = material.Parameters.Get(MaterialKeys.DomainStageSurfaceShaders);
                                materialInfo.DomainStageStreamInitializer = material.Parameters.Get(MaterialKeys.DomainStageStreamInitializer);

                                materialInfo.TessellationShader = material.Parameters.Get(MaterialKeys.TessellationShader);

                                materialInfo.PixelStageSurfaceShaders = material.Parameters.Get(MaterialKeys.PixelStageSurfaceShaders);
                                materialInfo.PixelStageStreamInitializer = material.Parameters.Get(MaterialKeys.PixelStageStreamInitializer);
                                materialInfo.HasNormalMap = material.Parameters.Get(MaterialKeys.HasNormalMap);
                                materialInfo.UsePixelShaderWithDepthPass = material.Parameters.Get(MaterialKeys.UsePixelShaderWithDepthPass);

                                materialInfo.MaterialParameters = material.Parameters;
                                materialInfo.ParametersChanged = isMaterialParametersChanged;
                                materialInfo.PermutationCounter = material.Parameters.PermutationCounter;
                            }
                        }
                    }

                    // VS
                    if (materialInfo.VertexStageSurfaceShaders != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.VertexStageSurfaceShaders, materialInfo.VertexStageSurfaceShaders);
                    if (materialInfo.VertexStageStreamInitializer != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.VertexStageStreamInitializer, materialInfo.VertexStageStreamInitializer);

                    // DS
                    if (materialInfo.DomainStageSurfaceShaders != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.DomainStageSurfaceShaders, materialInfo.DomainStageSurfaceShaders);
                    if (materialInfo.DomainStageStreamInitializer != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.DomainStageStreamInitializer, materialInfo.DomainStageStreamInitializer);

                    // Tessellation
                    if (materialInfo.TessellationShader != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.TessellationShader, materialInfo.TessellationShader);

                    // PS
                    if (materialInfo.PixelStageSurfaceShaders != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.PixelStageSurfaceShaders, materialInfo.PixelStageSurfaceShaders);
                    if (materialInfo.PixelStageStreamInitializer != null)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.PixelStageStreamInitializer, materialInfo.PixelStageStreamInitializer);
                    if (materialInfo.HasNormalMap)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.HasNormalMap, materialInfo.HasNormalMap);
                    if (materialInfo.UsePixelShaderWithDepthPass)
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.UsePixelShaderWithDepthPass, materialInfo.UsePixelShaderWithDepthPass);
                }
            });

            renderMeshesToGenerateAEN.Close();
        }

        /// <inheritdoc/>
        public override void Prepare(RenderDrawContext context)
        {
            // Assign descriptor sets to each render node
            var resourceGroupPool = ((RootEffectRenderFeature)RootRenderFeature).ResourceGroupPool;

            Dispatcher.For(0, RootRenderFeature.RenderNodes.Count, () => context.RenderContext.GetThreadContext(), (renderNodeIndex, threadContext) =>
            {
                var renderNodeReference = new RenderNodeReference(renderNodeIndex);
                var renderNode = RootRenderFeature.RenderNodes[renderNodeIndex];
                var renderMesh = (RenderMesh)renderNode.RenderObject;

                // Ignore fallback effects
                if (renderNode.RenderEffect.State != RenderEffectState.Normal)
                    return;

                // Collect materials and create associated MaterialInfo (includes reflection) first time
                // TODO: We assume same material will generate same ResourceGroup (i.e. same resources declared in same order)
                // Need to offer some protection if this invariant is violated (or support it if it can actually happen in real scenario)
                var material = renderMesh.MaterialPass;
                var materialInfo = renderMesh.MaterialInfo;
                var materialParameters = material.Parameters;

                // Register resources usage
                Context.StreamingManager?.StreamResources(materialParameters);

                if (!UpdateMaterial(RenderSystem, threadContext, materialInfo, perMaterialDescriptorSetSlot.Index, renderNode.RenderEffect, materialParameters))
                    return;

                var descriptorSetPoolOffset = ((RootEffectRenderFeature)RootRenderFeature).ComputeResourceGroupOffset(renderNodeReference);
                resourceGroupPool[descriptorSetPoolOffset + perMaterialDescriptorSetSlot.Index] = materialInfo.Resources;
            });
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            if (renderMeshesToGenerateAEN.Count > 0)
            {
                var tessellationStates = RootRenderFeature.RenderData.GetData(tessellationStateKey);

                foreach (var renderMesh in renderMeshesToGenerateAEN)
                {
                    var tessellationState = tessellationStates[renderMesh.StaticObjectNode];
                    if (tessellationState.GeneratedIndicesAEN != null)
                        continue;

                    var tessellationMeshDraw = tessellationState.MeshDraw;

                    var indicesAEN = IndexExtensions.GenerateIndexBufferAEN(tessellationMeshDraw.IndexBuffer, tessellationMeshDraw.VertexBuffers[0], context.CommandList);
                    tessellationState.GeneratedIndicesAEN = Buffer.Index.New(Context.GraphicsDevice, indicesAEN);
                    tessellationMeshDraw.IndexBuffer = new IndexBufferBinding(tessellationState.GeneratedIndicesAEN, true, tessellationMeshDraw.IndexBuffer.Count * 12 / 3);
                    tessellationMeshDraw.DrawCount = 12 / 3 * tessellationMeshDraw.DrawCount;
                }

                renderMeshesToGenerateAEN.Clear(false);
            }
        }

        public static unsafe bool UpdateMaterial(RenderSystem renderSystem, RenderDrawContext context, MaterialInfoBase materialInfo, int materialSlotIndex, RenderEffect renderEffect, ParameterCollection materialParameters)
        {
            var resourceGroupDescription = renderEffect.Reflection.ResourceGroupDescriptions[materialSlotIndex];
            if (resourceGroupDescription.DescriptorSetLayout == null)
                return false;

            // Check if this material was encountered for the first time this frame and mark it as used
            if (Interlocked.Exchange(ref materialInfo.LastFrameUsed, renderSystem.FrameCounter) == renderSystem.FrameCounter)
                return true;

            // First time we use the material with a valid effect, let's update layouts
            if (materialInfo.PerMaterialLayout == null || materialInfo.PerMaterialLayout.Hash != renderEffect.Reflection.ResourceGroupDescriptions[materialSlotIndex].Hash)
            {
                materialInfo.PerMaterialLayout = ResourceGroupLayout.New(renderSystem.GraphicsDevice, resourceGroupDescription, renderEffect.Effect.Bytecode);

                var parameterCollectionLayout = materialInfo.ParameterCollectionLayout = new ParameterCollectionLayout();
                parameterCollectionLayout.ProcessResources(resourceGroupDescription.DescriptorSetLayout);
                materialInfo.ResourceCount = parameterCollectionLayout.ResourceCount;

                // Process material cbuffer (if any)
                if (resourceGroupDescription.ConstantBufferReflection != null)
                {
                    materialInfo.ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection;
                    parameterCollectionLayout.ProcessConstantBuffer(resourceGroupDescription.ConstantBufferReflection);
                }
                materialInfo.ParametersChanged = true;
            }

            // If the parameters collection instance changed, we need to update it
            if (materialInfo.ParametersChanged)
            {
                materialInfo.ParameterCollection.UpdateLayout(materialInfo.ParameterCollectionLayout);
                materialInfo.ParameterCollectionCopier = new ParameterCollection.Copier(materialInfo.ParameterCollection, materialParameters);
                materialInfo.ParametersChanged = false;
            }

            // Copy back to ParameterCollection
            // TODO GRAPHICS REFACTOR directly copy to resource group?
            materialInfo.ParameterCollectionCopier.Copy();

            // Allocate resource groups
            context.ResourceGroupAllocator.PrepareResourceGroup(materialInfo.PerMaterialLayout, BufferPoolAllocationType.UsedMultipleTime, materialInfo.Resources);

            // Set resource bindings in PerMaterial resource set
            for (int resourceSlot = 0; resourceSlot < materialInfo.ResourceCount; ++resourceSlot)
            {
                materialInfo.Resources.DescriptorSet.SetValue(resourceSlot, materialInfo.ParameterCollection.ObjectValues[resourceSlot]);
            }

            // Process PerMaterial cbuffer
            if (materialInfo.ConstantBufferReflection != null)
            {
                var mappedCB = materialInfo.Resources.ConstantBuffer.Data;
                fixed (byte* dataValues = materialInfo.ParameterCollection.DataValues)
                    Utilities.CopyMemory(mappedCB, (IntPtr)dataValues, materialInfo.Resources.ConstantBuffer.Size);
            }

            return true;
        }

        private struct TessellationState : IDisposable
        {
            public StrideTessellationMethod Method;
            public Buffer GeneratedIndicesAEN;
            public MeshDraw MeshDraw;

            public void Dispose()
            {
                if (GeneratedIndicesAEN != null)
                {
                    GeneratedIndicesAEN.Dispose();
                    GeneratedIndicesAEN = null;
                }
            }
        }
    }
}
