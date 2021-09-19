using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh
{
    public class BoundingBoxMesh
    {
        public MeshDraw MeshDraw;

        private Buffer vertexBuffer;

        private readonly GraphicsDevice graphicsDevice;

        public BoundingBoxMesh(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
        }

        public void Build(BoundingBox boundingBox)
        {
            var indices = new int[12 * 2];
            var vertices = new VertexPositionNormalTexture[8];
            var min = boundingBox.Minimum;
            var max = boundingBox.Maximum;

            //TOP
            vertices[0] = new VertexPositionNormalTexture(new Vector3(min.X, min.Y, max.Z), Vector3.UnitY, Vector2.Zero);
            vertices[1] = new VertexPositionNormalTexture(min, Vector3.UnitY, Vector2.Zero);
            vertices[2] = new VertexPositionNormalTexture(new Vector3(max.X, min.Y, min.Z), Vector3.UnitY, Vector2.Zero);
            vertices[3] = new VertexPositionNormalTexture(new Vector3(max.X, min.Y, max.Z), Vector3.UnitY, Vector2.Zero);

            int indexOffset = 0;
            for (int i = 0; i < 4; i++)
            {
                indices[indexOffset++] = i;
                indices[indexOffset++] = (i + 1) % 4;
            }

            // Duplicate vertices and indices to bottom part
            for (int i = 0; i < 4; i++)
            {
                vertices[i + 4] = vertices[i];
                vertices[i + 4].Position.Y = max.Y;
                indices[indexOffset++] = indices[i * 2] + 4;
                indices[indexOffset++] = indices[i * 2 + 1] + 4;
            }

            // Sides
            for (int i = 0; i < 4; i++)
            {
                indices[indexOffset++] = i;
                indices[indexOffset++] = i + 4;
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
