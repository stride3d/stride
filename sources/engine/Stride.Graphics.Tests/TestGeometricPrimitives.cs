// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Games;
using Stride.Graphics.GeometricPrimitives;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestGeometricPrimitives : GraphicTestGameBase
    {
        private EffectInstance simpleEffect;
        private List<GeometricPrimitive> primitives;
        private Matrix view;
        private Matrix projection;

        private float timeSeconds;

        private bool isWireframe;

        private RasterizerStateDescription wireframeState;

        private bool isPaused;

        private int primitiveStartOffset;

        public TestGeometricPrimitives()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => SetTimeAndDrawPrimitives(0)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetTimeAndDrawPrimitives(1.7f)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangePrimitiveStartOffset(1)).Draw(() => SetTimeAndDrawPrimitives(2.5f)).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            wireframeState = new RasterizerStateDescription(CullMode.Back) { FillMode = FillMode.Wireframe };

            simpleEffect = new EffectInstance(new Effect(GraphicsDevice, SpriteEffect.Bytecode));

            // TODO GRAPHICS REFACTOR
            simpleEffect.Parameters.Set(TexturingKeys.Texture0, UVTexture);
            simpleEffect.UpdateEffect(GraphicsDevice);

            primitives = new List<GeometricPrimitive>();

            // Creates all primitives
            primitives = new List<GeometricPrimitive>
                             {
                                 GeometricPrimitive.Plane.New(GraphicsDevice),
                                 GeometricPrimitive.Cube.New(GraphicsDevice),
                                 GeometricPrimitive.Sphere.New(GraphicsDevice),
                                 GeometricPrimitive.GeoSphere.New(GraphicsDevice),
                                 GeometricPrimitive.Cylinder.New(GraphicsDevice),
                                 GeometricPrimitive.Torus.New(GraphicsDevice),
                                 GeometricPrimitive.Teapot.New(GraphicsDevice),
                                 GeometricPrimitive.Capsule.New(GraphicsDevice, 0.5f, 0.3f),
                                 GeometricPrimitive.Cone.New(GraphicsDevice)
                             };


            view = Matrix.LookAtRH(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY);

            Window.AllowUserResizing = true;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.W))
                isWireframe = !isWireframe;

            if (Input.IsKeyPressed(Keys.Space))
                isPaused = !isPaused;

            if (Input.IsKeyPressed(Keys.Left))
                ChangePrimitiveStartOffset(-1);

            if (Input.IsKeyPressed(Keys.Right))
                ChangePrimitiveStartOffset(1);

            projection = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)GraphicsDevice.Presenter.BackBuffer.ViewWidth / GraphicsDevice.Presenter.BackBuffer.ViewHeight, 0.1f, 100.0f);

            if (GraphicsDevice.Presenter.BackBuffer.ViewWidth < GraphicsDevice.Presenter.BackBuffer.ViewHeight) // the screen is standing up on Android{
                view = Matrix.LookAtRH(new Vector3(0, 0, 10), new Vector3(0, 0, 0), Vector3.UnitX);
        }

        private void ChangePrimitiveStartOffset(int i)
        {
            var modulo = primitives.Count - 8 + 1;
            primitiveStartOffset = (primitiveStartOffset + i + modulo) % modulo;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!isPaused)
                timeSeconds += 1 / 60f; // frame dependent time (for unit tests)

            if (!ScreenShotAutomationEnabled)
                DrawPrimitives();
        }

        private void SetTimeAndDrawPrimitives(float time)
        {
            timeSeconds = time;

            DrawPrimitives();
        }

        private void DrawPrimitives()
        {
            // Clears the screen with the Color.CornflowerBlue
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.CornflowerBlue);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render each primitive
            for (int i = 0; i < Math.Min(primitives.Count, 8); i++)
            {
                var primitive = primitives[i + primitiveStartOffset];

                // Calculate the translation
                float dx = (i % 4);
                float dy = (i >> 2);

                float x = (dx - 1.5f) * 1.7f;
                float y = 1.0f - 2.0f * dy;

                var time = timeSeconds + i;

                // Setup the World matrice for this primitive
                var world = Matrix.Scaling((float)Math.Sin(time * 1.5f) * 0.2f + 1.0f) * Matrix.RotationX(time) * Matrix.RotationY(time * 2.0f) * Matrix.RotationZ(time * .7f) * Matrix.Translation(x, y, 0);

                // Disable Cull only for the plane primitive, otherwise use standard culling
                var defaultRasterizerState = i == 0 ? RasterizerStates.CullNone : RasterizerStates.CullBack;
                primitive.PipelineState.State.RasterizerState = isWireframe ? wireframeState : defaultRasterizerState;

                // Draw the primitive using BasicEffect
                simpleEffect.Parameters.Set(SpriteBaseKeys.MatrixTransform, Matrix.Multiply(world, Matrix.Multiply(view, projection)));
                primitive.Draw(GraphicsContext, simpleEffect);
            }
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunGeometricPrimitives()
        {
            RunGameTest(new TestGeometricPrimitives());
        }
    }
}
