using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Xenko.Graphics;

namespace Xenko.Rendering.Rendering {
    public class StagedMeshDraw : MeshDraw {

        public Action<GraphicsDevice> performStage;

        private StagedMeshDraw() { }
        private static object StagedLock = new object();

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
                if (StagedMeshDrawTyped<T>.CachedBuffers.TryGetValue(vertexBuffer, out object[] bufferBindings)) {
                    smdt.VertexBuffers = (VertexBufferBinding[])bufferBindings[0];
                    smdt.IndexBuffer = (IndexBufferBinding)bufferBindings[1];
                } else {
                    Xenko.Graphics.Buffer vbo, ibo;
                    lock (StagedLock) {
                        vbo = Xenko.Graphics.Buffer.Vertex.New<T>(
                            graphicsDevice,
                            vertexBuffer,
                            GraphicsResourceUsage.Immutable
                        );
                        ibo = Xenko.Graphics.Buffer.Index.New<uint>(
                            graphicsDevice,
                            indexBuffer
                        );
                    }
                    object[] o = new object[2];
                    VertexBufferBinding[] vbb = new[] {
                        new VertexBufferBinding(vbo, vertexBufferLayout, smdt.DrawCount)
                    };
                    IndexBufferBinding ibb = new IndexBufferBinding(ibo, true, smdt.DrawCount);
                    o[0] = vbb;
                    o[1] = ibb;
                    StagedMeshDrawTyped<T>.CachedBuffers.TryAdd(vertexBuffer, o);
                    smdt.VertexBuffers = vbb;
                    smdt.IndexBuffer = ibb;
                }
            };
            return smdt;
        }

        private class StagedMeshDrawTyped<T> : StagedMeshDraw where T : struct {
            private uint[] stagedIndexBuffer;
            private T[] stagedVertexBuffer;
            public static ConcurrentDictionary<T[], object[]> CachedBuffers = new ConcurrentDictionary<T[], object[]>();
        }
    }
}
