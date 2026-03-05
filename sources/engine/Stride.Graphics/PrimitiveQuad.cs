// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics
{
    /// <summary>
    ///   A primitive triangle that can be used to draw a Texture or an Effect,
    ///   commonly used to draw full-screen.
    ///   This is directly accessible from the <see cref="GraphicsDeviceExtensions.DrawQuad"/> method.
    /// </summary>
    public class PrimitiveQuad : ComponentBase
    {
        private readonly MutablePipelineState pipelineState;

        private readonly EffectInstance simpleEffect;
        private readonly SharedData sharedData;

        /// <summary>
        ///   The definition of the layout of the vertices used to draw the triangle.
        /// </summary>
        public static readonly VertexDeclaration VertexDeclaration = VertexPositionNormalTexture.Layout;
        /// <summary>
        ///   The type of primitives used to draw the triangle.
        /// </summary>
        public static readonly PrimitiveType PrimitiveType = PrimitiveType.TriangleList;


        /// <summary>
        ///   Initializes a new instance of the <see cref="PrimitiveQuad"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        public PrimitiveQuad(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;

            sharedData = graphicsDevice.GetOrCreateSharedData(SharedData.SharedDataKey, static device => new SharedData(device));

            var spriteEffect = new Effect(graphicsDevice, SpriteEffect.Bytecode).DisposeBy(this);
            simpleEffect = new EffectInstance(spriteEffect).DisposeBy(this);
            simpleEffect.Parameters.Set(SpriteBaseKeys.MatrixTransform, Matrix.Identity);
            simpleEffect.UpdateEffect(graphicsDevice);

            pipelineState = new MutablePipelineState(GraphicsDevice);
            pipelineState.State.SetDefaults();
            pipelineState.State.InputElements = VertexDeclaration.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveType;
        }


        /// <summary>
        ///   Gets the Graphics Device.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        ///   Gets the parameters used by the default Effect used to draw.
        /// </summary>
        public ParameterCollection Parameters => simpleEffect.Parameters;


        /// <summary>
        ///   Draws a full-screen triangle.
        /// </summary>
        /// <param name="commandList">The Command List to use for drawing.</param>
        /// <remarks>
        ///   An Effect with a Pixel Shader having the signature <c>float2 : TEXCOORD</c>
        ///   must have been applied before calling this method.
        /// </remarks>
        public void Draw(CommandList commandList)
        {
            commandList.SetVertexBuffer(index: 0, sharedData.VertexBuffer.Buffer, sharedData.VertexBuffer.Offset, sharedData.VertexBuffer.Stride);
            commandList.Draw(SharedData.VertexCount);
        }

        /// <summary>
        ///   Draws a full-screen triangle.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use for drawing.</param>
        /// <param name="effectInstance">
        ///   The Effect instance to use for drawing.
        ///   It must define a Pixel Shader having the signature <c>float2 : TEXCOORD</c>.
        /// </param>
        public void Draw(GraphicsContext graphicsContext, EffectInstance effectInstance)
        {
            effectInstance.UpdateEffect(GraphicsDevice);

            pipelineState.State.RootSignature = effectInstance.RootSignature;
            pipelineState.State.EffectBytecode = effectInstance.Effect.Bytecode;
            pipelineState.State.BlendState = BlendStates.Default;
            pipelineState.State.Output.CaptureState(graphicsContext.CommandList);
            pipelineState.Update();

            graphicsContext.CommandList.SetPipelineState(pipelineState.CurrentState);

            effectInstance.Apply(graphicsContext);

            Draw(graphicsContext.CommandList);
        }

        /// <summary>
        ///   Draws a full-screen triangle with a Texture.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use for drawing.</param>
        /// <param name="texture">The Texture to draw.</param>
        /// <param name="blendState">
        ///   An optional Blend State to use when drawing the Texture.
        ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
        /// </param>
        /// <remarks>
        ///   The Texture will be sampled using the default Sampler State <see cref="SamplerStateFactory.LinearClamp"/>.
        /// </remarks>
        public void Draw(GraphicsContext graphicsContext, Texture texture, BlendStateDescription? blendState = null)
        {
            Draw(graphicsContext, texture, samplerState: null, Color.White, blendState);
        }

        /// <summary>
        ///   Draws a full-screen triangle with a tinted Texture.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use for drawing.</param>
        /// <param name="texture">The Texture to draw.</param>
        /// <param name="samplerState">
        ///   The Sampler State to use for sampling the texture.
        ///   Specify <see langword="null"/> to use the default Sampler State <see cref="SamplerStateFactory.LinearClamp"/>.
        /// </param>
        /// <param name="color">
        ///   The color to tint the Texture with. The final color will be the texture color multiplied by the specified color.
        /// </param>
        /// <param name="blendState">
        ///   An optional Blend State to use when drawing the Texture.
        ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
        /// </param>
        public void Draw(GraphicsContext graphicsContext, Texture texture, SamplerState samplerState, Color4 color, BlendStateDescription? blendState = null)
        {
            pipelineState.State.RootSignature = simpleEffect.RootSignature;
            pipelineState.State.EffectBytecode = simpleEffect.Effect.Bytecode;
            pipelineState.State.BlendState = blendState ?? BlendStates.Default;
            pipelineState.State.Output.CaptureState(graphicsContext.CommandList);
            pipelineState.Update();

            graphicsContext.CommandList.SetPipelineState(pipelineState.CurrentState);

            // Make sure that we are using our Vertex Shader
            simpleEffect.Parameters.Set(SpriteEffectKeys.Color, color);
            simpleEffect.Parameters.Set(TexturingKeys.Texture0, texture);
            simpleEffect.Parameters.Set(TexturingKeys.Sampler, samplerState ?? GraphicsDevice.SamplerStates.LinearClamp);
            simpleEffect.Apply(graphicsContext);

            Draw(graphicsContext.CommandList);

            // TODO: ADD QUICK UNBIND FOR SRV
            //GraphicsDevice.Context.PixelShader.SetShaderResource(0, null);
        }


        /// <summary>
        ///   Internal class used to store the Vertex Buffer and Vertex Input Layout.
        /// </summary>
        private class SharedData : ComponentBase
        {
            public const string SharedDataKey = $"{nameof(PrimitiveQuad)}::VertexBuffer";

            /// <summary>
            ///   The Vertex Buffer.
            /// </summary>
            public readonly VertexBufferBinding VertexBuffer;

            /// <summary>
            ///   The number of vertices in the triangle.
            /// </summary>
            public const int VertexCount = 3;

            // TODO: This is not a quad, but a fullscreen triangle! Maybe this class should be renamed?
            private static readonly VertexPositionNormalTexture[] TriangleVertices =
            [
                //                              Position                Normal                Texture Coordinates
                new VertexPositionNormalTexture(new Vector3(-1,  1, 0), new Vector3(0, 0, 1), new Vector2(0, 0)),
                new VertexPositionNormalTexture(new Vector3(+3,  1, 0), new Vector3(0, 0, 1), new Vector2(2, 0)),
                new VertexPositionNormalTexture(new Vector3(-1, -3, 0), new Vector3(0, 0, 1), new Vector2(0, 2)),
            ];


            /// <summary>
            ///   Initializes a new instance of the <see cref="SharedData"/> class.
            /// </summary>
            /// <param name="device">The Graphics Device.</param>
            public SharedData(GraphicsDevice device) : base(name: SharedDataKey)
            {
                ReadOnlySpan<VertexPositionNormalTexture> triangleVertices =
                [
                    //                              Position                Normal                Texture Coordinates
                    new VertexPositionNormalTexture(new Vector3(-1,  1, 0), new Vector3(0, 0, 1), new Vector2(0, 0)),
                    new VertexPositionNormalTexture(new Vector3(+3,  1, 0), new Vector3(0, 0, 1), new Vector2(2, 0)),
                    new VertexPositionNormalTexture(new Vector3(-1, -3, 0), new Vector3(0, 0, 1), new Vector2(0, 2)),
                ];

                var vertexBuffer = Buffer.Vertex.New(device, TriangleVertices).DisposeBy(this);
                vertexBuffer.Name = device.IsDebugMode
                    ? $"{SharedDataKey} ({vertexBuffer.Name})"
                    : SharedDataKey;

                // Register reload
                vertexBuffer.Reload = (graphicsResource, services) => ((Buffer) graphicsResource).Recreate(TriangleVertices);

                VertexBuffer = new VertexBufferBinding(vertexBuffer, VertexDeclaration, TriangleVertices.Length, VertexPositionNormalTexture.Size);
            }
        }
    }
}
