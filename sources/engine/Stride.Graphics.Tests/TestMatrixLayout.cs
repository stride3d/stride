// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

/// <summary>
///   A <see cref="Matrix"/> copied as it is into a constant buffer or a structured buffer is read by a shader with the same
///   elements: <c>M23</c> is <c>matrix[1][2]</c>.
/// </summary>
public class TestMatrixLayout : GraphicTestGameBase
{
    private static readonly Matrix Expected = new(
        11, 12, 13, 14,
        21, 22, 23, 24,
        31, 32, 33, 34,
        41, 42, 43, 44);

    public TestMatrixLayout()
    {
        // Test shaders compile at the test settings' profile otherwise, which has no structured buffers
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    [Theory]
    [InlineData(0u)] // constant buffer matrix
    [InlineData(1u)] // element of a constant buffer array
    [InlineData(2u)] // element of a structured buffer
    public void ShaderReadsTheElementsOfTheMatrix(uint source)
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, 4, 4, PixelFormat.R32_Float, TextureFlags.RenderTarget);
            using var matrixBuffer = Buffer.Structured.New(device, new[] { Matrix.Identity, Expected });

            var effect = new EffectInstance(game.EffectSystem.LoadEffect("MatrixLayoutTestShader").WaitForResult());
            effect.Parameters.Set(MatrixLayoutTestShaderKeys.TestMatrix, Expected);
            effect.Parameters.Set(MatrixLayoutTestShaderKeys.TestMatrices, new[] { Matrix.Identity, Expected });
            effect.Parameters.Set(MatrixLayoutTestShaderKeys.TestMatrixBuffer, matrixBuffer);
            effect.Parameters.Set(MatrixLayoutTestShaderKeys.Source, source);
            effect.UpdateEffect(device);

            var pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
            pipelineState.State.InputElements = PrimitiveQuad.VertexDeclaration.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveQuad.PrimitiveType;

            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();
            commandList.SetPipelineState(pipelineState.CurrentState);
            effect.Apply(game.GraphicsContext);
            device.PrimitiveQuad.Draw(commandList);

            var pixels = target.GetData<float>(commandList);
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Assert.Equal(Expected[row, column], pixels[(row * 4) + column]);
                }
            }
        });
    }
}
