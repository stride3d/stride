using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Engine.Splines.Models.Mesh
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

        public void Build(Vector3[] positions, PrimitiveType pimType)
        {
            var vertices = new VertexPositionNormalTexture[positions.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                vertices[0] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
            }

            vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);

            MeshDraw = new MeshDraw
            {
                DrawCount = 10,
                PrimitiveType = pimType,
                VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
            };
        }
    }
}

