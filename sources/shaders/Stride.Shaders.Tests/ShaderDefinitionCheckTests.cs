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
                override module float4 Helper() { return base.Helper() + 1; }
            }
            """);
        Write("CheckInheritsModule", """
            namespace Stride.Shaders.Tests;

            shader CheckInheritsModule : CheckModule
            {
                override module float4 Helper() { return base.Helper() * 2; }
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

    // Classes for the module rules: a [Root] module and shader, a module importing the [Root] module, a shader with instance members
    private void WriteRootClasses()
    {
        Write("CheckContract", Contract);
        Write("CheckModule", """
            namespace Stride.Shaders.Tests;

            module CheckModule
            {
                float4 Helper() { return 1; }
            }
            """);
        Write("CheckRootModule", """
            namespace Stride.Shaders.Tests;

            [Root]
            module CheckRootModule
            {
                float4 Entry() { return 0; }
            }
            """);
        Write("CheckBridge", """
            namespace Stride.Shaders.Tests;

            module CheckBridge
            {
                import CheckRootModule;
            }
            """);
        Write("CheckRootShader", """
            namespace Stride.Shaders.Tests;

            [Root]
            shader CheckRootShader
            {
            }
            """);
        Write("CheckImportsRoot", """
            namespace Stride.Shaders.Tests;

            shader CheckImportsRoot
            {
                import CheckRootModule;
            }
            """);
        Write("CheckParent", """
            namespace Stride.Shaders.Tests;

            shader CheckParent
            {
                float Radius;
                module float4 Knob() { return 0; }
                float4 Value() { return Radius; }
            }
            """);
    }

    // Root-only: [Root], a shader parent that is root-only, or a direct import of a [Root] module; a module passes nothing on
    [Theory]
    [InlineData("[Root] module CheckRootOnly { }", true)]
    [InlineData("module CheckRootOnly { import CheckRootModule; }", false)]
    [InlineData("shader CheckRootOnly { import CheckRootModule; }", true)]
    [InlineData("shader CheckRootOnly : CheckRootModule { }", true)]
    [InlineData("shader CheckRootOnly { import CheckBridge; }", false)]
    [InlineData("shader CheckRootOnly : CheckRootShader { }", true)]
    [InlineData("shader CheckRootOnly : CheckImportsRoot { }", true)]
    [InlineData("shader CheckRootOnly : CheckParent { }", false)]
    public void RootOnlyFollowsShaderParentsAndDirectImports(string declaration, bool expected)
    {
        WriteRootClasses();
        Write("CheckRootOnly", $"""
            namespace Stride.Shaders.Tests;

            {declaration}
            """);

        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };
        Assert.True(loader.LoadExternalBuffer("CheckRootOnly", [], out var buffer, out _, out _), string.Join(Environment.NewLine, log.Messages));
        Assert.Equal(expected, Spirv.Building.SpirvBuilder.GetShaderFlags(buffer).HasFlag(Spirv.Specification.ShaderFlagsMask.Root));
    }

    // SDSL0119: a root-only shader in a composition
    [Theory]
    [InlineData("[Root] shader CheckComposed : CheckContract { }", "SDSL0119: CheckComposed can only be mixed at the root of an effect ([Root] on it, or on a class it inherits or imports), but it is in the composition contract.")]
    [InlineData("shader CheckComposed : CheckContract { import CheckRootModule; }", "SDSL0119: CheckComposed can only be mixed at the root")]
    [InlineData("shader CheckComposed : CheckContract { import CheckBridge; }", null)]
    public void RootOnlyShaderInACompositionIsAnError(string declaration, string? expected)
    {
        WriteRootClasses();
        Write("CheckComposed", $"""
            namespace Stride.Shaders.Tests;

            {declaration}
            """);
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
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("CheckComposed") } } },
        };
        var log = new LoggerResult();
        bool ok;
        try
        {
            ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL")).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);
        }
        catch (InvalidOperationException e)
        {
            ok = false;
            log.Error(e.Message);
        }
        var text = string.Join(Environment.NewLine, log.Messages.Select(m => m.Text));
        if (expected == null)
        {
            Assert.True(ok, text);
        }
        else
        {
            Assert.False(ok);
            Assert.Contains(expected, text);
        }
    }

    // SDSL0247: a module goes to the root, so a call through the slot finds a method by name there
    [Fact]
    public void AModuleInACompositionIsAnError()
    {
        WriteRootClasses();
        Write("CheckSlotModule", """
            namespace Stride.Shaders.Tests;

            module CheckSlotModule { float4 Compute() { return 1; } }
            """);
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
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("CheckSlotModule") } } },
        };

        var log = new LoggerResult();
        var ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"))
            .MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);

        Assert.False(ok);
        Assert.Contains(log.Messages, x => x.Text.Contains("SDSL0247: CheckSlotModule is a module, but it is in the composition contract.", StringComparison.Ordinal));
    }

    // SDSL0119 names the composition from the root's slot down, as an effect writes it
    [Fact]
    public void RootOnlyShaderInANestedCompositionNamesThePathFromTheRoot()
    {
        WriteRootClasses();
        Write("CheckComposed", """
            namespace Stride.Shaders.Tests;

            [Root] shader CheckComposed : CheckContract { }
            """);
        Write("CheckOuter", """
            namespace Stride.Shaders.Tests;

            shader CheckOuter : CheckContract
            {
                compose CheckContract inner[];
                override float4 Compute() { return inner[0].Compute(); }
            }
            """);
        Write("CheckMixRoot", """
            namespace Stride.Shaders.Tests;

            shader CheckMixRoot : ComputeShaderBase
            {
                compose CheckContract contract;
                RWBuffer<float4> Out;
                override void Compute() { Out[0] = contract.Compute(); }
            }
            """);

        var inner = new ShaderArraySource();
        inner.Add(new ShaderMixinSource { Mixins = { new ShaderClassSource("CheckComposed") } });
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("CheckMixRoot") },
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("CheckOuter") }, Compositions = { ["inner"] = inner } } },
        };
        var log = new LoggerResult();
        bool ok;
        try
        {
            ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL")).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);
        }
        catch (InvalidOperationException e)
        {
            ok = false;
            log.Error(e.Message);
        }
        Assert.False(ok);
        Assert.Contains("but it is in the composition contract.inner[0].", string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // SDSL0204: module code runs once per effect, so in a shader used in compositions it can't read one instance's members
    [Theory]
    [InlineData("shader CheckRead", "float Radius; module float4 Shading() { return Radius; }", "Warning: CheckRead.sdsl(6,52): SDSL0204: CheckRead.Shading() is module code and reads Radius, an instance member of CheckRead.")]
    [InlineData("shader CheckRead", "cbuffer PerMaterial { float Radius; } module float4 Shading() { return Radius; }", "SDSL0204: CheckRead.Shading() is module code and reads Radius, an instance member of CheckRead.")]
    [InlineData("shader CheckRead", "compose CheckContract color; module float4 Shading() { return color.Compute(); }", "SDSL0204: CheckRead.Shading() is module code and reads color, an instance member of CheckRead.")]
    [InlineData("shader CheckRead : CheckParent", "module float4 Shading() { return Radius; }", "SDSL0204: CheckRead.Shading() is module code and reads Radius, an instance member of CheckParent.")]
    [InlineData("shader CheckRead : CheckParent", "module float4 Shading() { return CheckParent.Radius; }", "SDSL0204: CheckRead.Shading() is module code and reads Radius, an instance member of CheckParent.")]
    [InlineData("shader CheckRead : CheckParent", "float4 Other() { return CheckParent.Radius; }", null)]
    [InlineData("shader CheckRead", "import CheckBridge; float Radius; module float4 Shading() { return Radius; }", "SDSL0204")]
    [InlineData("shader CheckRead", "module float Radius; module float4 Shading() { return Radius; }", null)]
    [InlineData("shader CheckRead", "float Radius; float4 Shading() { return Radius; }", null)]
    [InlineData("shader CheckRead", "float Radius; module float4 Shading() { float Radius = 1; return Radius; }", null)]
    [InlineData("shader CheckRead", "float Radius; module float4 Shading(float Radius) { return Radius; }", null)]
    [InlineData("shader CheckRead", "static const float Radius = 1; module float4 Shading() { return Radius; }", null)]
    [InlineData("[Root] shader CheckRead", "float Radius; module float4 Shading() { return Radius; }", null)]
    [InlineData("shader CheckRead : CheckRootShader", "float Radius; module float4 Shading() { return Radius; }", null)]
    [InlineData("shader CheckRead", "import CheckRootModule; float Radius; module float4 Shading() { return Radius; }", null)]
    public void ModuleCodeReadsNoInstanceMember(string declaration, string members, string? expected)
    {
        WriteRootClasses();
        Write("CheckRead", $$"""
            namespace Stride.Shaders.Tests;

            {{declaration}}
            {
                float4 Unused() { return 0; }
                {{members}}
            }
            """);

        var (ok, log) = Compile("CheckRead");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0204", log);
    }

    // SDSL0205: module code can't call an instance method of its class or a parent
    [Theory]
    [InlineData("shader CheckCall", "float4 Helper() { return 1; } module float4 Shading() { return Helper(); }", "Warning: CheckCall.sdsl(5,68): SDSL0205: CheckCall.Shading() is module code and calls CheckCall.Helper(), an instance method.")]
    [InlineData("shader CheckCall", "float4 Helper() { return 1; } module float4 Shading() { return this.Helper(); }", "SDSL0205: CheckCall.Shading() is module code and calls CheckCall.Helper()")]
    [InlineData("shader CheckCall : CheckParent", "module float4 Shading() { return Value(); }", "SDSL0205: CheckCall.Shading() is module code and calls CheckParent.Value()")]
    [InlineData("shader CheckCall", "module float4 Helper() { return 1; } module float4 Shading() { return Helper(); }", null)]
    [InlineData("shader CheckCall", "float4 Helper() { return 1; } float4 Shading() { return Helper(); }", null)]
    [InlineData("shader CheckCall : CheckParent", "module float4 Shading() { return Knob(); }", null)]
    [InlineData("[Root] shader CheckCall", "float4 Helper() { return 1; } module float4 Shading() { return Helper(); }", null)]
    public void ModuleCodeCallsNoInstanceMethod(string declaration, string members, string? expected)
    {
        WriteRootClasses();
        Write("CheckCall", $$"""
            namespace Stride.Shaders.Tests;

            {{declaration}}
            {
                {{members}}
            }
            """);

        var (ok, log) = Compile("CheckCall");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0205", log);
    }

    // SDSL0206 / SDSL0207: an override has the kind of the method it overrides (in a composition, they are in different nodes)
    [Theory]
    [InlineData("shader CheckKind : CheckParent", "override float4 Knob() { return 1; }", "Warning: CheckKind.sdsl(5,21): SDSL0206: CheckKind.Knob() overrides the module method CheckParent.Knob but is not 'module'.")]
    [InlineData("shader CheckKind", "import CheckModule; override float4 Helper() { return 1; }", "SDSL0206: CheckKind.Helper() overrides the module method CheckModule.Helper but is not 'module'.")]
    [InlineData("shader CheckKind : CheckParent", "override module float4 Value() { return 1; }", "SDSL0207: CheckKind.Value() is 'module' but overrides the instance method CheckParent.Value.")]
    [InlineData("shader CheckKind : CheckParent", "override module float4 Knob() { return 1; }", null)]
    [InlineData("shader CheckKind : CheckParent", "override float4 Value() { return 1; }", null)]
    [InlineData("[Root] shader CheckKind : CheckParent", "override float4 Knob() { return 1; } override module float4 Value() { return 1; }", null)]
    public void OverrideHasTheKindOfItsParent(string declaration, string member, string? expected)
    {
        WriteRootClasses();
        Write("CheckKind", $$"""
            namespace Stride.Shaders.Tests;

            {{declaration}}
            {
                {{member}}
            }
            """);

        var (ok, log) = Compile("CheckKind");
        Assert.True(ok, log);
        if (expected == null)
        {
            Assert.DoesNotContain("SDSL0206", log);
            Assert.DoesNotContain("SDSL0207", log);
        }
        else
        {
            Assert.Contains(expected, log);
        }
    }

    // SDSL0208: instance code can't reach a module method through base (the module method is in another node)
    [Theory]
    [InlineData("shader CheckBaseKind : CheckParent", "float4 Other() { return base.Knob(); }", "SDSL0208: base.Knob() in CheckBaseKind.Other(): Knob is a module method, called from instance code.")]
    [InlineData("shader CheckBaseKind : CheckParent", "module float4 Other() { return base.Knob(); }", null)]
    [InlineData("[Root] shader CheckBaseKind : CheckParent", "float4 Other() { return base.Knob(); }", null)]
    public void InstanceCodeCallsNoBaseModuleMethod(string declaration, string member, string? expected)
    {
        WriteRootClasses();
        Write("CheckBaseKind", $$"""
            namespace Stride.Shaders.Tests;

            {{declaration}}
            {
                {{member}}
            }
            """);

        var (ok, log) = Compile("CheckBaseKind");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0208", log);
    }

    // SDSL0209 / SDSL0210 / SDSL0213: a [Pure] module has functions and constants only, imports pure modules, and runs in any stage
    [Theory]
    [InlineData("static const float Scale = 2; float F(float x) { return abs(x) * Scale; }", null, null)]
    [InlineData("float Scale;", "SDSL0209", "Warning: CheckPure.sdsl(6,5): SDSL0209: [Pure] module CheckPure declares the variable Scale: a pure module has functions and constants only.")]
    [InlineData("cbuffer PerFrame { float Time; }", "SDSL0209", "SDSL0209: [Pure] module CheckPure declares PerFrame")]
    [InlineData("Texture2D Tex;", "SDSL0209", "SDSL0209: [Pure] module CheckPure declares the variable Tex")]
    [InlineData("stream float4 Color;", "SDSL0209", "SDSL0209: [Pure] module CheckPure declares the stream Color")]
    [InlineData("import CheckStreams; float4 F() { return streams.Color; }", "SDSL0209", "SDSL0209: [Pure] module CheckPure reads streams in F()")]
    [InlineData("import CheckModule;", "SDSL0210", "SDSL0210: [Pure] module CheckPure uses CheckModule, which is not a pure module.")]
    [InlineData("float4 F() { return CheckModule.Helper(); }", "SDSL0210", "SDSL0210: [Pure] module CheckPure uses CheckModule")]
    [InlineData("float F() { return Global.Time; }", "SDSL0210", "SDSL0210: [Pure] module CheckPure uses Global")]
    [InlineData("float F(float x) { return CheckPureHelper.Twice(x); }", "SDSL0210", null)]
    [InlineData("import CheckPureHelper; float F(float x) { return Twice(x); }", "SDSL0210", null)]
    [InlineData("float F(float x) { return ddx(x); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: ddx() in F() works only in a pixel shader")]
    [InlineData("float F(float x) { return fwidth(x); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: fwidth() in F()")]
    [InlineData("void F(float x) { clip(x); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: clip() in F()")]
    [InlineData("void F(float x) { if (x < 0) discard; }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: discard in F()")]
    [InlineData("float4 F(Texture2D t, SamplerState s, float2 uv) { return t.Sample(s, uv); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: Sample() in F()")]
    [InlineData("float4 F(Texture2D t, SamplerState s, float2 uv) { return t.SampleBias(s, uv, 1.0); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: SampleBias() in F()")]
    [InlineData("float F(Texture2D t, SamplerComparisonState s, float2 uv) { return t.SampleCmp(s, uv, 0.5); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: SampleCmp() in F()")]
    [InlineData("float F(Texture2D t, SamplerState s, float2 uv) { return t.CalculateLevelOfDetail(s, uv); }", "SDSL0213", "SDSL0213: [Pure] module CheckPure: CalculateLevelOfDetail() in F()")]
    [InlineData("float4 F(Texture2D t, SamplerState s, float2 uv) { return t.SampleLevel(s, uv, 0.0); }", null, null)]
    public void PureModuleHasNoState(string members, string? code, string? expected)
    {
        WriteRootClasses();
        Write("CheckStreams", """
            namespace Stride.Shaders.Tests;

            module CheckStreams
            {
                stream float4 Color;
            }
            """);
        Write("CheckPureHelper", """
            namespace Stride.Shaders.Tests;

            [Pure]
            module CheckPureHelper
            {
                float Twice(float x) { return x * 2; }
            }
            """);
        Write("CheckPure", $$"""
            namespace Stride.Shaders.Tests;

            [Pure]
            module CheckPure
            {
                {{members}}
            }
            """);

        var (ok, log) = Compile("CheckPure");
        Assert.True(ok, log);
        if (code == null)
            Assert.DoesNotContain("SDSL02", log);
        else
            AssertWarning(expected, code, log);
    }

    // Without [Pure], a module may read state and use pixel shader intrinsics
    [Fact]
    public void ModuleWithoutPureIsNotChecked()
    {
        Write("CheckState", """
            namespace Stride.Shaders.Tests;

            module CheckState
            {
                float Scale;
                float F(float x) { return ddx(x) * Scale; }
            }
            """);

        var (ok, log) = Compile("CheckState");
        Assert.True(ok, log);
        Assert.DoesNotContain("SDSL02", log);
    }

    // SDSL0211: only a module can be pure
    [Fact]
    public void PureShaderIsAWarning()
    {
        Write("CheckPureShader", """
            namespace Stride.Shaders.Tests;

            [Pure]
            shader CheckPureShader
            {
            }
            """);

        var (ok, log) = Compile("CheckPureShader");
        Assert.True(ok, log);
        Assert.Contains("SDSL0211: [Pure] on shader CheckPureShader: only a module can be pure.", log);
    }

    // SDSL0212: 'clone' is parsed and ignored since Stride 4.4
    [Theory]
    [InlineData("clone float4 F() { return 0; }", "Warning: CheckClone.sdsl(5,18): SDSL0212: CheckClone.F(): 'clone' has no effect since Stride 4.4. Remove it.")]
    [InlineData("float4 F() { return 0; }", null)]
    public void CloneIsAWarning(string member, string? expected)
    {
        Write("CheckClone", $$"""
            namespace Stride.Shaders.Tests;

            shader CheckClone
            {
                {{member}}
            }
            """);

        var (ok, log) = Compile("CheckClone");
        Assert.True(ok, log);
        AssertWarning(expected, "SDSL0212", log);
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
