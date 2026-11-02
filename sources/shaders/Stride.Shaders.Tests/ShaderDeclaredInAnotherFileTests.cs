// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compilers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A file that also declares the class of another file: the class stays the one of its own file, in the running
/// session and in the shader cache on disk. A reload goes as the file watcher does it, with
/// <see cref="ShaderSourceManager.DeleteObsoleteCache"/> for the file that changed, then a compile on the same loader.
/// </summary>
public class ShaderDeclaredInAnotherFileTests : IDisposable
{
    // The SPIR-V cache lives in the application cache, which outlives the test run: fresh names per run.
    private readonly string target = $"CacheTarget{Guid.NewGuid():N}";
    private readonly string other = $"CacheOther{Guid.NewGuid():N}";
    private readonly string user = $"CacheUser{Guid.NewGuid():N}";

    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-owner-src");
    private readonly FileSystemProvider sources;
    private readonly List<EffectCompiler> compilers = [];

    public ShaderDeclaredInAnotherFileTests()
    {
        sources = new FileSystemProvider($"/shadersources-{Guid.NewGuid():N}", sourceDir.FullName);
        Write(target, TargetCode);
        Write(other, OtherCode);
        Write(user, UserCode);
    }

    public void Dispose()
    {
        foreach (var compiler in compilers)
            compiler.Dispose();
        sources.Dispose();
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private string TargetCode => $$"""
        shader {{target}}
        {
            int TargetValue() { return 1; }
        }
        """;

    private string OtherCode => $$"""
        shader {{other}}
        {
            int OtherValue() { return 2; }
        }
        """;

    private string OtherAlsoDeclaringTarget => OtherCode + $$"""

        shader {{target}}
        {
        }
        """;

    private string OtherFixed => OtherCode + "\n// fixed\n";

    private string UserCode => $$"""
        shader {{user}} : {{target}}
        {
            int UserValue() { return TargetValue(); }
        }
        """;

    [Fact]
    public void ClassStaysItsOwnWhileAnotherFileDeclaresIt()
    {
        var session = NewSession();
        AssertOriginal(session);

        Reload(session, other, OtherAlsoDeclaringTarget);

        AssertOriginal(session);
    }

    [Fact]
    public void ClassStaysItsOwnWithANewCacheInstanceWhileAnotherFileDeclaresIt()
    {
        var session = NewSession();
        AssertOriginal(session);

        Reload(session, other, OtherAlsoDeclaringTarget);

        AssertOriginal(NewSession());
    }

    [Fact]
    public void ClassIsItsOwnWhenAnotherFileCompiledFirst()
    {
        Write(other, OtherAlsoDeclaringTarget);
        var session = NewSession();
        Load(session, other);

        AssertOriginal(session);
    }

    [Fact]
    public void ClassCompilesAgainOnceTheOtherFileIsFixed()
    {
        var session = NewSession();
        Load(session, target);
        Reload(session, other, OtherAlsoDeclaringTarget);

        Reload(session, other, OtherFixed);

        AssertOriginal(session);
    }

    [Fact]
    public void ClassCompilesAgainWithANewCacheInstanceOnceTheOtherFileIsFixed()
    {
        var session = NewSession();
        Load(session, target);
        Reload(session, other, OtherAlsoDeclaringTarget);

        Reload(session, other, OtherFixed);

        AssertOriginal(NewSession());
    }

    [Fact]
    public void ClassCompilesAgainWithANewCacheInstanceWhenTheOtherFileIsFixedWhileClosed()
    {
        var session = NewSession();
        Load(session, target);
        Reload(session, other, OtherAlsoDeclaringTarget);

        Write(other, OtherFixed);

        AssertOriginal(NewSession());
    }

    // The in-memory cache keeps one hash per class, whatever the macros
    [Fact]
    public void ClassStaysItsOwnAfterACompileWithOtherMacros()
    {
        var session = NewSession();
        Reload(session, other, OtherAlsoDeclaringTarget);

        Assert.Contains("TargetValue", Load(session, target, new ShaderMacro("OTHER_MACROS", "1")));

        AssertOriginal(session);
    }

    // A pasted copy holds only the other class, byte for byte, so both files have the same hash
    [Fact]
    public void PastedCopyOfAClassIsHarmless()
    {
        var session = NewSession();
        AssertOriginal(session);

        Reload(session, other, TargetCode);
        AssertOriginal(session);

        Reload(session, other, OtherFixed);
        AssertOriginal(session);
        AssertOriginal(NewSession());
    }

    [Fact]
    public void ClassWithNoFileOfItsOwnStaysUsable()
    {
        var second = $"CacheSecond{Guid.NewGuid():N}";
        Write(other, $$"""
            {{OtherCode}}

            shader {{second}}
            {
                int SecondValue() { return 3; }
            }
            """);

        var session = NewSession();
        Load(session, other);
        Assert.Contains("SecondValue", Load(session, second));
        Assert.Contains("SecondValue", Load(NewSession(), second));
    }

    // A check that rejects too much still gives the right class: it would only show as slow compiles
    [Fact]
    public void OrdinaryClassStaysACacheHit()
    {
        var session = NewSession();
        Assert.False(IsFromCache(session, user));
        Assert.True(IsFromCache(session, user));
        Assert.True(IsFromCache(session, target));

        var newSession = NewSession();
        Assert.True(IsFromCache(newSession, user));
        Assert.True(IsFromCache(newSession, target));
    }

    private void AssertOriginal(FileShaderLoader session)
    {
        Assert.Contains("TargetValue", Load(session, target));
        Assert.Contains("UserValue", Load(session, user));
    }

    private void Write(string name, string code) => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    private void Reload(FileShaderLoader session, string name, string code)
    {
        Write(name, code);
        session.SourceManager.DeleteObsoleteCache([name]);
        Load(session, name);
    }

    private static string Load(FileShaderLoader session, string name, params ShaderMacro[] macros)
    {
        Assert.True(session.LoadExternalBuffer(name, macros, out var buffer, out _, out _), $"could not load shader {name}");
        return Spv.Dis(buffer, DisassemblerFlags.Name);
    }

    private static bool IsFromCache(FileShaderLoader session, string name)
    {
        Assert.True(session.LoadExternalBuffer(name, [], out _, out _, out var isFromCache), $"could not load shader {name}");
        return isFromCache;
    }

    /// <summary>The loader of a new <see cref="EffectCompiler"/>, with an empty in-memory cache over the application cache.</summary>
    private FileShaderLoader NewSession()
    {
        var compiler = new EffectCompiler(sources);
        compilers.Add(compiler);
        compiler.SourceDirectories.Add("");
        return compiler.GetFileShaderLoader();
    }
}
