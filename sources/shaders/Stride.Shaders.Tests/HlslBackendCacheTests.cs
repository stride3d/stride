// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;
using Stride.Core.IO;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// On Direct3D 11, effects that give the same SPIR-V, or the same HLSL for a stage, share the work of SPIRV-Cross and FXC.
/// </summary>
public class HlslBackendCacheTests : IDisposable
{
    // The application cache outlives the test run: fresh shader names per run keep it from what an earlier run left
    private readonly string redShader = $"BackendCacheRed{Guid.NewGuid():N}";
    private readonly string greenShader = $"BackendCacheGreen{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly EffectCompiler compiler;

    public HlslBackendCacheTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"backendcache-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/backendcachetest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");

        WriteShader(redShader, "1, 0, 0, 1");
        WriteShader(greenShader, "0, 1, 0, 1");
    }

    public void Dispose()
    {
        compiler.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        foreach (var shaderName in new[] { redShader, greenShader })
        {
            var effectDir = Path.Combine(PlatformFolders.ApplicationCacheDirectory, EffectCompilerCache.GetEffectCacheDirectory(shaderName));
            if (Directory.Exists(effectDir))
                Directory.Delete(effectDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private void WriteShader(string shaderName, string color)
    {
        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        File.WriteAllText(shaderPath, $$"""
            shader {{shaderName}} : SpriteBase
            {
                module override float4 Shading()
                {
                    return float4({{color}});
                }
            };
            """);
        compiler.UrlToFilePath[$"{sourceDir.Name}/{shaderName}.sdsl"] = shaderPath;
    }

    // The effect compiler alone, without the effect cache in front of it: each call compiles
    private EffectBytecode Compile(string shaderName)
    {
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Direct3D11 },
        };
        var completed = compiler.Compile(new ShaderClassSource(shaderName), parameters).Bytecode.WaitForResult();
        Assert.False(completed.CompilationLog.HasErrors,
            string.Join(Environment.NewLine, completed.CompilationLog.Messages.Select(m => m.Text)));
        return completed.Bytecode!;
    }

    [Fact]
    public void AnEffectCompiledAgainReusesTheBytecodeOfEachStage()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        var first = Compile(redShader);
        var second = Compile(redShader);

        Assert.Equal(2, first.Stages.Length);
        Assert.All(first.Stages.Zip(second.Stages), pair => Assert.Same(pair.First.Data, pair.Second.Data));
        // The reflection is filled from the bytecode again: it is the same
        Assert.Equal(first.ComputeId(), second.ComputeId());
    }

    [Fact]
    public void EffectsWithTheSameVertexShaderShareItsBytecode()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        var red = Compile(redShader);
        var green = Compile(greenShader);

        Assert.Same(red.Stages.Single(s => s.Stage == ShaderStage.Vertex).Data, green.Stages.Single(s => s.Stage == ShaderStage.Vertex).Data);
        Assert.NotEqual(red.Stages.Single(s => s.Stage == ShaderStage.Pixel).Id, green.Stages.Single(s => s.Stage == ShaderStage.Pixel).Id);
    }
}
