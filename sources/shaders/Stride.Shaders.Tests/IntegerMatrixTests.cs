// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Spv = Stride.Shaders.Spirv.Tools.Spv;
using SpirvOp = Stride.Shaders.Spirv.Specification.Op;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// SPIR-V has floating-point matrices only: an int, uint or bool matrix is an array of its rows, as in DXC. In a cbuffer or a
/// buffer it is error SDSL0195 (no layout yet). The values are checked by the RenderTest <c>IntegerMatrices</c>.
/// </summary>
public class IntegerMatrixTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-integer-matrices");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IntegerMatrixIsAnArrayOfRows(bool vulkan)
    {
        var mixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/CompilerTests"));
        mixer.ShaderLoader.LoadExternalBuffer("IntegerMatrix", [], out _, out _, out _);
        var log = new LoggerResult();
        var options = vulkan
            ? new ShaderMixer.Options(ResourcesRegisterSeparate: false, StripGoogleUserType: true, Direct3DStructuredBufferLayout: true)
            : new ShaderMixer.Options(ResourcesRegisterSeparate: true);
        Assert.True(mixer.MergeSDSL(new ShaderClassSource("IntegerMatrix"), options, log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode, targetVulkan: vulkan);
        Assert.True(validation.IsValid, validation.Output);

        // int4x4: an array of 4 int4, and no matrix type
        var words = MemoryMarshal.Cast<byte, uint>(bytecode).ToArray();
        var (matrices, arrays) = (0, 0);
        for (var w = 5; w < words.Length; w += (int)(words[w] >> 16))
        {
            var op = (SpirvOp)(words[w] & 0xFFFF);
            if (op == SpirvOp.OpTypeMatrix)
                matrices++;
            else if (op == SpirvOp.OpTypeArray)
                arrays++;
        }
        Assert.Equal(0, matrices);
        Assert.Equal(1, arrays);
    }

    [Theory]
    [InlineData("int3x3 Kernel;", "Kernel", "int3x3", "int3")]
    [InlineData("cbuffer Params { uint2x4 Mask; }", "Mask", "uint2x4", "uint4")]
    [InlineData("int2x2 Kernels[2];", "Kernels", "int2x2", "int2")]
    [InlineData("RWStructuredBuffer<Cell> Cells;", "Cells", "bool3x2", "bool2")]
    public void IntegerMatrixInABufferIsReported(string member, string name, string matrix, string row)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "CheckMatrix.sdsl"), $$"""
            shader CheckMatrix
            {
                struct Cell { float Weight; bool3x2 Flags; };

                {{member}}
            }
            """);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName) { Log = log };
        try
        {
            loader.LoadExternalBuffer("CheckMatrix", [], out _, out _, out _);
        }
        catch (InvalidOperationException)
        {
        }

        var messages = string.Join(Environment.NewLine, log.Messages.Select(m => m.Text));
        Assert.Contains("CheckMatrix.sdsl(5,", messages);
        Assert.Contains($"SDSL0195: {name} holds an int, uint or bool matrix ({matrix}), which can't be in a cbuffer or a buffer yet. Use an array of {row} instead", messages);
    }
}
