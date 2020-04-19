// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.LightProbes;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.Compositing
{
    partial class ForwardRenderer
    {
        private DynamicEffectInstance bakeLightProbes;
        private MutablePipelineState bakeLightProbesPipeline;

        private unsafe void PrepareLightprobeConstantBuffer(RenderContext context)
        {
            var renderView = context.RenderView;
            var lightProbesData = context.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
            if (lightProbesData != null)
            {
                foreach (var renderFeature in context.RenderSystem.RenderFeatures)
                {
                    if (!(renderFeature is RootEffectRenderFeature))
                        continue;

                    var logicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("LightProbes");
                    var viewFeature = renderView.Features[renderFeature.Index];

                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                        var logicalGroup = viewLayout.GetLogicalGroup(logicalKey);
                        if (logicalGroup.Hash == ObjectId.Empty)
                            continue;

                        var mappedCB = (LightProbeCBuffer*)(resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset);
                        mappedCB->UserVertexCount = lightProbesData.UserVertexCount;
                    }
                }
            }
        }

        /// <summary>
        /// Bake lightprobes into buffers compatible with <see cref="LightProbeRenderer"/>
        /// </summary>
        /// <param name="drawContext">The drawing context</param>
        private unsafe void BakeLightProbes(RenderContext context, RenderDrawContext drawContext)
        {
            Texture ibl = null;
            Buffer tetrahedronProbeIndices = null;
            Buffer tetrahedronMatrices = null;
            Buffer lightprobesCoefficients = null;
            var renderView = context.RenderView;

            var lightProbesData = context.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
            if (lightProbesData == null || lightProbesData.Tetrahedra.Count == 0)
            {
                // No lightprobes, we still set GPU resources (otherwise rendering might fetch invalid data)
                goto SetGPUResources;
            }

            // First time initialization
            if (bakeLightProbes == null)
            {
                bakeLightProbes = new DynamicEffectInstance("StrideBakeLightProbeEffect");
                bakeLightProbes.Initialize(Services);

                bakeLightProbesPipeline = new MutablePipelineState(GraphicsDevice);
                bakeLightProbesPipeline.State.InputElements = LightProbeVertex.Layout.CreateInputElements();
                bakeLightProbesPipeline.State.PrimitiveType = PrimitiveType.TriangleList;
            }

            // Render IBL tetrahedra ID so that we can assign them per pixel
            //ibl = PushScopedResource(Context.Allocator.GetTemporaryTexture2D(drawContext.CommandList.DepthStencilBuffer.Width, drawContext.CommandList.DepthStencilBuffer.Height, PixelFormat.R16_UInt));
            ibl = PushScopedResource(Context.Allocator.GetTemporaryTexture2D(TextureDescription.New2D(drawContext.CommandList.DepthStencilBuffer.Width, drawContext.CommandList.DepthStencilBuffer.Height,
                        1, PixelFormat.R16_UInt, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 1, GraphicsResourceUsage.Default, actualMultisampleCount)));
            using (drawContext.PushRenderTargetsAndRestore())
            {
                drawContext.CommandList.Clear(ibl, Color4.Black);
                drawContext.CommandList.SetRenderTarget(drawContext.CommandList.DepthStencilBuffer, ibl);

                bakeLightProbes.UpdateEffect(GraphicsDevice);

                bakeLightProbesPipeline.State.RootSignature = bakeLightProbes.RootSignature;
                bakeLightProbesPipeline.State.EffectBytecode = bakeLightProbes.Effect.Bytecode;
                bakeLightProbesPipeline.State.RasterizerState.DepthClipEnable = false;
                bakeLightProbesPipeline.State.DepthStencilState = new DepthStencilStateDescription(true, false)
                {
                    StencilEnable = true,
                    FrontFace = new DepthStencilStencilOpDescription
                    {
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Increment,
                        StencilFunction = CompareFunction.Equal,
                    },
                };
                //bakeLightProbesPipeline.State.RasterizerState.DepthClipEnable = false;
                bakeLightProbesPipeline.State.Output.CaptureState(drawContext.CommandList);
                bakeLightProbesPipeline.Update();

                drawContext.CommandList.SetPipelineState(bakeLightProbesPipeline.CurrentState);
                drawContext.CommandList.SetStencilReference(0);

                // Apply the effect
                bakeLightProbes.Parameters.Set(BakeLightProbeShaderKeys.MatrixTransform, ref renderView.ViewProjection);
                bakeLightProbes.Apply(drawContext.GraphicsContext);

                /*int tetrahedrawGridSize = 5;
                Vector3 tetrahedraMin = new Vector3(-12.0f);
                Vector3 tetrahedraMax = new Vector3(12.0f);
                var lightprobePositions = new Vector3[tetrahedrawGridSize*tetrahedrawGridSize*tetrahedrawGridSize];

                for (int i = 0; i < lightprobePositions.Length; ++i)
                {
                    lightprobePositions[i] = new Vector3(
                        MathUtil.Lerp(tetrahedraMin.X, tetrahedraMax.X, (float)(i/(tetrahedrawGridSize*tetrahedrawGridSize))/(tetrahedrawGridSize - 1)),
                        MathUtil.Lerp(tetrahedraMin.Y, tetrahedraMax.Y, (float)((i/tetrahedrawGridSize)%tetrahedrawGridSize)/(tetrahedrawGridSize - 1)),
                        MathUtil.Lerp(tetrahedraMin.Z, tetrahedraMax.Z, (float)(i%tetrahedrawGridSize)/(tetrahedrawGridSize - 1)));
                }

                var tetra = new BowyerWatsonTetrahedralization();
                var tetraResult = tetra.Compute(lightprobePositions);*/

                Matrix.Invert(ref renderView.View, out var viewInverse);

                var eye = new Vector3(viewInverse.M41, viewInverse.M42, viewInverse.M43);

                var tetraResult = lightProbesData.Tetrahedra;
                var lightprobePositions = lightProbesData.Vertices;
                var lightprobeFaces = lightProbesData.Faces;

                // We build a graph of tetrahedron connectivity from back to front, then do a topological sort on top of it
                var tetraDepth = new TetrahedronSortKey[tetraResult.Count];
                var faceDirection = new bool[lightprobeFaces.Count];
                var incomingEdges = new byte[tetraResult.Count];
                var processQueue = new Queue<int>();
                int processedTetrahedra = 0;

                for (int i = 0; i < lightprobeFaces.Count; ++i)
                {
                    var face = lightprobeFaces[i];

                    // Compute face orientations
                    var vertex0 = lightprobePositions[face.Vertices[0]];
                    Vector3.Subtract(ref vertex0, ref eye, out vertex0);
                    bool faceFrontFacing = Vector3.Dot(face.Normal, vertex0) >= 0.0f;
                    faceDirection[i] = faceFrontFacing;

                    // Only process edges that connect two tetrahedra (ignore boundaries for now)
                    if (face.BackTetrahedron != -1)
                    {
                        // Build list of incoming edges (back to front)
                        if (faceFrontFacing)
                            incomingEdges[face.FrontTetrahedron] |= (byte)(1 << face.FrontFace);
                        else
                            incomingEdges[face.BackTetrahedron] |= (byte)(1 << face.BackFace);
                    }
                }

                for (int i = 0; i < tetraResult.Count; ++i)
                {
                    // Tetrahedron without any incoming edges means they should be drawn first (graph nodes with no incoming edges for our topological sort)
                    if (incomingEdges[i] == 0)
                    {
                        processQueue.Enqueue(i);
                    }
                }

                // Perform topological sort
                while (processQueue.Count > 0)
                {
                    var tetrahedronIndex = processQueue.Dequeue();
                    tetraDepth[tetrahedronIndex] = new TetrahedronSortKey(tetrahedronIndex, processedTetrahedra++);
                    var tetrahedron = tetraResult[tetrahedronIndex];
                    //var frontFacingFaces = frontFacing[tetrahedronIndex];

                    // Process each outgoing face (edges in the graph)
                    for (int tetrahedronFace = 0; tetrahedronFace < 4; ++tetrahedronFace)
                    {
                        // Check if there is a neighbour
                        if (tetrahedron.Neighbours[tetrahedronFace] == -1)
                            continue;

                        var faceIndex = tetrahedron.Faces[tetrahedronFace];
                        var realFaceIndex = faceIndex >= 0 ? faceIndex : ~faceIndex;

                        // Only process faces going back to front (outgoing edges)
                        if (faceDirection[realFaceIndex] == faceIndex >= 0)
                            continue;

                        var face = lightprobeFaces[realFaceIndex];

                        int tetrahedronNeighbourIndex;
                        sbyte tetrahedronNeighbourFace;
                        if (faceIndex >= 0)
                        {
                            tetrahedronNeighbourIndex = face.BackTetrahedron;
                            tetrahedronNeighbourFace = face.BackFace;
                        }
                        else
                        {
                            tetrahedronNeighbourIndex = face.FrontTetrahedron;
                            tetrahedronNeighbourFace = face.FrontFace;
                        }

                        var neighbourTraversedFaces = incomingEdges[tetrahedronNeighbourIndex];
                        var newNeighbourTraversedFaces = (byte)(neighbourTraversedFaces & ~(1 << tetrahedronNeighbourFace));

                        // Proceed only if something changed
                        if (newNeighbourTraversedFaces != neighbourTraversedFaces)
                        {
                            incomingEdges[tetrahedronNeighbourIndex] = newNeighbourTraversedFaces;
                            if (newNeighbourTraversedFaces == 0) // are all incoming edges already marked? If yes, go on
                            {
                                processQueue.Enqueue(tetrahedronNeighbourIndex);
                            }
                        }
                    }
                }

                Array.Sort(tetraDepth);

                // Draw shape
                tetrahedronMatrices = PushScopedResource(Context.Allocator.GetTemporaryBuffer(new BufferDescription(tetraResult.Count * 3 * sizeof(Vector4), BufferFlags.ShaderResource, GraphicsResourceUsage.Default), PixelFormat.R32G32B32A32_Float));
                tetrahedronProbeIndices = PushScopedResource(Context.Allocator.GetTemporaryBuffer(new BufferDescription(tetraResult.Count * 4 * sizeof(int), BufferFlags.ShaderResource, GraphicsResourceUsage.Default), PixelFormat.R32G32B32A32_UInt));
                lightprobesCoefficients = PushScopedResource(Context.Allocator.GetTemporaryBuffer(new BufferDescription(lightProbesData.Coefficients.Length * sizeof(Color3), BufferFlags.ShaderResource, GraphicsResourceUsage.Default), PixelFormat.R32G32B32_Float));

                var tetraInsideIndex = -1;

                fixed (Color3* lightProbeCoefficients = lightProbesData.Coefficients)
                fixed (Vector4* matrices = lightProbesData.Matrices)
                fixed (Int4* probeIndices = lightProbesData.LightProbeIndices)
                {
                    drawContext.CommandList.UpdateSubresource(lightprobesCoefficients, 0, new DataBox((IntPtr)lightProbeCoefficients, 0, 0));
                    drawContext.CommandList.UpdateSubresource(tetrahedronProbeIndices, 0, new DataBox((IntPtr)probeIndices, 0, 0));
                    drawContext.CommandList.UpdateSubresource(tetrahedronMatrices, 0, new DataBox((IntPtr)matrices, 0, 0));

                    // Find which probe we are currently in
                    // TODO: Optimize (use previous coherency info?)
                    for (int i = 0; i < tetraResult.Count; ++i)
                    {
                        // Get tetrahedra matrix
                        var tetrahedraMatrix = Matrix.Identity;
                        tetrahedraMatrix.Column1 = matrices[i * 3 + 0];
                        tetrahedraMatrix.Column2 = matrices[i * 3 + 1];
                        tetrahedraMatrix.Column3 = matrices[i * 3 + 2];

                        // Extract and zero-out position of 3rd vertex (we get the 3x3 matrix)
                        var vertex3 = tetrahedraMatrix.TranslationVector;
                        tetrahedraMatrix.TranslationVector = Vector3.Zero;

                        Vector3 tetraFactors = Vector3.TransformCoordinate(eye - vertex3, tetrahedraMatrix);
                        var tetraFactorW = 1.0f - tetraFactors.X - tetraFactors.Y - tetraFactors.Z;
                        if (tetraFactors.X >= 0.0f && tetraFactors.X <= 1.0f
                            && tetraFactors.Y >= 0.0f && tetraFactors.Y <= 1.0f
                            && tetraFactors.Z >= 0.0f && tetraFactors.Z <= 1.0f
                            && tetraFactorW >= 0.0f && tetraFactorW <= 1.0f)
                        {
                            tetraInsideIndex = i;
                            break;
                        }
                    }
                }

                // Fill vertex/index buffers
                var vertexBuffer = PushScopedResource(Context.Allocator.GetTemporaryBuffer(new BufferDescription((tetraResult.Count * 4 + 3) * LightProbeVertex.Size, BufferFlags.VertexBuffer, GraphicsResourceUsage.Dynamic)));
                var indexBuffer = PushScopedResource(Context.Allocator.GetTemporaryBuffer(new BufferDescription(tetraResult.Count * 12 * sizeof(uint), BufferFlags.IndexBuffer, GraphicsResourceUsage.Dynamic)));

                var mappedVertexBuffer = drawContext.CommandList.MapSubresource(vertexBuffer, 0, MapMode.WriteDiscard);
                var vertices = (LightProbeVertex*)mappedVertexBuffer.DataBox.DataPointer;
                // Upload sorted tetrahedron indices
                for (int i = 0; i < tetraResult.Count; ++i)
                {
                    var sortedIndex = tetraDepth[i].Index;
                    var tetrahedra = tetraResult[sortedIndex];
                    for (int j = 0; j < 4; ++j)
                        vertices[i * 4 + j] = new LightProbeVertex(lightprobePositions[tetrahedra.Vertices[j]], (uint)sortedIndex);
                }
                // Full screen pass
                if (tetraInsideIndex != -1)
                {
                    vertices[tetraResult.Count * 4 + 0] = new LightProbeVertex(new Vector3(-1, 1, 0), (uint)tetraInsideIndex);
                    vertices[tetraResult.Count * 4 + 1] = new LightProbeVertex(new Vector3(3, 1, 0), (uint)tetraInsideIndex);
                    vertices[tetraResult.Count * 4 + 2] = new LightProbeVertex(new Vector3(-1, -3, 0), (uint)tetraInsideIndex);
                }
                drawContext.CommandList.UnmapSubresource(mappedVertexBuffer);

                var mappedIndexBuffer = drawContext.CommandList.MapSubresource(indexBuffer, 0, MapMode.WriteDiscard);
                var indices = (int*)mappedIndexBuffer.DataBox.DataPointer;
                for (int i = 0; i < tetraResult.Count; ++i)
                {
                    indices[i * 12 + 0] = i * 4 + 0;
                    indices[i * 12 + 1] = i * 4 + 2;
                    indices[i * 12 + 2] = i * 4 + 1;

                    indices[i * 12 + 3] = i * 4 + 1;
                    indices[i * 12 + 4] = i * 4 + 2;
                    indices[i * 12 + 5] = i * 4 + 3;

                    indices[i * 12 + 6] = i * 4 + 3;
                    indices[i * 12 + 7] = i * 4 + 2;
                    indices[i * 12 + 8] = i * 4 + 0;

                    indices[i * 12 + 9] = i * 4 + 3;
                    indices[i * 12 + 10] = i * 4 + 0;
                    indices[i * 12 + 11] = i * 4 + 1;
                }
                drawContext.CommandList.UnmapSubresource(mappedIndexBuffer);

                drawContext.CommandList.SetVertexBuffer(0, vertexBuffer, 0, LightProbeVertex.Size);
                drawContext.CommandList.SetIndexBuffer(indexBuffer, 0, true);

                // Draw until current tetrahedra
                drawContext.CommandList.DrawIndexed(tetraResult.Count * 12);

                // For now, drawing them one by one (easier to debug)
                //for (int i = 0; i < tetraResult.Count; ++i)
                //{
                //    context.CommandList.DrawIndexed(12, i * 12);
                //}

                // Draw current tetrahedron we are in as full screen (fill stencil holes)
                if (tetraInsideIndex != -1)
                {
                    bakeLightProbesPipeline.State.DepthStencilState.DepthBufferEnable = false;
                    bakeLightProbesPipeline.Update();

                    drawContext.CommandList.SetPipelineState(bakeLightProbesPipeline.CurrentState);

                    // Apply the effect
                    bakeLightProbes.Parameters.Set(BakeLightProbeShaderKeys.MatrixTransform, Matrix.Identity);
                    bakeLightProbes.Apply(drawContext.GraphicsContext);

                    drawContext.CommandList.Draw(3, tetraResult.Count * 4);
                }

                // TODO: Draw the tetrahedron we are in full screen
                // context.CommandList.Draw...
            }

            // Set LightProbes resources
        SetGPUResources:
            foreach (var renderFeature in context.RenderSystem.RenderFeatures)
            {
                if (!(renderFeature is RootEffectRenderFeature))
                    continue;

                var logicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("LightProbes");
                var viewFeature = renderView.Features[renderFeature.Index];

                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                    var logicalGroup = viewLayout.GetLogicalGroup(logicalKey);
                    if (logicalGroup.Hash == ObjectId.Empty)
                        continue;

                    resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart, ibl);
                    resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart + 1, tetrahedronProbeIndices);
                    resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart + 2, tetrahedronMatrices);
                    resourceGroup.DescriptorSet.SetShaderResourceView(logicalGroup.DescriptorSlotStart + 3, lightprobesCoefficients);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LightProbeCBuffer
        {
            public int UserVertexCount;
        }
    }
}
