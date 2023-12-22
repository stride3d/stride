using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using static Stride.Rendering.Shadows.LightDirectionalShadowMapRenderer;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Effects
{
    public class WireFrameRenderObject : RenderObject
    {
        // Shader properties
        public Color Color = Color.Red;
        public Matrix WorldMatrix = Matrix.Identity;

        // Vertex buffer setup
        public  PrimitiveType PrimitiveType = PrimitiveType.TriangleList;
        public Buffer VertexBuffer;
        public Buffer IndiceBuffer;

        public void Prepare(GraphicsDevice graphicsDevice, int[] indices, VertexPositionNormalTexture[] vertextData)
        {
            if (VertexBuffer != null)
                return;

            var normal = new Vector3(0, 0, 1);
            var texturePos = new Vector2(0, 0);
            IndiceBuffer = Graphics.Buffer.Index.New(graphicsDevice, indices);
            VertexBuffer = Graphics.Buffer.Vertex.New(graphicsDevice, vertextData, GraphicsResourceUsage.Dynamic);//Buffer.New<VertexPositionColorTexture>(context.GraphicsDevice, shapeData.Value.Points.Count, BufferFlags.ShaderResource | BufferFlags.VertexBuffer);
        }
    }
}
