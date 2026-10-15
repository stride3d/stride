// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// <c>[PushConstant] cbuffer</c>: the members go to the effect's push-constant block (Vulkan push constants, Direct3D 12
/// root constants, a constant buffer at b13 on Direct3D 11). Values are checked by the graphics test <c>TestPushConstants</c>.
/// </summary>
public class PushConstantTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-push-constant");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    // A push-constant cbuffer with module members, or in a root-only class
    [Theory]
    [InlineData("shader PcCheck { [PushConstant] cbuffer PerDraw { module uint Index; module float2 Scale; module int4 Ids; }; }")]
    [InlineData("module PcCheck { [PushConstant] cbuffer PerDraw { uint Index; float Value; }; }")]
    [InlineData("[Root] shader PcCheck { [PushConstant] cbuffer PerDraw { uint Index; uint2 Range; }; }")]
    public void PushConstantCBufferCompiles(string code)
    {
        Write("PcCheck", code);

        var (ok, log) = Compile("PcCheck");
        Assert.True(ok, log);
    }

    // SDSL0129: only 32-bit scalars and 2- or 4-component vectors of them
    [Theory]
    [InlineData("float3", "SDSL0129: PcCheck: member Value of the [PushConstant] cbuffer PerDraw has type float3. A push-constant member is an int, uint or float, or a 2- or 4-component vector of them (for example uint2 or float4). Change the type, or move the member to another cbuffer.")]
    [InlineData("bool", "SDSL0129: PcCheck: member Value of the [PushConstant] cbuffer PerDraw has type bool.")]
    [InlineData("double", "SDSL0129: PcCheck: member Value of the [PushConstant] cbuffer PerDraw has type double.")]
    [InlineData("float4x4", "SDSL0129: PcCheck: member Value of the [PushConstant] cbuffer PerDraw has type float4x4.")]
    [InlineData("uint[2]", null)]
    public void PushConstantMemberTypeIsChecked(string type, string? expected)
    {
        // An array is written after the name
        var member = type.EndsWith(']') ? $"module {type[..type.IndexOf('[')]} Value{type[type.IndexOf('[')..]};" : $"module {type} Value;";
        Write("PcCheck", $"shader PcCheck {{ [PushConstant] cbuffer PerDraw {{ {member} }}; }}");

        var (ok, log) = Compile("PcCheck");
        Assert.False(ok);
        Assert.Contains(expected ?? "SDSL0129: PcCheck: member Value of the [PushConstant] cbuffer PerDraw has type uint", log);
    }

    // SDSL0130: instance members in a shader that can be in a composition
    [Fact]
    public void PushConstantCBufferIsOncePerEffect()
    {
        Write("PcCheck", "shader PcCheck { [PushConstant] cbuffer PerDraw { uint Index; }; }");

        var (ok, log) = Compile("PcCheck");
        Assert.False(ok);
        Assert.Contains("SDSL0130: PcCheck: the [PushConstant] cbuffer PerDraw has instance members, so each composition instance of PcCheck would get its own copy, but an effect has one push-constant block. Mark its members 'module', or mark PcCheck [Root] if it is only mixed at the root of an effect.", log);
    }

    private const string Root = """
        shader PcRoot : ComputeShaderBase
        {
            import PcMaterial;
            [PushConstant] cbuffer PerDraw { module uint Index; module uint Count; };
            RWBuffer<uint4> Out;
            override void Compute() { Out[0] = uint4(Index, Count, MaterialIndex, asuint(Scale.y)); }
        }
        """;

    // Every [PushConstant] cbuffer of the effect, whatever its name, is in one block, with tight offsets
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EffectJoinsThePushConstantCBuffers(bool direct3D11)
    {
        Write("PcMaterial", "module PcMaterial { [PushConstant] cbuffer PerMaterial { float2 Scale; uint MaterialIndex; }; }");
        Write("PcRoot", Root);

        var (ok, log, reflection, bytecode) = Mix("PcRoot", direct3D11);
        Assert.True(ok, log);

        var pushConstants = reflection!.PushConstants;
        Assert.NotNull(pushConstants);
        Assert.Equal(ConstantBufferType.PushConstant, pushConstants.Type);
        Assert.Equal(20, pushConstants.Size);
        Assert.Equal(["Scale 0 8", "MaterialIndex 8 4", "Index 12 4", "Count 16 4"], pushConstants.Members.Select(x => $"{x.RawName} {x.Offset} {x.Size}"));
        // Not a descriptor-set resource
        Assert.DoesNotContain(reflection.ResourceGroups, x => x.Name is "PerDraw" or "PerMaterial" or "PushConstants");
        Assert.DoesNotContain(reflection.ResourceBindings, x => x.RawName is "PerDraw" or "PerMaterial" or "PushConstants");

        var words = MemoryMarshal.Cast<byte, uint>(bytecode);
        if (!direct3D11)
        {
            Assert.Null(SpirvTools.Validate(words));
        }
        else
        {
            // Direct3D 11: a cbuffer at b13
            SpirvCrossSupport.SkipUnlessAvailable();
            var translator = new SpirvTranslator(SpirvTools.LegalizeForHlsl(words).AsMemory());
            var hlsl = translator.Translate(Backend.Hlsl, translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute));
            Assert.Matches(@"cbuffer \w+ : register\(b13\)", hlsl);
        }
    }

    // Two parts may have a member of the same name: the access chains use indices, and SPIRV-Cross renames the second one
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PartsMayHaveMembersOfTheSameName(bool direct3D11)
    {
        Write("PcMaterial", "module PcMaterial { [PushConstant] cbuffer PerMaterial { float2 Scale; uint Count; }; uint MaterialCount() { return Count; } }");
        Write("PcRoot", """
            shader PcRoot : ComputeShaderBase
            {
                import PcMaterial;
                [PushConstant] cbuffer PerDraw { module uint Index; module uint Count; };
                RWBuffer<uint4> Out;
                override void Compute() { Out[0] = uint4(Index, Count, MaterialCount(), asuint(Scale.y)); }
            }
            """);

        var (ok, log, reflection, bytecode) = Mix("PcRoot", direct3D11);
        Assert.True(ok, log);
        var words = MemoryMarshal.Cast<byte, uint>(bytecode);
        if (!direct3D11)
        {
            Assert.Null(SpirvTools.Validate(words));
            return;
        }
        SpirvCrossSupport.SkipUnlessAvailable();
        var translator = new SpirvTranslator(SpirvTools.LegalizeForHlsl(words).AsMemory());
        var hlsl = translator.Translate(Backend.Hlsl, translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute));
        Assert.Contains("uint PushConstants_Count : packoffset(c0.z);", hlsl);
        Assert.Contains("uint PushConstants_Count_1 : packoffset(c1);", hlsl);
        Assert.Contains("uint4(PushConstants_Index, PushConstants_Count_1, PushConstants_Count,", hlsl);
    }

    // SDSL0131: more than 32 bytes in the joined block
    [Fact]
    public void PushConstantsLargerThanTheBudgetIsAnError()
    {
        Write("PcMaterial", "module PcMaterial { [PushConstant] cbuffer PerMaterial { float4 Color; float2 Scale; uint MaterialIndex; uint Padding; }; }");
        Write("PcRoot", Root);

        var (ok, log, _, _) = Mix("PcRoot", false);
        Assert.False(ok);
        Assert.Contains("SDSL0131: the push constants of this effect take 40 bytes, more than the 32-byte limit: PcMaterial.PerMaterial 32 bytes, PcRoot.PerDraw 8 bytes. Remove members, or move some of them to another cbuffer.", log);
    }

    // SDSL0132: one name with and without [PushConstant]
    [Fact]
    public void PushConstantAndNormalCBufferOfTheSameNameIsAnError()
    {
        Write("PcMaterial", "module PcMaterial { cbuffer PerDraw { float2 Scale; uint MaterialIndex; }; }");
        Write("PcRoot", Root);

        var (ok, log, _, _) = Mix("PcRoot", false);
        Assert.False(ok);
        Assert.Contains("SDSL0132: cbuffer PerDraw is [PushConstant] in PcRoot but not in PcMaterial. Add [PushConstant] in PcMaterial, or rename one of them.", log);
    }

    // SDSL0167: the push-constant block is named PushConstants, whatever the names of its cbuffers
    [Fact]
    public void ANormalCBufferNamedLikeThePushConstantBlockIsAnError()
    {
        Write("PcMaterial", "module PcMaterial { cbuffer PushConstants { float2 Scale; uint MaterialIndex; }; }");
        Write("PcRoot", Root);

        var (ok, log, _, _) = Mix("PcRoot", false);
        Assert.False(ok);
        Assert.Contains("SDSL0167: cbuffer PushConstants in PcMaterial: the name is taken by the push-constant block of the effect, which PcRoot declares with [PushConstant]. Rename the cbuffer.", log);
    }

    // SDSL0133: on Direct3D 11, the block takes b13, so 13 other cbuffers at most (ComputeShaderBase has one, PerDispatch)
    [Theory]
    [InlineData(12, false, null)]
    [InlineData(13, false, "SDSL0133: on Direct3D 11, the push constants of this effect use register b13, but the effect has 14 other cbuffers, so b13 is taken. Use fewer cbuffers, or remove [PushConstant].")]
    [InlineData(13, true, null)]
    public void Direct3D11KeepsARegisterForPushConstants(int cbufferCount, bool vulkan, string? expected)
    {
        var cbuffers = string.Concat(Enumerable.Range(0, cbufferCount).Select(i => $"cbuffer C{i} {{ uint V{i}; }}; "));
        var sum = string.Join(" + ", Enumerable.Range(0, cbufferCount).Select(i => $"V{i}"));
        Write("PcMaterial", "module PcMaterial { [PushConstant] cbuffer PerMaterial { float2 Scale; uint MaterialIndex; }; }");
        Write("PcMany", $$"""
            shader PcMany : ComputeShaderBase
            {
                import PcMaterial;
                {{cbuffers}}
                RWBuffer<uint> Out;
                override void Compute() { Out[0] = MaterialIndex + {{sum}}; }
            }
            """);

        var (ok, log, _, _) = Mix("PcMany", !vulkan);
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

    // SDSL0134: a vector at an offset that is not a multiple of its size
    [Fact]
    public void MisalignedVectorIsAnError()
    {
        Write("PcMaterial", "module PcMaterial { [PushConstant] cbuffer PerMaterial { uint MaterialIndex; float2 Scale; }; }");
        Write("PcRoot", Root);

        var (ok, log, _, _) = Mix("PcRoot", false);
        Assert.False(ok);
        Assert.Contains("SDSL0134: PcMaterial: member Scale (float2) of the [PushConstant] cbuffer PerMaterial is at byte 4 of the push-constant block, but a float2 must start at a multiple of 8 bytes, so that the C# struct given to SetPushConstants has the same layout on every graphics API. Move it to such an offset (add a member before it), or reorder the members (for example the vectors first).", log);
    }

    private (bool Ok, string Log, EffectReflection? Reflection, byte[] Bytecode) Mix(string root, bool direct3D11)
    {
        var log = new LoggerResult();
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"));
        bool ok;
        EffectReflection? reflection = null;
        byte[] bytecode = [];
        try
        {
            ok = mixer.MergeSDSL(new ShaderClassSource(root), new ShaderMixer.Options(ResourcesRegisterSeparate: direct3D11, StripGoogleUserType: !direct3D11), log, out var bytes, out reflection, out _, out _);
            bytecode = bytes.ToArray();
        }
        catch (InvalidOperationException e)
        {
            ok = false;
            log.Error(e.Message);
        }
        return (ok, Messages(log), reflection, bytecode);
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

    // File names without their folder, so the messages read the same on every machine
    private string Messages(LoggerResult log)
        => string.Join(Environment.NewLine, log.Messages.Select(m => $"{m.Type}: {m.Text.Replace(sourceDir.FullName + Path.DirectorySeparatorChar, "").Replace(sourceDir.FullName + "/", "")}"));

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), $"namespace Stride.Shaders.Tests;\n{code}\n");
}
