// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

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
