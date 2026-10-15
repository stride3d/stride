// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
///   Vertex and index buffers created with <see cref="BufferFlags.ShaderResource"/> | <see cref="BufferFlags.RawBuffer"/>
///   are read by shaders as <c>ByteAddressBuffer</c> and still work as vertex and index input.
/// </summary>
public class TestShaderReadableMeshBuffers : GraphicTestGameBase
{
    private const BufferFlags ShaderReadable = BufferFlags.ShaderResource | BufferFlags.RawBuffer;
    private const int TargetSize = 4;

    // Vertices 0, 2 and 4 make a triangle that covers the whole target; 1 and 3 must not be read
    private static readonly Vector3[] Positions =
    [
        new(-1, -1, 0.5f), new(7, 8, 9), new(-1, 3, 0.5f), new(10, 11, 12), new(3, -1, 0.5f),
    ];

    // The triangle's indices start at byte 2, after an unused index, so the first one is in the high half of a word
    private static readonly ushort[] Indices = [9, 4, 2, 0];
    private const int IndexByteOffset = 2;

    [SkippableFact]
    public void ComputeShaderFetchesTriangleThroughIndexBuffer()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;

            using var vertexBuffer = Buffer.Vertex.New(device, Positions, additionalFlags: ShaderReadable);
            using var indexBuffer = Buffer.Index.New(device, Indices, additionalFlags: ShaderReadable);

            var fetched = Fetch(game, vertexBuffer, indexBuffer, IndexByteOffset, indexSize: sizeof(ushort), vertexByteOffset: 0, Vector3.SizeInBytes, indexCount: 3);

            Assert.Equal(new[] { new Vector4(Positions[4], 4), new Vector4(Positions[2], 2), new Vector4(Positions[0], 0) }, fetched);
        });
    }

    [SkippableFact]
    public void ModelAssetOptionMakesBuffersShaderReadable()
    {
        PerformTest(game =>
        {
            // Built from a cube with ShaderReadableBuffers: true
            var model = game.Content.Load<Model>("ShaderReadableCube");
            var draw = model.Meshes[0].Draw;
            var vertices = draw.VertexBuffers[0];
            var indices = draw.IndexBuffer;

            foreach (var buffer in new[] { vertices.Buffer, indices.Buffer })
            {
                Assert.True(buffer.ViewFlags.HasFlag(ShaderReadable));
                Assert.Equal(0, buffer.SizeInBytes % sizeof(uint));
            }

            var positionOffset = vertices.Declaration.EnumerateWithOffsets().First(x => x.VertexElement.SemanticName == VertexElementUsage.Position).Offset;
            var indexSize = indices.Is32Bit ? sizeof(uint) : sizeof(ushort);

            var fetched = Fetch(game, vertices.Buffer, indices.Buffer, indices.Offset + draw.StartLocation * indexSize, indexSize,
                                vertices.Offset + positionOffset, vertices.Stride, draw.DrawCount);

            // Every vertex the triangles use is a vertex of the mesh, on a face of the model's bounding box
            var box = model.BoundingBox;
            foreach (var vertex in fetched)
            {
                Assert.InRange(vertex.W, 0, vertices.Count - 1);
                var position = new Vector3(vertex.X, vertex.Y, vertex.Z);
                Assert.Equal(ContainmentType.Contains, box.Contains(ref position));
                Assert.True(IsOnFace(position.X, box.Minimum.X, box.Maximum.X)
                         || IsOnFace(position.Y, box.Minimum.Y, box.Maximum.Y)
                         || IsOnFace(position.Z, box.Minimum.Z, box.Maximum.Z), $"{position} is inside the cube");
            }

            static bool IsOnFace(float value, float minimum, float maximum)
                => MathUtil.WithinEpsilon(value, minimum, 1e-4f) || MathUtil.WithinEpsilon(value, maximum, 1e-4f);
        });
    }

    /// <summary>
    ///   Fetches the vertex positions that <paramref name="indexCount"/> indices point to, with a compute shader that reads
    ///   both buffers as <c>ByteAddressBuffer</c>. Each result is (position, index).
    /// </summary>
    private static Vector4[] Fetch(GameTestBase game, Buffer vertexBuffer, Buffer indexBuffer, int indexByteOffset, int indexSize,
                                   int vertexByteOffset, int vertexStride, int indexCount)
    {
        var commandList = game.GraphicsContext.CommandList;
        var renderContext = RenderContext.GetShared(game.Services);

        using var output = Buffer.Structured.New<Vector4>(game.GraphicsDevice, indexCount, unorderedAccess: true);
        using var effect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "MeshBufferFetchTestShader",
            ThreadNumbers = new Int3(indexCount, 1, 1),
        };

        commandList.ResourceBarrierTransition(vertexBuffer, BarrierLayout.ShaderResource);
        commandList.ResourceBarrierTransition(indexBuffer, BarrierLayout.ShaderResource);
        commandList.ResourceBarrierTransition(output, BarrierLayout.UnorderedAccess);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.Vertices, vertexBuffer);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.Indices, indexBuffer);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.Output, output);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.IndexByteOffset, (uint) indexByteOffset);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.IndexSize, (uint) indexSize);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.VertexByteOffset, (uint) vertexByteOffset);
        effect.Parameters.Set(MeshBufferFetchTestShaderKeys.VertexStride, (uint) vertexStride);
        ((RendererBase) effect).Draw(new RenderDrawContext(game.Services, renderContext, game.GraphicsContext));

        return output.GetData<Vector4>(commandList);
    }

    [SkippableFact]
    public void ShaderReadableBuffersStillDrawAsVertexAndIndexInput()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var vertexBuffer = Buffer.Vertex.New(device, Positions, additionalFlags: ShaderReadable);
            using var indexBuffer = Buffer.Index.New(device, Indices, additionalFlags: ShaderReadable);
            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);

            var effect = new EffectInstance(game.EffectSystem.LoadEffect("MeshBufferDrawTestShader").WaitForResult());
            effect.Parameters.Set(MeshBufferDrawTestShaderKeys.Color, (Vector4) Color.Lime);
            effect.UpdateEffect(device);

            var pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
            pipelineState.State.RasterizerState = RasterizerStates.CullNone;
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
            pipelineState.State.InputElements =
            [
                new InputElementDescription { SemanticName = "POSITION", Format = PixelFormat.R32G32B32_Float },
            ];

            commandList.Clear(target, Color.Black);
            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();

            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetVertexBuffer(0, vertexBuffer, 0, Vector3.SizeInBytes);
            commandList.SetIndexBuffer(indexBuffer, IndexByteOffset, false);
            effect.Apply(game.GraphicsContext);
            commandList.DrawIndexed(3);

            foreach (var pixel in target.GetData<Color>(commandList))
            {
                Assert.Equal(Color.Lime, pixel);
            }
        });
    }

    [SkippableFact]
    public void RawBufferSizeMustBeWholeWords()
    {
        PerformTest(game =>
        {
            // Three 16-bit indices are 6 bytes: a raw view could not reach the last one
            Assert.Throws<ArgumentException>(() => Buffer.Index.New(game.GraphicsDevice, new ushort[] { 0, 1, 2 }, additionalFlags: ShaderReadable));
        });
    }
}
