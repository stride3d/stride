// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xunit;

using Xenko.Core.Mathematics;
using Xenko.Rendering;
using Xenko.Games;

namespace Xenko.Graphics.Tests
{
    public class TestTextureSampling : GraphicTestGameBase
    {
        struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoords;
        };

        private struct DrawOptions
        {
            public Matrix Transform;

            public SamplerState Sampler;
        };

        private EffectInstance simpleEffect;

        // TODO GRAPHICS REFACTOR
        //private VertexArrayObject vao;

        private DrawOptions[] myDraws;
        private Mesh mesh;
        private MutablePipelineState pipelineState;

        public TestTextureSampling()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawTextureSampling).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            pipelineState = new MutablePipelineState(GraphicsDevice);

            var vertices = new Vertex[4];
            vertices[0] = new Vertex { Position = new Vector3(-1, -1, 0.5f), TexCoords = new Vector2(0, 0) };
            vertices[1] = new Vertex { Position = new Vector3(-1, 1, 0.5f), TexCoords = new Vector2(3, 0) };
            vertices[2] = new Vertex { Position = new Vector3(1, 1, 0.5f), TexCoords = new Vector2(3, 3) };
            vertices[3] = new Vertex { Position = new Vector3(1, -1, 0.5f), TexCoords = new Vector2(0, 3) };

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };

            var vertexBuffer = Buffer.Vertex.New(GraphicsDevice, vertices, GraphicsResourceUsage.Default);
            var indexBuffer = Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var meshDraw = new MeshDraw
            {
                DrawCount = 4,
                PrimitiveType = PrimitiveType.TriangleList,
                VertexBuffers = new[]
                {
                    new VertexBufferBinding(vertexBuffer,
                        new VertexDeclaration(VertexElement.Position<Vector3>(),
                            VertexElement.TextureCoordinate<Vector2>()),
                        4)
                },
                IndexBuffer = new IndexBufferBinding(indexBuffer, false, indices.Length),
            };

            mesh = new Mesh
            {
                Draw = meshDraw,
            };

            simpleEffect = new EffectInstance(new Effect(GraphicsDevice, SpriteEffect.Bytecode));
            simpleEffect.Parameters.Set(TexturingKeys.Texture0, UVTexture);
            simpleEffect.UpdateEffect(GraphicsDevice);

            // TODO GRAPHICS REFACTOR
            //vao = VertexArrayObject.New(GraphicsDevice, mesh.Draw.IndexBuffer, mesh.Draw.VertexBuffers);

            myDraws = new DrawOptions[3];
            myDraws[0] = new DrawOptions { Sampler = GraphicsDevice.SamplerStates.LinearClamp, Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(-0.5f, 0.5f, 0f)) };
            myDraws[1] = new DrawOptions { Sampler = GraphicsDevice.SamplerStates.LinearWrap, Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(0.5f, 0.5f, 0f)) };
            myDraws[2] = new DrawOptions { Sampler = SamplerState.New(GraphicsDevice, new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Mirror)), Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(0.5f, -0.5f, 0f)) };
            //var borderDescription = new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Border) { BorderColor = Color.Purple };
            //var border = SamplerState.New(GraphicsDevice, borderDescription);
            //myDraws[3] = new DrawOptions { Sampler = border, Transform = Matrix.Multiply(Matrix.Scale(0.3f), Matrix.Translation(-0.5f, -0.5f, 0f)) };
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                DrawTextureSampling();
        }

        private void DrawTextureSampling()
        {
            // Clears the screen 
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.LightBlue);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Create pipeline state
            pipelineState.State.SetDefaults();
            pipelineState.State.RootSignature = simpleEffect.RootSignature;
            pipelineState.State.EffectBytecode = simpleEffect.Effect.Bytecode;
            pipelineState.State.InputElements = mesh.Draw.VertexBuffers.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
            pipelineState.State.RasterizerState = RasterizerStates.CullNone;
            pipelineState.State.Output.CaptureState(GraphicsContext.CommandList);
            pipelineState.Update();

            // Set pipeline state
            GraphicsContext.CommandList.SetPipelineState(pipelineState.CurrentState);

            // Set vertex/index buffers
            GraphicsContext.CommandList.SetVertexBuffer(0, mesh.Draw.VertexBuffers[0].Buffer, mesh.Draw.VertexBuffers[0].Offset, mesh.Draw.VertexBuffers[0].Stride);
            GraphicsContext.CommandList.SetIndexBuffer(mesh.Draw.IndexBuffer.Buffer, mesh.Draw.IndexBuffer.Offset, mesh.Draw.IndexBuffer.Is32Bit);

            for (var i = 0; i < myDraws.Length; ++i)
            {
                simpleEffect.Parameters.Set(TexturingKeys.Sampler, myDraws[i].Sampler);
                simpleEffect.Parameters.Set(SpriteBaseKeys.MatrixTransform, myDraws[i].Transform);
                simpleEffect.Apply(GraphicsContext);
                GraphicsContext.CommandList.DrawIndexed(6);
            }
        }

        internal static void Main()
        {
            using (var game = new TestTextureSampling())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestTextureSampling()
        {
            RunGameTest(new TestTextureSampling());
        }
    }
}
