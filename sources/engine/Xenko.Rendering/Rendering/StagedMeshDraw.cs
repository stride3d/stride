using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Graphics;

namespace Xenko.Rendering.Rendering {
    public class StagedMeshDraw : MeshDraw {

        public Action<GraphicsDevice> performStage;

        private StagedMeshDraw() { }

        /// <summary>
        /// Gets a MeshDraw that will be prepared when needed with the given index buffer & vertex buffer.
        /// </summary>
        /// <typeparam name="T">Type of vertex buffer used</typeparam>
        /// <param name="indexBuffer">Array of vertex indicies</param>
        /// <param name="vertexBuffer">Vertex buffer</param>
        /// <returns></returns>
        public static StagedMeshDraw MakeStagedMeshDraw<T>(uint[] indexBuffer, T[] vertexBuffer, VertexDeclaration vertexBufferLayout) where T : struct {
            StagedMeshDrawTyped<T> smdt = new StagedMeshDrawTyped<T>();
            smdt.PrimitiveType = PrimitiveType.TriangleList;
            smdt.DrawCount = indexBuffer.Length;
            smdt.performStage = (GraphicsDevice graphicsDevice) => {
                Xenko.Graphics.Buffer vbo, ibo;
                vbo = Xenko.Graphics.Buffer.Vertex.New<T>(
                    graphicsDevice,
                    vertexBuffer,
                    GraphicsResourceUsage.Immutable
                );
                ibo = Xenko.Graphics.Buffer.Index.New<uint>(
                    graphicsDevice,
                    indexBuffer
                );
                smdt.VertexBuffers = new[] {
                    new VertexBufferBinding(vbo, vertexBufferLayout, smdt.DrawCount)
                };
                smdt.IndexBuffer = new IndexBufferBinding(ibo, true, smdt.DrawCount);
            };
            return smdt;
        }

        private class StagedMeshDrawTyped<T> : StagedMeshDraw where T : struct {
            private uint[] stagedIndexBuffer;
            private T[] stagedVertexBuffer;
        }
    }
}
