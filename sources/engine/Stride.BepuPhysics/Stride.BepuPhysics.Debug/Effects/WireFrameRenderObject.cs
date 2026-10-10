// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Debug.Effects
{
    public class WireFrameRenderObject : RenderObject, IDisposable
    {
        // Shader properties
        public Color Color = Color.Red;
        public Matrix WorldMatrix = Matrix.Identity;
        public Matrix CollidableBaseMatrix = Matrix.Identity;

        /// <summary> The collidable's entity, whose visual models the lines are compared to </summary>
        public Entity? Owner;

        /// <summary> Shared by the wireframes of one collidable, never 0 </summary>
        public int ObjectId;

        // Vertex buffer setup
        public readonly int VertexStride;
        public readonly Buffer VertexBuffer;
        public readonly Buffer IndexBuffer;
        public PrimitiveType PrimitiveType => PrimitiveType.TriangleList;

        private WireFrameRenderObject(int vertexStride, Buffer vertexBuffer, Buffer indexBuffer)
        {
            VertexStride = vertexStride;
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
        }

        public static WireFrameRenderObject New<T>(GraphicsDevice graphicsDevice, int[] indices, T[] vertices) where T : unmanaged, IVertexStructure
        {
            #warning change dynamic to default
            #warning we should pool those as well
            return new(T.Declaration().VertexStride, Buffer.Vertex.New(graphicsDevice, vertices, GraphicsResourceUsage.Dynamic), Buffer.Index.New(graphicsDevice, indices));
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }
}
