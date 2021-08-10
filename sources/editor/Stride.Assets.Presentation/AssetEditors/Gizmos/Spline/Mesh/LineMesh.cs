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
            var vertices = new VertexPositionNormalTexture[positions.Length];

            for (int i = 0; i < positions.Length; i++)
            {
                vertices[0] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
            }

            vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
            MeshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.LineStrip,
                DrawCount = 10, //? 
                VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
            };
        }
    }
}

