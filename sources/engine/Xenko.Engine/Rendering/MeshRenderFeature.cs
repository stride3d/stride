// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Core.Threading;
using Xenko.Graphics;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Rendering
{
    /// <summary>
    /// Renders <see cref="RenderMesh"/>.
    /// </summary>
    public class MeshRenderFeature : RootEffectRenderFeature
    {
        private readonly ThreadLocal<DescriptorSet[]> descriptorSets = new ThreadLocal<DescriptorSet[]>();

        private Buffer emptyBuffer;

        /// <summary>
        /// Lists of sub render features that can be applied on <see cref="RenderMesh"/>.
        /// </summary>
        [DataMember]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public TrackingCollection<SubRenderFeature> RenderFeatures = new TrackingCollection<SubRenderFeature>();

        /// <inheritdoc/>
        public override Type SupportedRenderObjectType => typeof(RenderMesh);

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            RenderFeatures.CollectionChanged += RenderFeatures_CollectionChanged;

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.AttachRootRenderFeature(this);
                renderFeature.Initialize(Context);
            }

            // Create an empty buffer to compensate for missing vertex streams
            emptyBuffer = Buffer.Vertex.New(Context.GraphicsDevice, new Vector4[1]);
        }

        protected override void Destroy()
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Dispose();
            }

            RenderFeatures.CollectionChanged -= RenderFeatures_CollectionChanged;

            descriptorSets.Dispose();

            emptyBuffer?.Dispose();
            emptyBuffer = null;

            base.Destroy();
        }

        /// <inheritdoc/>
        public override void Collect()
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Collect();
            }
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Extract();
            }
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public override void PrepareEffectPermutationsImpl(RenderDrawContext context)
        {
            // Setup ActiveMeshDraw
            Dispatcher.ForEach(RenderObjects, renderObject =>
            {
                var renderMesh = (RenderMesh)renderObject;

                renderMesh.ActiveMeshDraw = renderMesh.Mesh.Draw;
            });

            base.PrepareEffectPermutationsImpl(context);

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.PrepareEffectPermutations(context);
            }
        }

        /// <inheritdoc/>
        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);

            // Prepare each sub render feature
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Prepare(context);
            }
        }

        protected override void ProcessPipelineState(RenderContext context, RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            var renderMesh = (RenderMesh)renderObject;
            var drawData = renderMesh.ActiveMeshDraw;

            pipelineState.InputElements = PrepareInputElements(pipelineState, drawData);
            pipelineState.PrimitiveType = drawData.PrimitiveType;

            // Prepare each sub render feature
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.ProcessPipelineState(context, renderNodeReference, ref renderNode, renderObject, pipelineState);
            }
        }

        /// <inheritdoc/>
        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Draw(context, renderView, renderViewStage);
            }
        }

        /// <inheritdoc/>
        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            var commandList = context.CommandList;

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Draw(context, renderView, renderViewStage, startIndex, endIndex);
            }

            // TODO: stackalloc?
            var descriptorSetsLocal = descriptorSets.Value;
            if (descriptorSetsLocal == null || descriptorSetsLocal.Length < EffectDescriptorSetSlotCount)
            {
                descriptorSetsLocal = descriptorSets.Value = new DescriptorSet[EffectDescriptorSetSlotCount];
            }
            
            MeshDraw currentDrawData = null;
            int emptyBufferSlot = -1;
            for (int index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);

                var renderMesh = (RenderMesh)renderNode.RenderObject;
                var drawData = renderMesh.ActiveMeshDraw;

                // Get effect
                // TODO: Use real effect slot
                var renderEffect = renderNode.RenderEffect;
                if (renderEffect.Effect == null)
                    continue;

                // Bind VB
                if (currentDrawData != drawData)
                {
                    for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
                    {
                        var vertexBuffer = drawData.VertexBuffers[slot];
                        commandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
                    }

                    // If the mesh's vertex buffers miss any input streams, an additional input binding will have been added to the pipeline state.
                    // We bind an additional empty vertex buffer to that slot handle those streams gracefully.
                    if (emptyBufferSlot != drawData.VertexBuffers.Length)
                    {
                        commandList.SetVertexBuffer(drawData.VertexBuffers.Length, emptyBuffer, 0, 0);
                        emptyBufferSlot = drawData.VertexBuffers.Length;
                    }

                    if (drawData.IndexBuffer != null)
                        commandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
                    currentDrawData = drawData;
                }

                var resourceGroupOffset = ComputeResourceGroupOffset(renderNodeReference);
                
                // Update cbuffer
                renderEffect.Reflection.BufferUploader.Apply(context.CommandList, ResourceGroupPool, resourceGroupOffset);

                // Bind descriptor sets
                for (int i = 0; i < descriptorSetsLocal.Length; ++i)
                {
                    var resourceGroup = ResourceGroupPool[resourceGroupOffset++];
                    if (resourceGroup != null)
                        descriptorSetsLocal[i] = resourceGroup.DescriptorSet;
                }

                commandList.SetPipelineState(renderEffect.PipelineState);
                commandList.SetDescriptorSets(0, descriptorSetsLocal);

                // Draw
                if (drawData.IndexBuffer == null)
                {
                    commandList.Draw(drawData.DrawCount, drawData.StartLocation);
                }
                else
                {
                    commandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                }
            }
        }

        /// <inheritdoc/>
        public override void Flush(RenderDrawContext context)
        {
            base.Flush(context);

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Flush(context);
            }
        }

        private void RenderFeatures_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            var renderFeature = (SubRenderFeature)e.Item;

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    renderFeature.AttachRootRenderFeature(this);
                    renderFeature.Initialize(Context);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    renderFeature.Dispose();
                    break;
            }
        }

        private InputElementDescription[] PrepareInputElements(PipelineStateDescription pipelineState, MeshDraw drawData)
        {
            // Get the input elements already contained in the mesh's vertex buffers
            var availableInputElements = drawData.VertexBuffers.CreateInputElements();
            var inputElements = new List<InputElementDescription>(availableInputElements);

            // In addition, add input elements for all attributes that are not contained in a bound buffer, but required by the shader
            foreach (var inputAttribute in pipelineState.EffectBytecode.Reflection.InputAttributes)
            {
                var inputElementIndex = FindElementBySemantic(availableInputElements, inputAttribute.SemanticName, inputAttribute.SemanticIndex);
                
                // Provided by any vertex buffer?
                if (inputElementIndex >= 0)
                    continue;
                
                inputElements.Add(new InputElementDescription
                {
                    AlignedByteOffset = 0,
                    Format = PixelFormat.R32G32B32A32_Float,
                    InputSlot = drawData.VertexBuffers.Length,
                    InputSlotClass = InputClassification.Vertex,
                    InstanceDataStepRate = 0,
                    SemanticIndex = inputAttribute.SemanticIndex,
                    SemanticName = inputAttribute.SemanticName,
                });
            }

            return inputElements.ToArray();
        }

        private static int FindElementBySemantic(InputElementDescription[] inputElements, string semanticName, int semanticIndex)
        {
            int foundDescIndex = -1;
            for (int index = 0; index < inputElements.Length; index++)
            {
                if (semanticName == inputElements[index].SemanticName && semanticIndex == inputElements[index].SemanticIndex)
                    foundDescIndex = index;
            }

            return foundDescIndex;
        }
    }
}
