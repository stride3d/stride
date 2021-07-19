using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh
{
    public class LineMesh
    {
        public MeshDraw MeshDraw;

        private Buffer vertexBuffer;

        private readonly GraphicsDevice graphicsDevice;

        public LineMesh(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
        }

        public void Build(Vector3[] positions)
        {
            var indices = new int[12 * 2];
            var vertices = new VertexPositionNormalTexture[positions.Length];

            for (int i = 0; i < positions.Length; i++)
            {
                vertices[0] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
            }

            int indexOffset = 0;
            for (int i = 0; i < 4; i++)
            {
                indices[indexOffset++] = i;
                indices[indexOffset++] = (i + 1) % 4;
            }

            vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
            MeshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.LineList,
                DrawCount = indices.Length,
                IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
            };
        }
    }
}
