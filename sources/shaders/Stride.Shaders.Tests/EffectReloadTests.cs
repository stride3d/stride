// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Issue #3392, shader reloading works only once. Models what the runtime does when a shader file
/// changes: <see cref="EffectCompilerBase.ResetCache"/> for the modified shader, then a recompile of
/// the effects that used it. Each edit in a row must produce its own bytecode.
/// </summary>
public class EffectReloadTests : IDisposable
{
    // The effect database and the SPIR-V cache both live in the application cache, which outlives the
    // test run, so a fresh shader name per run keeps this from depending on what an earlier run left.
    private readonly string shaderName = $"ReloadTest{Guid.NewGuid():N}";

    // Sources are looked up through a provider rather than with EffectCompiler.UseFileSystem, which
    // ShaderSourceManager only honours on Windows. The shaders live in a directory under the test
    // output so one mount covers both them and the engine's, and UrlToFilePath keeps the recorded
    // source path the real file, the way EffectCompileCommand sets it up for the asset pipeline.
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly DatabaseFileProvider database;
    private readonly EffectCompiler compiler;
    private readonly EffectCompilerCache cache;

    public EffectReloadTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"reload-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/reloadtest-{Guid.NewGuid():N}", AppContext.BaseDirectory);

        database = new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase());
        compiler = new EffectCompiler(sourceProvider);
        // Relative to the provider's base path: it resolves a url by appending it, mount point included.
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");
        cache = new EffectCompilerCache(compiler, database) { CompileEffectAsynchronously = false };
        compiler.UrlToFilePath[ShaderUrl] = ShaderPath;
    }

    private string ShaderPath => Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");

    private string ShaderUrl => $"{sourceDir.Name}/{shaderName}.sdsl";

    public void Dispose()
    {
        cache.Dispose();
        compiler.Dispose();
        database.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private string ShaderWithColor(string color) => $$"""
        shader {{shaderName}} : SpriteBase
        {
            stage override float4 Shading()
            {
                return float4({{color}});
            }
        };
        """;

    [Fact]
    public void EveryShaderEditIsSeen()
    {
        var red = CompileAfterEdit(ShaderWithColor("1, 0, 0, 1"));
        var green = CompileAfterEdit(ShaderWithColor("0, 1, 0, 1"));
        var blue = CompileAfterEdit(ShaderWithColor("0, 0, 1, 1"));

        Assert.NotEqual(red, green);
        // The second reload is the one that used to be dropped: the effect's input hash doesn't change
        // when a shader file does, so the result compiled by the first reload was memoized forever.
        Assert.NotEqual(green, blue);
    }

    /// <summary>Writes a new version of the shader, then reloads it the way the runtime does.</summary>
    private byte[] CompileAfterEdit(string code)
    {
        File.WriteAllText(ShaderPath, code);
        cache.ResetCache([shaderName]);

        // Vulkan rather than the Direct3D11 default: it consumes SPIR-V directly, so this compiles on
        // every platform, where the D3D backend throws NotSupportedException off Windows.
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Vulkan },
        };
        var results = cache.Compile(new ShaderClassSource(shaderName), parameters);
        Assert.False(results.HasErrors, string.Join(Environment.NewLine, results.Messages.Select(m => m.Text)));

        var completed = results.Bytecode.WaitForResult();
        Assert.False(completed.CompilationLog.HasErrors,
            string.Join(Environment.NewLine, completed.CompilationLog.Messages.Select(m => m.Text)));
        return completed.Bytecode.Stages[0].Data;
    }
}
