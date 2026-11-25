// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.IO;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// On Direct3D 11, the effect compiler takes the resource bindings from FXC's reflection of each stage: each kind of
/// resource gets its parameter type and class.
/// </summary>
public class Direct3D11ReflectionTests : IDisposable
{
    // The application cache outlives the test run: a fresh shader name per run keeps it from what an earlier run left
    private readonly string shaderName = $"Reflection{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly EffectCompiler compiler;

    public Direct3D11ReflectionTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"reflection-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/reflectiontest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");
    }

    public void Dispose()
    {
        compiler.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ComputeResourcesGetTheirTypeFromFxc()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("FXC is only on Windows");

        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        File.WriteAllText(shaderPath, $$"""
            shader {{shaderName}} : ComputeShaderBase
            {
                StructuredBuffer<float> Structured;
                ByteAddressBuffer Raw;
                Texture2DMS<float4> Multisampled;
                RWBuffer<float> Typed;
                RWTexture2D<float4> Image2D;
                RWTexture3D<float4> Image3D;
                RWStructuredBuffer<float> RWStructured;
                RWByteAddressBuffer RWRaw;

                cbuffer Counts
                {
                    uint Count;
                };

                override void Compute()
                {
                    uint i = streams.DispatchThreadId.x;
                    uint width, height, samples;
                    Multisampled.GetDimensions(width, height, samples);
                    float value = Structured[i] + asfloat(Raw.Load(i * 4)) + Multisampled.Load(int2(i, 0), 0).x + width + Count;
                    Typed[i] = value;
                    Image2D[uint2(i, 0)] = value;
                    Image3D[uint3(i, 0, 0)] = value;
                    RWStructured[i] = value;
                    RWRaw.Store(i * 4, asuint(value));
                }
            };
            """);
        compiler.UrlToFilePath[$"{sourceDir.Name}/{shaderName}.sdsl"] = shaderPath;

        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Direct3D11 },
        };
        var result = compiler.Compile(new ShaderClassSource(shaderName), parameters).Bytecode.WaitForResult();
        Assert.False(result.CompilationLog.HasErrors, string.Join(Environment.NewLine, result.CompilationLog.Messages.Select(m => m.Text)));

        var bindings = result.Bytecode!.Reflection.ResourceBindings.Where(b => b.Stage == ShaderStage.Compute).ToList();
        string Binding(string name)
        {
            var binding = bindings.Single(b => b.KeyInfo.KeyName == $"{shaderName}.{name}");
            return $"{name}: {binding.Class} {binding.Type}";
        }

        Assert.Equal(
            [
                "Structured: ShaderResourceView StructuredBuffer",
                "Raw: ShaderResourceView ByteAddressBuffer",
                "Multisampled: ShaderResourceView Texture2DMultisampled",
                "Typed: UnorderedAccessView RWBuffer",
                "Image2D: UnorderedAccessView RWTexture2D",
                "Image3D: UnorderedAccessView RWTexture3D",
                "RWStructured: UnorderedAccessView RWStructuredBuffer",
                "RWRaw: UnorderedAccessView RWByteAddressBuffer",
            ],
            new[] { "Structured", "Raw", "Multisampled", "Typed", "Image2D", "Image3D", "RWStructured", "RWRaw" }.Select(Binding));

        var count = result.Bytecode.Reflection.ConstantBuffers.Single(c => c.Name == "Counts").Members.Single();
        Assert.Equal(EffectParameterType.UInt, count.Type.Type);
    }
}
