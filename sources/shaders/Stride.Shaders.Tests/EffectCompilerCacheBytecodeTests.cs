// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The effect system makes one <c>Effect</c> per <see cref="EffectBytecode"/> object, so the cache must give
/// one object per compiled bytecode, or the draws of one permutation get several effects and pipeline states.
/// </summary>
public class EffectCompilerCacheBytecodeTests : IDisposable
{
    // The application cache outlives the test run: a fresh shader name per run keeps it from what an earlier run left
    private readonly string shaderName = $"BytecodeTest{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly DatabaseFileProvider database;
    private readonly EffectCompiler compiler;
    private readonly EffectCompilerCache cache;

    public EffectCompilerCacheBytecodeTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"bytecode-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/bytecodetest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        database = new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase());
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");
        cache = new EffectCompilerCache(compiler, database);

        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        WriteShader(shaderPath, "1, 0, 0, 1");
        compiler.UrlToFilePath[$"{sourceDir.Name}/{shaderName}.sdsl"] = shaderPath;
    }

    public void Dispose()
    {
        cache.Dispose();
        compiler.Dispose();
        database.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private void WriteShader(string path, string color) => File.WriteAllText(path, $$"""
        shader {{shaderName}} : SpriteBase
        {
            stage override float4 Shading()
            {
                return float4({{color}});
            }
        };
        """);

    private EffectBytecode Compile(ShaderSource source)
    {
        // Vulkan: it consumes SPIR-V directly, so this compiles on every platform
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Vulkan },
        };
        var completed = cache.Compile(source, parameters).Bytecode.WaitForResult();
        Assert.False(completed.CompilationLog.HasErrors,
            string.Join(Environment.NewLine, completed.CompilationLog.Messages.Select(m => m.Text)));
        return completed.Bytecode!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnePermutationGivesOneBytecode(bool compileAsynchronously)
    {
        cache.CompileEffectAsynchronously = compileAsynchronously;

        // The second request finds the first in the application cache
        var compiled = Compile(new ShaderClassSource(shaderName));
        var loaded = Compile(new ShaderClassSource(shaderName));
        var loadedAgain = Compile(new ShaderClassSource(shaderName));

        Assert.Same(compiled, loaded);
        Assert.Same(compiled, loadedAgain);
    }

    [Fact]
    public void TwoPermutationsWithOneOutputGiveOneBytecode()
    {
        var plain = Compile(new ShaderClassSource(shaderName));

        // A macro the shader doesn't read: another permutation, the same bytecode
        var withMacro = new ShaderMixinSource { Name = shaderName };
        withMacro.Mixins.Add(new ShaderClassSource(shaderName));
        withMacro.AddMacro("UNUSED_BY_THE_SHADER", 1);
        var other = Compile(withMacro);

        Assert.Equal(plain.ComputeId(), other.ComputeId());
        Assert.Same(plain, other);
        Assert.Same(plain, Compile(withMacro));
    }

    [Fact]
    public void EditedShaderGivesANewBytecode()
    {
        var before = Compile(new ShaderClassSource(shaderName));

        WriteShader(Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl"), "0, 1, 0, 1");
        cache.ResetCache([shaderName]);
        var after = Compile(new ShaderClassSource(shaderName));

        Assert.NotEqual(before.ComputeId(), after.ComputeId());
        Assert.Same(after, Compile(new ShaderClassSource(shaderName)));
    }
}
