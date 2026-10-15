// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Partial structs: <c>partial struct X</c> in its owner, <c>extend struct X</c> in classes that import the owner. A class
/// sees the fields of the parts it imports; the effect gets one struct with every part. Values are checked by the
/// RenderTests <c>PartialStructParts</c> and <c>PartialStructNested</c>.
/// </summary>
public class PartialStructTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-partial-struct");

    public PartialStructTests()
    {
        Write("PsOwner", "module PsOwner { partial struct PsData { float a; }; }");
        Write("PsB", "module PsB { import PsOwner; extend struct PsData { float b; }; float GetB(PsData d) { return d.b; } }");
        Write("PsB2", "module PsB2 { import PsOwner; extend struct PsData { float b; }; }");
        Write("PsC", "module PsC { import PsOwner; extend struct PsData { float c; }; }");
        Write("PsLayerBase", "shader PsLayerBase { import PsOwner; float Read(PsData d) { return 0; } }");
    }

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    // A class compiled alone sees only the fields of the parts it imports
    [Theory]
    [InlineData("import PsOwner;", "SDSL0113: Could not resolve member b")]
    [InlineData("import PsB;", null)]
    public void ClassSeesTheFieldsOfThePartsItImports(string imports, string? expected)
    {
        Write("PsReader", $$"""
            shader PsReader : ComputeShaderBase
            {
                {{imports}}
                RWBuffer<float4> Out;
                override void Compute() { PsData d = (PsData)0; Out[0] = d.b; }
            }
            """);

        var (ok, log) = Compile("PsReader");
        if (expected == null)
            Assert.True(ok, log);
        else
            Assert.Contains(expected, log);
    }

    // The effect's struct has every part: owner first, then the parts by class name, whatever the import order
    [Fact]
    public void EffectJoinsThePartsByClassName()
    {
        Write("PsLayer", "shader PsLayer : PsLayerBase { import PsC; override float Read(PsData d) { return d.c; } }");
        Write("PsRoot", """
            shader PsRoot : ComputeShaderBase
            {
                import PsB;
                compose PsLayerBase layer;
                RWBuffer<float4> Out;
                override void Compute() { PsData d = (PsData)0; Out[0] = layer.Read(d) + GetB(d); }
            }
            """);

        var (ok, log, explanation) = Mix("PsRoot", "PsLayer");
        Assert.True(ok, log);
        Assert.Contains("""
            partial struct PsData (owner PsOwner): 3 fields, 12 bytes
              field PsData.a : float (PsOwner)
              field PsData.b : float (PsB)
              field PsData.c : float (PsC)
            """.ReplaceLineEndings(), explanation);
    }

    // SDSL0120: two parts add the same field, even with the same type
    [Fact]
    public void SameFieldTwiceInOneClassIsAnError()
    {
        Write("PsBoth", "module PsBoth { import PsB; import PsB2; }");

        var (ok, log) = Compile("PsBoth");
        Assert.False(ok);
        Assert.Contains("SDSL0120: PsData.b is declared by both PsB and PsB2. Rename one, or move the field into a module both import.", log);
    }

    // SDSL0120 at link: the two parts meet only in the effect
    [Fact]
    public void SameFieldTwiceInTheEffectIsAnError()
    {
        Write("PsLayer", "shader PsLayer : PsLayerBase { import PsB2; override float Read(PsData d) { return d.b; } }");
        Write("PsRoot", """
            shader PsRoot : ComputeShaderBase
            {
                import PsB;
                compose PsLayerBase layer;
                RWBuffer<float4> Out;
                override void Compute() { PsData d = (PsData)0; Out[0] = layer.Read(d) + GetB(d); }
            }
            """);

        var (ok, log, _) = Mix("PsRoot", "PsLayer");
        Assert.False(ok);
        Assert.Contains("SDSL0120: PsData.b is declared by both PsB and PsB2.", log);
    }

    [Theory]
    // SDSL0121: nothing to extend
    [InlineData("module PsCheck { extend struct PsData { float d; }; }", "SDSL0121: PsCheck extends struct PsData, but no partial struct PsData is visible. Import (or inherit) the class that declares 'partial struct PsData'.")]
    // SDSL0122: the visible struct is not partial
    [InlineData("module PsCheck { import PsPlain; extend struct PsPlainData { float d; }; }", "SDSL0122: PsCheck extends struct PsPlainData, but PsPlain declares PsPlainData without 'partial', so it can't get more fields. Add 'partial' to its declaration in PsPlain, or rename this struct.")]
    // SDSL0123: a second declaration of a visible partial struct
    [InlineData("module PsCheck { import PsOwner; struct PsData { float d; }; }", "SDSL0123: PsCheck declares struct PsData, but PsOwner already declares 'partial struct PsData'. Write 'extend struct PsData' to add fields to it, or rename this struct.")]
    [InlineData("module PsCheck { import PsOwner; partial struct PsData { float d; }; }", "SDSL0123: PsCheck declares struct PsData, but PsOwner already declares 'partial struct PsData'.")]
    // SDSL0124: two owners of one name
    [InlineData("module PsCheck { import PsOwner; import PsOwner2; }", "SDSL0124: PsCheck sees two partial structs named PsData, declared by PsOwner and PsOwner2. Rename one of them.")]
    // SDSL0126: an owner per effect
    [InlineData("module PsCheck<int N> { partial struct PsGeneric { float a; }; }", "SDSL0126: PsCheck declares 'partial struct PsGeneric', but PsCheck is generic.")]
    // SDSL0128: a struct that contains itself, directly, through an array, through another partial struct or a plain struct
    [InlineData("module PsCheck { partial struct PsSelf { float a; PsSelf inner; }; }", "SDSL0128: struct PsSelf contains itself: PsSelf.inner (PsCheck) -> PsSelf. A struct can't contain itself, directly or through an array. Remove one of these fields.")]
    [InlineData("module PsCheck { partial struct PsSelf { PsSelf items[2]; }; }", "SDSL0128: struct PsSelf contains itself: PsSelf.items (PsCheck) -> PsSelf.")]
    [InlineData("module PsCheck { partial struct PsP { PsQ q; }; partial struct PsQ { PsP p; }; }", "SDSL0128: struct PsP contains itself: PsP.q (PsCheck) -> PsQ.p (PsCheck) -> PsP.")]
    [InlineData("module PsCheck { import PsOwner; struct PsHolder { PsData d; }; extend struct PsData { PsHolder h; }; }", "SDSL0128: struct PsHolder contains itself: PsHolder.d (PsCheck) -> PsData.h (PsCheck) -> PsHolder.")]
    [InlineData("module PsCheck { import PsHolderModule; extend struct PsData { PsHolder h; }; }", "SDSL0128: struct PsData contains itself: PsData.h (PsCheck) -> PsHolder.d (PsHolderModule) -> PsData.")]
    [InlineData("module PsCheck { struct PsPlainSelf { float a; PsPlainSelf b; }; }", "SDSL0128: struct PsPlainSelf contains itself: PsPlainSelf.b (PsCheck) -> PsPlainSelf.")]
    public void DeclarationErrors(string code, string expected)
    {
        Write("PsPlain", "module PsPlain { struct PsPlainData { float a; }; }");
        Write("PsOwner2", "module PsOwner2 { partial struct PsData { float z; }; }");
        Write("PsHolderModule", "module PsHolderModule { import PsOwner; struct PsHolder { PsData d; }; }");
        Write("PsCheck", code);

        var (ok, log) = Compile("PsCheck");
        Assert.False(ok);
        Assert.Contains(expected, log);
    }

    // A field of a partial struct can have a struct type: a plain struct of this class or another, an array of one,
    // another partial struct; a plain struct can hold a partial struct that holds a struct
    [Theory]
    [InlineData("module PsCheck { import PsPlain; partial struct PsWithStruct { PsPlainData inner; }; }")]
    [InlineData("module PsCheck { partial struct PsWithStruct { PsOwn inner; PsOwn items[2]; }; struct PsOwn { float a; }; }")]
    [InlineData("module PsCheck { import PsOwner; extend struct PsData { PsInner inner; }; partial struct PsInner { float x; }; }")]
    [InlineData("module PsCheck { import PsOwner; struct PsHolder { PsData d; }; struct PsPair { float2 v; }; extend struct PsData { PsPair pair; }; }")]
    public void FieldsCanBeStructs(string code)
    {
        Write("PsPlain", "module PsPlain { struct PsPlainData { float a; }; }");
        Write("PsCheck", code);

        var (ok, log) = Compile("PsCheck");
        Assert.True(ok, log);
    }

    // The explain lines count a struct field with its fields (a partial struct field with the fields of the effect)
    [Fact]
    public void ExplainCountsStructFields()
    {
        Write("PsInnerOwner", "module PsInnerOwner { partial struct PsInner { float x; }; }");
        Write("PsInnerY", "module PsInnerY { import PsInnerOwner; extend struct PsInner { float2 y; }; }");
        Write("PsWithStruct", "module PsWithStruct { import PsOwner; import PsInnerOwner; struct PsPair { float2 v; }; extend struct PsData { PsPair pair; PsInner inner; }; }");
        Write("PsLayer", "shader PsLayer : PsLayerBase { import PsInnerY; }");
        Write("PsRoot", """
            shader PsRoot : ComputeShaderBase
            {
                import PsWithStruct;
                compose PsLayerBase layer;
                RWBuffer<float4> Out;
                override void Compute() { PsData d = (PsData)0; Out[0] = layer.Read(d) + d.pair.v.x + d.inner.x; }
            }
            """);

        var (ok, log, explanation) = Mix("PsRoot", "PsLayer");
        Assert.True(ok, log);
        Assert.Contains("""
            partial struct PsData (owner PsOwner): 3 fields, 24 bytes
              field PsData.a : float (PsOwner)
              field PsData.pair : PsPair (PsWithStruct)
              field PsData.inner : PsInner (PsWithStruct)
            partial struct PsInner (owner PsInnerOwner): 2 fields, 12 bytes
            """.ReplaceLineEndings(), explanation);
    }

    // SDSL0128 at link: each class sees one half of the cycle, the effect has both
    [Fact]
    public void CycleInTheEffectIsAnError()
    {
        Write("PsCycleP", "module PsCycleP { partial struct PsP { float a; }; }");
        Write("PsCycleQ", "module PsCycleQ { partial struct PsQ { float b; }; }");
        Write("PsAddQ", "module PsAddQ { import PsCycleP; import PsCycleQ; extend struct PsP { PsQ q; }; float GetA(PsP p) { return p.a; } }");
        Write("PsAddP", "module PsAddP { import PsCycleP; import PsCycleQ; extend struct PsQ { PsP p; }; float GetB(PsQ q) { return q.b; } }");
        Write("PsLayer", "shader PsLayer : PsLayerBase { import PsAddP; override float Read(PsData d) { PsQ q = (PsQ)0; return GetB(q); } }");
        Write("PsRoot", """
            shader PsRoot : ComputeShaderBase
            {
                import PsOwner;
                import PsAddQ;
                compose PsLayerBase layer;
                RWBuffer<float4> Out;
                override void Compute() { PsData d = (PsData)0; PsP p = (PsP)0; Out[0] = layer.Read(d) + GetA(p); }
            }
            """);

        var (ok, log, _) = Mix("PsRoot", "PsLayer");
        Assert.False(ok);
        Assert.Contains("SDSL0128: struct PsP contains itself: PsP.q (PsAddQ) -> PsQ.p (PsAddP) -> PsP.", log);
    }

    // SDSL0125: where the layout is shared (streams, cbuffers, buffers, groupshared), also through a plain struct
    [Theory]
    [InlineData("static PsData value;", null)]
    [InlineData("static PsHolder value;", null)]
    [InlineData("stream PsData value;", "SDSL0125: stream member value has type PsData, which is or contains the partial struct PsData.")]
    [InlineData("stream PsHolder value;", "SDSL0125: stream member value has type PsHolder, which is or contains the partial struct PsData.")]
    [InlineData("cbuffer PerDraw { PsData value; };", "SDSL0125: PerDraw member value has type PsData")]
    [InlineData("PsData value;", "SDSL0125: member (in the global cbuffer) value has type PsData")]
    [InlineData("RWStructuredBuffer<PsHolder> value;", "SDSL0125: buffer value has type RWStructuredBuffer<PsHolder>")]
    [InlineData("groupshared PsData value;", "SDSL0125: groupshared variable value has type PsData")]
    public void FixedLayoutsCantHoldAPartialStruct(string member, string? expected)
    {
        Write("PsCheck", $$"""
            shader PsCheck
            {
                import PsOwner;
                struct PsHolder { float pad; PsData data; };
                {{member}}
            }
            """);

        var (ok, log) = Compile("PsCheck");
        if (expected == null)
        {
            Assert.True(ok, log);
        }
        else
        {
            Assert.False(ok);
            Assert.Contains(expected, log);
        }
    }

    // SDSL0214: 'partial' on a class was accepted and ignored
    [Theory]
    [InlineData("partial shader PsCheck { }")]
    [InlineData("[Root] partial shader PsCheck { }")]
    [InlineData("partial module PsCheck { }")]
    public void PartialOnAClassIsIgnored(string code)
    {
        Write("PsCheck", code);

        var (ok, log) = Compile("PsCheck");
        Assert.True(ok, log);
        Assert.Contains("SDSL0214: 'partial' on PsCheck has no meaning and is ignored. Remove it.", log);
    }

    // SDSL0215: two plain structs of one name, the first import wins; one struct reached twice is fine
    [Theory]
    [InlineData("import PsS1; import PsS2;", "Warning: PsCheck.sdsl(2,8): SDSL0215: PsCheck sees two structs named S, declared by PsS1 and PsS2: S means the one of PsS1 (the first inherited or imported). Rename one of them.")]
    [InlineData("import PsS1; import PsS1User;", null)]
    public void TwoPlainStructsOfOneName(string imports, string? expected)
    {
        Write("PsS1", "module PsS1 { struct S { float a; }; }");
        Write("PsS2", "module PsS2 { struct S { float b; }; }");
        Write("PsS1User", "module PsS1User { import PsS1; float Get(S s) { return s.a; } }");
        Write("PsCheck", $"module PsCheck {{ {imports} }}");

        var (ok, log) = Compile("PsCheck");
        Assert.True(ok, log);
        if (expected == null)
            Assert.DoesNotContain("SDSL0215", log);
        else
            Assert.Contains(expected, log);
    }

    // Compiles one class alone (and the classes it uses), like the first effect that needs it would
    private (bool Ok, string Log) Compile(string name)
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };
        bool ok;
        try
        {
            ok = loader.LoadExternalBuffer(name, [], out _, out _, out _);
        }
        catch (InvalidOperationException)
        {
            ok = false;
        }
        return (ok, Messages(log));
    }

    private (bool Ok, string Log, string? Explanation) Mix(string root, string layer)
    {
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource(root) },
            Compositions = { ["layer"] = new ShaderMixinSource { Mixins = { new ShaderClassSource(layer) } } },
        };
        var log = new LoggerResult();
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL")) { CollectExplanation = true };
        bool ok;
        try
        {
            ok = mixer.MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);
        }
        catch (InvalidOperationException e)
        {
            ok = false;
            log.Error(e.Message);
        }
        return (ok, Messages(log), mixer.Explanation);
    }

    // File names without their folder, so the messages read the same on every machine
    private string Messages(LoggerResult log)
        => string.Join(Environment.NewLine, log.Messages.Select(m => $"{m.Type}: {m.Text.Replace(sourceDir.FullName + Path.DirectorySeparatorChar, "").Replace(sourceDir.FullName + "/", "")}"));

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), $"namespace Stride.Shaders.Tests;\n{code}\n");
}
