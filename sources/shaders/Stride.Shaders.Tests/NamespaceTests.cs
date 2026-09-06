// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;
using Xunit;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// What namespaces and <c>using</c> directives do in SDSL today: a class is resolved by its name
/// alone, one file per class, so a using takes no part in resolution and two classes of one name
/// in different namespaces cannot both be reached. These tests pin that down, so that a change
/// to it is a decision rather than an accident.
/// </summary>
public class NamespaceTests
{
    private static (bool ok, string log, string disassembly) Compile(string root, params string[] files)
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var mixer = new ShaderMixer(loader);
        foreach (var name in files)
            mixer.ShaderLoader.LoadExternalBuffer(name, [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        var ok = mixer.MergeSDSL(new ShaderClassSource(root), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _);
        var text = string.Join(Environment.NewLine, log.Messages.Select(m => m.Text));
        var disassembly = ok ? Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true) : "";
        return (ok, text, disassembly);
    }

    // A `using` of the namespace a base class lives in compiles, and the base is found.
    [Fact]
    public void UsingDirectiveIsAcceptedAndTheClassIsFoundByName()
    {
        var (ok, log, disassembly) = Compile("NsUsingRoot", "ComposeSharedBase", "NsUtils", "NsUsingRoot");
        Assert.True(ok, log);
        Assert.Contains("NsUtils.One", disassembly);
    }

    // A `using` of a namespace nothing declares is not an error: the directive takes no part in
    // resolution, so there is nothing for it to fail to find.
    [Fact]
    public void UsingOfAnUnknownNamespaceIsIgnored()
    {
        var (ok, log, _) = Compile("NsUnknownUsingRoot", "ComposeSharedBase", "NsUtils", "NsUnknownUsingRoot");
        Assert.True(ok, log);
    }

    // Two classes of one name in two namespaces of one file: resolution is by name alone, so the
    // `using` - which names the first one's namespace - does not pick between them, and the last
    // declaration in the file is the one mixed in. Documents what happens today, so a compiler
    // that one day honours namespaces has to change this test knowingly.
    [Fact]
    public void SameNameInTwoNamespacesIsNotDisambiguatedByUsing()
    {
        var (ok, log, disassembly) = Compile("NsClashRoot", "ComposeSharedBase", "NsClashed", "NsClashRoot");
        Assert.True(ok, log);
        // One NsClashed.Which is mixed in, not two.
        Assert.Equal(1, disassembly.Split("OpName").Count(s => s.Contains("NsClashed.Which")));
        // And it is the second one - Which() returning 2 - despite the using naming the first's namespace.
        var constants = System.Text.RegularExpressions.Regex.Matches(disassembly, @"OpConstant %\S+ (\d+)").Select(m => m.Groups[1].Value).ToList();
        Assert.Contains("2", constants);
        Assert.DoesNotContain("1", constants);
    }
}
