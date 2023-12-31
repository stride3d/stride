using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using static Stride.Rendering.Shadows.LightDirectionalShadowMapRenderer;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.DebugRender.Effects
{
    public class WireFrameRenderObject : RenderObject
    {
        // Shader properties
        public Color Color = Color.Red;
        public Matrix WorldMatrix = Matrix.Identity;

        // Vertex buffer setup
        public PrimitiveType PrimitiveType = PrimitiveType.TriangleList;
        public Buffer VertexBuffer;
        public Buffer IndiceBuffer;

        public void Prepare(GraphicsDevice graphicsDevice, int[] indices, VertexPositionNormalTexture[] vertextData)
        {
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                IndiceBuffer.Dispose();
            }

            VertexBuffer = Graphics.Buffer.Vertex.New(graphicsDevice, vertextData, GraphicsResourceUsage.Dynamic);//Buffer.New<VertexPositionColorTexture>(context.GraphicsDevice, shapeData.Value.Points.Count, BufferFlags.ShaderResource | BufferFlags.VertexBuffer);
            IndiceBuffer = Graphics.Buffer.Index.New(graphicsDevice, indices);
        }
    }
}
