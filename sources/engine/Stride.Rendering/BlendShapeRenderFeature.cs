// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Silk.NET.SDL;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Rendering.Materials;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Computes and uploads Blendshape info.
    /// </summary>
    public class BlendShapeRenderFeature : SubRenderFeature
    {
        public override unsafe void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            base.Draw(context, renderView, renderViewStage, startIndex, endIndex);

            if (context == null || context.CommandList == null || context.CommandList.IsDisposed) { return; }

            for (int index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = RootRenderFeature.GetRenderNode(renderNodeReference);
                var renderMesh = (RenderMesh)renderNode.RenderObject;            
                if (renderMesh==null||renderMesh.HasBlendShapes==false|| renderMesh.ActiveMeshDraw == null || renderMesh.ActiveMeshDraw.VertexData == null) { return; }

                var drawData = renderMesh.ActiveMeshDraw;

                var renderEffect = renderNode.RenderEffect;
                if (renderEffect.Effect == null)
                    continue;

                for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
                {
                    var vertexBuffer = drawData.VertexBuffers[slot];
                    var mappedVertices = context.CommandList.MapSubresource(vertexBuffer.Buffer, 0, Graphics.MapMode.WriteDiscard);
                    var pointer = (byte*)mappedVertices.DataBox.DataPointer;
                    if (drawData != null)
                    {
                        fixed (byte* matPtr = drawData.VertexData)
                        {
                            Unsafe.CopyBlockUnaligned(pointer, matPtr, (uint)drawData.VertexData.Length);
                        }
                        context.CommandList.UnmapSubresource(mappedVertices);
                    }
                }
            }
        }
    }
}
