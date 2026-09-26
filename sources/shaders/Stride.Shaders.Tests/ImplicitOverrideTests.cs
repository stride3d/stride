// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A method that implements an <c>abstract</c> one without the <c>override</c> keyword. The old
/// mixer let the name and signature stand in for the keyword, so the engine's hair and subsurface
/// scattering functions were written that way and worked; this mixer keeps the abstract method as
/// the most derived member of its group and refuses to call it, which is how those two materials
/// stopped compiling in 4.4. These tests pin both halves down: the keyword makes it work, and its
/// absence fails with a message that names the abstract method, so a reader can find the missing keyword.
/// </summary>
public class ImplicitOverrideTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-override");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Contract = """
        namespace Stride.Shaders.Tests;

        shader OverrideContract
        {
            abstract int Value();
        }
        """;

    private const string ImplicitImplementation = """
        namespace Stride.Shaders.Tests;

        shader OverrideImplementation : OverrideContract
        {
            int Value() { return 7; }
        }
        """;

    private const string ExplicitImplementation = """
        namespace Stride.Shaders.Tests;

        shader OverrideImplementation : OverrideContract
        {
            override int Value() { return 7; }
        }
        """;

    private const string Root = """
        namespace Stride.Shaders.Tests;

        shader OverrideRoot : ComputeShaderBase
        {
            compose OverrideContract contract;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = contract.Value(); }
        }
        """;

    [Fact]
    public void ExplicitOverrideOfAnAbstractMethodIsCalledThroughAComposition()
    {
        Write("OverrideContract", Contract);
        Write("OverrideImplementation", ExplicitImplementation);
        Write("OverrideRoot", Root);

        var (ok, log) = Mix();
        Assert.True(ok, log);
    }

    // The shape of Stride.Rendering's hair and subsurface-scattering functions before they gained the
    // keyword. Whether the mixer should accept it as the old one did is a language decision; until it
    // does, the error must at least say which abstract method was left unimplemented.
    [Fact]
    public void MissingOverrideOfAnAbstractMethodIsReportedByName()
    {
        Write("OverrideContract", Contract);
        Write("OverrideImplementation", ImplicitImplementation);
        Write("OverrideRoot", Root);

        var (ok, log) = Mix();
        Assert.False(ok);
        Assert.Contains("OverrideContract.Value", log);
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    private (bool ok, string log) Mix()
    {
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("OverrideRoot") },
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("OverrideImplementation") } } },
        };

        var log = new LoggerResult();
        var ok = new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);

        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }
}
