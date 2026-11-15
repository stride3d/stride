// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// FXC compiles the HLSL that SPIRV-Cross generates for Direct3D 11. Its messages are about generated code:
/// a game shows only their errors, and they name the effect and the stage rather than an address.
/// </summary>
public class ShaderBackendLogTests : IDisposable
{
    private const string FxcModule = "ShaderBackend.FXC";

    // The application cache outlives the test run: a fresh shader name per run keeps it from what an earlier run left
    private readonly string shaderName = $"BackendLog{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly EffectCompiler compiler;

    public ShaderBackendLogTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"backendlog-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/backendlogtest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");

        // Inlining a function with two returns gives a loop that runs once, which FXC warns about (X3557)
        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        File.WriteAllText(shaderPath, $$"""
            shader {{shaderName}} : SpriteBase
            {
                float4 Pick(float4 color)
                {
                    if (color.x > 0.5)
                        return color;
                    return color.yxzw;
                }

                stage override float4 Shading()
                {
                    return Pick(base.Shading());
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
        GC.SuppressFinalize(this);
    }

    private LoggerResult Compile()
    {
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Direct3D11 },
        };
        var log = compiler.Compile(new ShaderClassSource(shaderName), parameters).Bytecode.WaitForResult().CompilationLog;
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        return log;
    }

    [Fact]
    public void FxcWarningsNameTheEffectAndTheStage()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("FXC is only on Windows");

        var warnings = Compile().Messages.Where(m => m.Module == FxcModule).ToList();

        Assert.NotEmpty(warnings);
        Assert.All(warnings, w => Assert.StartsWith($"{shaderName}_ps.spirv-cross.hlsl(", w.Text));
    }

    // Messages created without a module, by the shader loader for example, get the one of the compile log
    [Fact]
    public void CompileMessagesCarryAModule()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("FXC is only on Windows");

        var messages = Compile().Messages;

        Assert.Contains(messages, m => m.Module == EffectCompileLog.EffectCompilerModule && m.Text.Contains("references non-stage method"));
        Assert.All(messages, m => Assert.NotNull(m.Module));
    }

    // The result keeps the messages; the place that logs it filters them, so a game also filters what the effect compiler server sends
    [Fact]
    public void FxcWarningsAreLoggedOnlyWhenTheirModuleLetsThemThrough()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("FXC is only on Windows");

        var result = Compile();

        var hidden = new LoggerResult();
        ShaderBackendLog.CopyTo(result, hidden);
        Assert.DoesNotContain(hidden.Messages, m => m.Module == FxcModule);

        GlobalLogger.SetModuleLevel(FxcModule, LogMessageType.Warning);
        try
        {
            var shown = new LoggerResult();
            ShaderBackendLog.CopyTo(result, shown);
            Assert.Contains(shown.Messages, m => m.Module == FxcModule && m.Type == LogMessageType.Warning);
        }
        finally
        {
            GlobalLogger.SetModuleLevel(FxcModule, LogMessageType.Error);
        }
    }
}
