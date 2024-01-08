using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.DebugRender.Effects
{
    public class WireFrameRenderObject : RenderObject, IDisposable
    {
        // Shader properties
        public Color Color = Color.Red;
        public Matrix WorldMatrix = Matrix.Identity;
        public Matrix ContainerBaseMatrix = Matrix.Identity;

        // Vertex buffer setup
        public readonly int VertexStride;
        public readonly Buffer VertexBuffer;
        public readonly Buffer IndiceBuffer;
        public PrimitiveType PrimitiveType => PrimitiveType.TriangleList;

        private WireFrameRenderObject(int vertexStride, Buffer vertexBuffer, Buffer indiceBuffer)
        {
            VertexStride = vertexStride;
            VertexBuffer = vertexBuffer;
            IndiceBuffer = indiceBuffer;
        }

        public static WireFrameRenderObject New<T>(GraphicsDevice graphicsDevice, int[] indices, T[] vertices) where T : unmanaged, IVertexStructure
        {
            return new(T.Declaration().VertexStride, Buffer.Vertex.New(graphicsDevice, vertices, GraphicsResourceUsage.Dynamic), Buffer.Index.New(graphicsDevice, indices));
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndiceBuffer.Dispose();
        }
    }
}
