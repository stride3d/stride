// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Mistakes reported when a shader class compiles, before any effect mixes it.
/// </summary>
public class ShaderDefinitionCheckTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-definition");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Contract = """
        namespace Stride.Shaders.Tests;

        shader CheckContract
        {
            float4 Compute() { return 0; }
        }
        """;

    [Fact]
    public void UnknownMethodThroughACompositionNamesTheCompositionType()
    {
        Write("CheckContract", Contract);
        Write("CheckRoot", """
            namespace Stride.Shaders.Tests;

            shader CheckRoot
            {
                compose CheckContract contract;
                float4 Shading() { return contract.Missing(); }
            }
            """);

        var (ok, log) = Compile("CheckRoot");
        Assert.False(ok);
        Assert.Contains("SDSL0109: Could not resolve method Missing in type CheckContract", log);
    }

    // SDSL0200: without an inherited method, 'override' starts a new method that calls through a parent never reach
    [Theory]
    [InlineData("override float4 Compute() { return 1; }", null)]
    [InlineData("override float4 Other() { return 1; }", "Warning: CheckOverride.sdsl(5,21): SDSL0200: CheckOverride.Other() is marked 'override', but no inherited class declares Other")]
    public void OverrideNeedsAnInheritedMethod(string method, string? expected)
    {
        Write("CheckContract", Contract);
        Write("CheckOverride", $$"""
            namespace Stride.Shaders.Tests;

            shader CheckOverride : CheckContract
            {
                {{method}}
            }
            """);

        var (ok, log) = Compile("CheckOverride");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0200", log);
    }

    // SDSL0176 (from CheckOverriddenMethod): an inherited method of the same name but another signature
    [Fact]
    public void OverrideWithAnotherSignatureIsAnError()
    {
        Write("CheckContract", Contract);
        Write("CheckOverride", """
            namespace Stride.Shaders.Tests;

            shader CheckOverride : CheckContract
            {
                override float4 Compute(float x) { return x; }
            }
            """);

        var (ok, log) = Compile("CheckOverride");
        Assert.False(ok);
        Assert.Contains("SDSL0176: CheckOverride.Compute(float) is marked 'override', but no inherited method has this name and these parameters. Inherited: CheckContract.Compute().", log);
    }

    // SDSL0201: base.X() looks for the previous method of the same chain, so it needs an inherited X
    [Theory]
    [InlineData("override float4 Compute() { return base.Compute() + 1; }", null)]
    [InlineData("float4 Other() { return base.Other(); }", "Warning: CheckBase.sdsl(5,34): SDSL0201: base.Other() in CheckBase: no inherited class declares Other()")]
    public void BaseCallNeedsAnInheritedMethod(string method, string? expected)
    {
        Write("CheckContract", Contract);
        Write("CheckBase", $$"""
            namespace Stride.Shaders.Tests;

            shader CheckBase : CheckContract
            {
                {{method}}
            }
            """);

        var (ok, log) = Compile("CheckBase");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0201", log);
    }

    private const string Generic = """
        namespace Stride.Shaders.Tests;

        shader CheckGeneric<int N, int M> : CheckContract
        {
            override float4 Compute() { return N + M; }
        }
        """;

    // SDSL0202: values past the generic parameters of a parent are ignored
    [Theory]
    [InlineData("CheckGeneric<1, 2>", null)]
    [InlineData("CheckGeneric<1, 2, 3>", "Warning: CheckChild.sdsl(3,21): SDSL0202: CheckGeneric has 2 generic parameter(s) (N, M), but 3 given")]
    public void ParentGetsOneValuePerGenericParameter(string parent, string? expected)
    {
        Write("CheckContract", Contract);
        Write("CheckGeneric", Generic);
        Write("CheckChild", $$"""
            namespace Stride.Shaders.Tests;

            shader CheckChild : {{parent}}
            {
            }
            """);

        var (ok, log) = Compile("CheckChild");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0202", log);
    }

    // SDSL0117: too few values for the generic parameters of a parent
    [Fact]
    public void ParentWithTooFewGenericValuesIsAnError()
    {
        Write("CheckContract", Contract);
        Write("CheckGeneric", Generic);
        Write("CheckChild", """
            namespace Stride.Shaders.Tests;

            shader CheckChild : CheckGeneric<1>
            {
            }
            """);

        var (ok, log) = Compile("CheckChild");
        Assert.False(ok);
        Assert.Contains("SDSL0117: CheckGeneric has 2 generic parameter(s) (N, M), but 1 given.", log);
    }

    // SDSL0117 from a mixin tree, e.g. an effect log written for an older version of the shader
    [Fact]
    public void EffectWithTooFewGenericValuesIsAnError()
    {
        Write("CheckContract", Contract);
        Write("CheckGeneric", Generic);
        Write("CheckMixRoot", """
            namespace Stride.Shaders.Tests;

            shader CheckMixRoot : ComputeShaderBase
            {
                compose CheckContract contract;
                RWBuffer<float4> Out;
                override void Compute() { Out[0] = contract.Compute(); }
            }
            """);

        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("CheckMixRoot") },
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("CheckGeneric", "1") } } },
        };
        var log = new LoggerResult();
        var ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL")).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);
        Assert.False(ok);
        Assert.Contains("SDSL0117: CheckGeneric has 2 generic parameter(s) (N, M), but 1 given (1).", string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // SDSL0203: the effect reflection can only make a float3 or float4 a color
    [Theory]
    [InlineData("cbuffer PerMaterial { [Color] float3 Tint; }", null)]
    [InlineData("cbuffer PerMaterial { [Color] float3 Tints[4]; }", null)]
    [InlineData("cbuffer PerMaterial { [Color] float Tint; }", "Warning: CheckColor.sdsl(5,27): SDSL0203: [Color] on Tint, of type float")]
    [InlineData("[Color] float2 Tint;", "Warning: CheckColor.sdsl(5,5): SDSL0203: [Color] on Tint, of type float2")]
    public void ColorIsAFloat3OrFloat4(string member, string? expected)
    {
        Write("CheckColor", $$"""
            namespace Stride.Shaders.Tests;

            shader CheckColor
            {
                {{member}}
            }
            """);

        var (ok, log) = Compile("CheckColor");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0203", log);
    }

    // The warnings are kept with the compiled class: an effect compiled later from the cache reports them too
    [Fact]
    public void WarningsAreReportedForEachEffectFromTheCache()
    {
        Write("CheckContract", Contract);
        Write("CheckOverride", """
            namespace Stride.Shaders.Tests;

            shader CheckOverride : CheckContract
            {
                override float4 Other() { return 1; }
            }
            """);

        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");
        foreach (var expectedFromCache in new[] { false, true })
        {
            var log = new LoggerResult();
            loader.Log = log;
            Assert.True(loader.LoadExternalBuffer("CheckOverride", [], out _, out _, out var isFromCache));
            Assert.Equal(expectedFromCache, isFromCache);
            // Once per effect (logger), however many times the class is loaded
            Assert.True(loader.LoadExternalBuffer("CheckOverride", [], out _, out _, out _));
            Assert.Single(log.Messages, m => m.Type == LogMessageType.Warning && m.Text.Contains("SDSL0200"));
        }
    }

    // With WarningsAsErrors (STRIDE_SHADER_WARNINGS_AS_ERRORS), a class with warnings fails to load
    [Fact]
    public void WarningsAsErrorsFailsTheClass()
    {
        Write("CheckContract", Contract);
        Write("CheckOverride", """
            namespace Stride.Shaders.Tests;

            shader CheckOverride : CheckContract
            {
                override float4 Other() { return 1; }
            }
            """);

        var (ok, log) = Compile("CheckOverride", warningsAsErrors: true);
        Assert.False(ok);
        Assert.Contains("Error: CheckOverride.sdsl(5,21): SDSL0200", log);
    }

    // Imported and inherited modules count as inherited classes, so overrides and base calls in the module model don't warn;
    // a module unit is checked like a shader
    [Theory]
    [InlineData("CheckImporter", null)]
    [InlineData("CheckInheritsModule", null)]
    [InlineData("CheckModuleOverride", null)]
    [InlineData("CheckCompanionOverride", null)]
    [InlineData("CheckBadModule", "SDSL0200: CheckBadModule.Missing() is marked 'override'")]
    public void ModulesCountAsInheritedClasses(string name, string? expected)
    {
        Write("CheckModule", """
            namespace Stride.Shaders.Tests;

            module CheckModule
            {
                float4 Helper() { return 1; }
            }
            """);
        Write("CheckImporter", """
            namespace Stride.Shaders.Tests;

            shader CheckImporter
            {
                import CheckModule;
                override float4 Helper() { return base.Helper() + 1; }
            }
            """);
        Write("CheckInheritsModule", """
            namespace Stride.Shaders.Tests;

            shader CheckInheritsModule : CheckModule
            {
                override float4 Helper() { return base.Helper() * 2; }
            }
            """);
        Write("CheckModuleOverride", """
            namespace Stride.Shaders.Tests;

            module CheckModuleOverride
            {
                import CheckModule;
                override float4 Helper() { return base.Helper() * 3; }
            }
            """);
        Write("CheckCompanion", """
            namespace Stride.Shaders.Tests;

            shader CheckCompanion
            {
                module float4 Knob() { return 0; }
            }
            """);
        Write("CheckCompanionOverride", """
            namespace Stride.Shaders.Tests;

            shader CheckCompanionOverride : CheckCompanion
            {
                override module float4 Knob() { return base.Knob() + 1; }
            }
            """);
        Write("CheckBadModule", """
            namespace Stride.Shaders.Tests;

            module CheckBadModule
            {
                import CheckModule;
                override float4 Missing() { return base.Helper(); }
            }
            """);

        var (ok, log) = Compile(name);
        Assert.True(ok, log);
        if (expected == null)
            Assert.DoesNotContain("SDSL02", log);
        else
            Assert.Contains(expected, log);
    }

    private static void AssertWarning(string? expected, string code, string log)
    {
        if (expected == null)
            Assert.DoesNotContain(code, log);
        else
            Assert.Contains(expected, log);
    }

    // Compiles one class alone (and the classes it uses), like the first effect that needs it would
    private (bool Ok, string Log) Compile(string name, bool warningsAsErrors = false)
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log, WarningsAsErrors = warningsAsErrors };
        bool ok;
        try
        {
            ok = loader.LoadExternalBuffer(name, [], out _, out _, out _);
        }
        catch (InvalidOperationException)
        {
            ok = false;
        }
        // File names without their folder, so the messages read the same on every machine
        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => $"{m.Type}: {m.Text.Replace(sourceDir.FullName + Path.DirectorySeparatorChar, "").Replace(sourceDir.FullName + "/", "")}")));
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
}
