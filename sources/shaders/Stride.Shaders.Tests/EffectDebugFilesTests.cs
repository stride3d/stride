// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;
using Stride.Core.IO;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The files the effect compiler writes next to its cache for each compiled effect.
/// </summary>
public class EffectDebugFilesTests : IDisposable
{
    // The application cache outlives the test run: a fresh shader name per run keeps it from what an earlier run left
    private readonly string shaderName = $"DebugFiles{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly EffectCompiler compiler;

    public EffectDebugFilesTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"debugfiles-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/debugfilestest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");

        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        File.WriteAllText(shaderPath, $$"""
            shader {{shaderName}} : SpriteBase
            {
                stage override float4 Shading()
                {
                    return base.Shading().yxzw;
                }
            };
            """);
        compiler.UrlToFilePath[$"{sourceDir.Name}/{shaderName}.sdsl"] = shaderPath;
    }

    public void Dispose()
    {
        compiler.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        var effectDir = EffectDir();
        if (Directory.Exists(effectDir))
            Directory.Delete(effectDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string EffectDir() => Path.Combine(PlatformFolders.ApplicationCacheDirectory, EffectCompilerCache.GetEffectCacheDirectory(shaderName));

    [Fact]
    public void ASuccessfulCompileWritesTheSpirvButNotItsDisassembly()
    {
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Vulkan },
        };
        var log = compiler.Compile(new ShaderClassSource(shaderName), parameters).Bytecode.WaitForResult().CompilationLog;
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var files = Directory.GetFiles(EffectDir());
        Assert.Contains(files, f => f.EndsWith(".spv", StringComparison.Ordinal));
        Assert.DoesNotContain(files, f => f.EndsWith(".spvdis", StringComparison.Ordinal));
    }
}
