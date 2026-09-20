using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

public class StrideShaderTests
{
    // Regression: multidimensional array dimensions must be emitted in declaration order.
    // GenerateArrayType used to wrap the dimensions such that the last bracket became outermost,
    // so `float4 Data3D[2][3][5]` was emitted transposed as [5][3][2] (breaking indexing / fxc).
    [Fact]
    public void MultidimensionalArrayDimensionsKeepDeclarationOrder()
    {
        SpirvCrossSupport.SkipUnlessAvailable();

        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("ArrayDims3D", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("ArrayDims3D"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoint = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute);
        var hlsl = translator.Translate(Backend.Hlsl, entryPoint);

        Assert.Equal("[2][3][5]", MatchArrayDimensions(hlsl, "Data3D"));
        Assert.Equal("[7]", MatchArrayDimensions(hlsl, "Data1D")); // rank-1 control: unaffected
    }

    private static string MatchArrayDimensions(string hlsl, string arrayName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(hlsl, $@"\b{arrayName}((?:\[\d+\])+)\s*;");
        Assert.True(match.Success, $"'{arrayName}' declaration not found in emitted HLSL:{Environment.NewLine}{hlsl}");
        return match.Groups[1].Value;
    }

    // Regression: a shader field whose name matches an intrinsic method name (e.g. `float SampleLevel;`
    // alongside `Tex.SampleLevel(...)`) must not confuse method-call resolution with the field symbol.
    [Fact]
    public void FieldNameMatchingIntrinsicMethodNameDoesNotCrashCompiler()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("FieldNameShadowsIntrinsicMethod", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("FieldNameShadowsIntrinsicMethod"), new ShaderMixer.Options(true), log, out _, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // Regression: a Geometry shader reading a stage-input stream field directly through the input
    // array element (`input[i].Field`), not just via a whole-struct `streams = input[i]` assignment,
    // used to crash StreamAccessPatcher with a NullReferenceException. ReadWriteAnalyzer only
    // recognized `PointerType { BaseType: StreamsType }` as a stream access on OpVariable/
    // OpFunctionParameter, but a GS per-vertex input parameter (`Input input[3]`) is
    // `PointerType { BaseType: ArrayType { BaseType: StreamsType } }`, so `input[i].Field` reads were
    // never marked as "Read" and the field never got an InputStructFieldIndex.
    [Fact]
    public void GeometryShaderInputArrayFieldAccessDoesNotCrashCompiler()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("GSInputArrayFieldAccess", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("GSInputArrayFieldAccess"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        File.WriteAllBytes("GSInputArrayFieldAccess.spv", bytecode);
        var validationResult = Spv.ValidateFile("GSInputArrayFieldAccess.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);
    }

    // Reflection reports a multidimensional cbuffer array as a flat element count
    // (float4 Data2D[2][3] => 6 elements), matching fxc and the runtime parameter layout.
    [Fact]
    public void MultidimensionalArrayReflectionIsFlattened()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("ArrayDimsCBuffer", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("ArrayDimsCBuffer"), new ShaderMixer.Options(true), log, out _, out var reflection, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var members = reflection.ConstantBuffers.SelectMany(cb => cb.Members).ToDictionary(m => m.RawName);

        var data2D = members["Data2D"];
        Assert.Equal(6, data2D.Type.Elements);
        Assert.Equal(96, data2D.Size); // 6 float4 elements at stride 16

        var data1D = members["Data1D"]; // rank-1 control: unaffected
        Assert.Equal(5, data1D.Type.Elements);
        Assert.Equal(80, data1D.Size);
    }

    [Fact]
    public void NumThreadsOnNonEntryMethodIsIgnoredWithWarning()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("CSNumThreadsOnNonEntry"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        Assert.Contains(log.Messages, m => m.Type == Stride.Core.Diagnostics.LogMessageType.Warning
            && m.Text.Contains("[numthreads]") && m.Text.Contains("Compute"));
    }

    // A `stage compose` is one slot for the whole effect: its declaring shader is promoted to the root,
    // so a value supplied at a nested composition would have nothing to attach to.
    [Fact]
    public void StageCompositionSuppliedFromNestedIsReported()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var shaderSource = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("StageComposePathRoot") },
            Compositions =
            {
                ["nested"] = new ShaderMixinSource
                {
                    Mixins = { new ShaderClassSource("StageComposePathSupplier") },
                    Compositions = { ["Samplers"] = new ShaderArraySource { new ShaderClassSource("StageComposePathImpl") } },
                },
            },
        };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.False(shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out _, out _, out _, out _));

        Assert.Contains(log.Messages, m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error
            && m.Text.Contains("'Samplers'") && m.Text.Contains("StageComposePathDeclarer")
            && m.Text.Contains("supplied at the root") && m.Text.Contains("'nested'"));
    }

    // The other side of the rule: the value comes from the root even when only a nested composition
    // inherits the shader declaring the slot.
    [Fact]
    public void StageCompositionDeclaredFromNestedIsSuppliedAtRoot()
    {
        AssertStageSamplerTextureIsBoundAtRoot(MergeStageComposePath("StageComposePathRoot", "nested"));
    }

    // Two nested compositions inheriting the same declarer share the one slot.
    [Fact]
    public void StageCompositionDeclaredFromTwoNestedIsSuppliedAtRoot()
    {
        AssertStageSamplerTextureIsBoundAtRoot(MergeStageComposePath("StageComposePathRoot2", "nestedA", "nestedB"));
    }

    private static EffectReflection MergeStageComposePath(string root, params string[] nestedSlots)
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var shaderSource = new ShaderMixinSource { Mixins = { new ShaderClassSource(root) } };
        foreach (var nestedSlot in nestedSlots)
            shaderSource.Compositions[nestedSlot] = new ShaderMixinSource { Mixins = { new ShaderClassSource("StageComposePathSupplier") } };
        shaderSource.Compositions["Samplers"] = new ShaderArraySource { new ShaderClassSource("StageComposePathImpl") };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out _, out var reflection, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        return reflection;
    }

    // The supplied shader's texture gets the key of a root composition: no nested path after the slot
    private static void AssertStageSamplerTextureIsBoundAtRoot(EffectReflection reflection)
    {
        var textureKeys = reflection.ResourceBindings.Select(b => b.KeyInfo.KeyName).Where(k => k.Contains("StageComposePathImpl.Tex")).Distinct().ToList();
        Assert.Equal(["StageComposePathImpl.Tex.Samplers[0]"], textureKeys);
    }

    // SV_Coverage is a uint in HLSL and a one-element array decorated SampleMask in SPIR-V, on both sides.
    [Fact]
    public void CoverageIsSampleMaskInFragmentStage()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("PSCoverage"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var fragment = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.Fragment);
        var hlsl = translator.Translate(Backend.Hlsl, fragment);
        // Read as an input and, since the shader writes it, declared as an output too
        Assert.Contains("gl_SampleMaskIn : SV_Coverage", hlsl);
        Assert.Contains("gl_SampleMask : SV_Coverage", hlsl);
        var errors = FxcSupport.Compile(hlsl, "ps_5_0");
        Assert.True(errors is null, errors + Environment.NewLine + hlsl);
    }

    // fxc rejects the same shader with X4532, so this reports rather than emitting a module that only
    // fails later, deep inside the HLSL legalizer.
    [Fact]
    public void TextureSampleOutsideFragmentStageIsReported()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.False(shaderMixer.MergeSDSL(new ShaderClassSource("VSTextureSample"), new ShaderMixer.Options(true), log, out _, out _, out _, out _));

        Assert.Contains(log.Messages, m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error
            && m.Text.Contains("VSTextureSample.sdsl(14,") && m.Text.Contains("implicit level of detail")
            && m.Text.Contains("SampleLevel") && m.Text.Contains("Vertex"));
    }

    // ddx/ddy/fwidth need the same derivatives as an implicit-LOD sample, so they carry the same stage
    // restriction and must be reported rather than left to the legalizer.
    [Fact]
    public void DerivativeOutsideFragmentStageIsReported()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.False(shaderMixer.MergeSDSL(new ShaderClassSource("VSDerivative"), new ShaderMixer.Options(true), log, out _, out _, out _, out _));

        Assert.Contains(log.Messages, m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error
            && m.Text.Contains("VSDerivative.sdsl(11,") && m.Text.Contains("screen-space derivative")
            && m.Text.Contains("Vertex"));
    }

    // A hull patch constant function is linked by a PatchConstantFuncSDSL decoration rather than by a
    // call in the source. Validation runs after InterfaceProcessor generates the hull wrapper, which
    // calls it for real, so the caller/callee walk reaches it.
    [Fact]
    public void ImplicitLodInHullPatchConstantIsReported()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.False(shaderMixer.MergeSDSL(new ShaderClassSource("HSPatchConstantTextureSample"), new ShaderMixer.Options(true), log, out _, out _, out _, out _));

        Assert.Contains(log.Messages, m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error
            && m.Text.Contains("HSPatchConstantTextureSample.sdsl(22,") && m.Text.Contains("implicit level of detail")
            && m.Text.Contains("Hull"));
    }

    [Fact]
    public void TextureSampleInFragmentStageCompiles()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("PSTextureSample"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);
    }

    [Theory]
    [InlineData("CSNumThreadsOverride", 16u)]
    [InlineData("CSNumThreadsInherit", 8u)]
    public void OverriddenComputeEntryPointKeepsOneLocalSize(string shaderName, uint expectedX)
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        var localSizes = ReadExecutionModes(bytecode).Where(m => m.Mode == Stride.Shaders.Spirv.Specification.ExecutionMode.LocalSize).ToList();
        var localSize = Assert.Single(localSizes);
        Assert.Equal(expectedX, localSize.Parameters[0]);
        Assert.Contains(localSize.EntryPoint, ReadEntryPointIds(bytecode));
    }

    [Fact]
    public void OverriddenHullEntryPointInheritsBaseAttributes()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("HSOverride"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out var entryPoints),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        var hull = Assert.Single(entryPoints, e => e.Stage == ShaderStage.Hull);
        var outputVertices = Assert.Single(ReadExecutionModes(bytecode), m => m.Mode == Stride.Shaders.Spirv.Specification.ExecutionMode.OutputVertices);
        Assert.Equal(hull.Id, outputVertices.EntryPoint);
        Assert.Equal(3u, outputVertices.Parameters[0]);
    }

    [Theory]
    [InlineData("ComposeNumThreadsRoot", 16u)]
    [InlineData("ComposeNumThreadsInheritRoot", 8u)]
    public void CompositionEntryPointDoesNotContributeExecutionModes(string rootName, uint expectedX)
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        foreach (var name in new[] { "CSNumThreadsOverrideBase", "ComposeNumThreadsHelper", rootName })
            shaderMixer.ShaderLoader.LoadExternalBuffer(name, [], out _, out _, out _);

        var shaderSource = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource(rootName) },
            Compositions = { ["helper"] = new ShaderClassSource("ComposeNumThreadsHelper") },
        };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        var localSize = Assert.Single(ReadExecutionModes(bytecode), m => m.Mode == Stride.Shaders.Spirv.Specification.ExecutionMode.LocalSize);
        Assert.Equal(expectedX, localSize.Parameters[0]);
        Assert.Contains(localSize.EntryPoint, ReadEntryPointIds(bytecode));
    }

    private static List<(int EntryPoint, Stride.Shaders.Spirv.Specification.ExecutionMode Mode, uint[] Parameters)> ReadExecutionModes(Span<byte> bytecode)
    {
        var result = new List<(int, Stride.Shaders.Spirv.Specification.ExecutionMode, uint[])>();
        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode);
        for (var w = 5; w < words.Length;)
        {
            var count = (int)(words[w] >> 16);
            if (count == 0)
                break;
            if ((words[w] & 0xFFFF) == (uint)Stride.Shaders.Spirv.Specification.Op.OpExecutionMode)
                result.Add(((int)words[w + 1], (Stride.Shaders.Spirv.Specification.ExecutionMode)words[w + 2], words.Slice(w + 3, count - 3).ToArray()));
            w += count;
        }
        return result;
    }

    private static List<int> ReadEntryPointIds(Span<byte> bytecode)
    {
        var result = new List<int>();
        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode);
        for (var w = 5; w < words.Length;)
        {
            var count = (int)(words[w] >> 16);
            if (count == 0)
                break;
            if ((words[w] & 0xFFFF) == (uint)Stride.Shaders.Spirv.Specification.Op.OpEntryPoint)
                result.Add((int)words[w + 2]);
            w += count;
        }
        return result;
    }

    // Regression: the MemberName re-instantiation path sets ShaderLoaderBase.SourceHashOverride and
    // relies on the load consuming it. When that load hits the shader cache and returns early, a
    // surviving override would stamp one shader's source hash onto the next one compiled, which then
    // never matches its own file and is rebuilt on every run.
    [Fact]
    public void SourceHashOverrideDoesNotLeakAcrossCacheHits()
    {
        var loader = new ShaderLoader("./assets/Stride/SDSL");

        // Warm the cache: a normally compiled shader carries its OpSourceHashSDSL.
        Assert.True(loader.LoadExternalBuffer("Texturing", [], out var texturing, out _, out _));
        Assert.True(HasSourceHash(texturing), "sanity: a normally compiled shader carries OpSourceHashSDSL");

        // Reproduce the MemberName path (Builder.Class.InstantiateMemberNames): set the override, then
        // load a shader that is already cached. The (name, filename, code) overload returns on the cache
        // hit, so the override has to be consumed before that early return.
        Assert.True(loader.LoadExternalFileContent("Texturing", out var filename, out var code, out _));
        loader.SourceHashOverride = ObjectId.FromBytes("not a shader file"u8.ToArray());
        Assert.True(loader.LoadExternalBuffer("Texturing", filename, code, [], out _, out _, out var isFromCache));
        Assert.True(isFromCache, "precondition: the second load must be a cache hit to exercise the leak");

        Assert.Null(loader.SourceHashOverride);

        // Downstream symptom: with the override leaked, the next compiled shader records that hash
        // instead of its own file's, and never matches its source again.
        Assert.True(loader.LoadExternalBuffer("ShaderBase", [], out var shaderBase, out var shaderBaseHash, out _));
        Assert.True(HasSourceHash(shaderBase));
        Assert.True(loader.LoadExternalFileContent("ShaderBase", out _, out _, out var shaderBaseFileHash));
        Assert.Equal(shaderBaseFileHash, shaderBaseHash);
    }

    private static bool HasSourceHash(ShaderBuffers buffer)
    {
        foreach (var i in buffer.Context)
        {
            if (i.Op == Stride.Shaders.Spirv.Specification.Op.OpSourceHashSDSL)
                return true;
        }
        return false;
    }

    // FileShaderCache stamps a Generator id + Schema (format version) into the SPIR-V header of each
    // cached .spv. A cached buffer round-trips (including its OpSourceHashSDSL), and a header whose
    // version no longer matches is rejected so the shader recompiles instead of being served stale.
    [Fact]
    public void FileShaderCacheRoundTripsAndRejectsStaleVersion()
    {
        var loader = new ShaderLoader("./assets/Stride/SDSL");
        Assert.True(loader.LoadExternalBuffer("Texturing", [], out var buffer, out var hash, out _));
        Assert.NotEqual(ObjectId.Empty, hash);

        var tempDir = Directory.CreateTempSubdirectory("stride-shadercache-test");
        try
        {
            var provider = new FileSystemProvider("/testcache", tempDir.FullName);
            const string spvPath = "shaders/Texturing_default.spv";

            // Write via one instance, read via a fresh instance so the load must come from disk.
            new FileShaderCache(provider).RegisterShader("Texturing", null, [], buffer, hash);

            Assert.True(new FileShaderCache(provider).TryLoadFromCache("Texturing", null, [], out _, out var loadedHash));
            Assert.Equal(hash, loadedHash);

            // Corrupt the Schema word (5th header word, byte offset 16) to simulate an old/incompatible cache.
            byte[] bytes;
            using (var s = provider.OpenStream(spvPath, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                bytes = new byte[s.Length];
                s.ReadExactly(bytes);
            }
            BitConverter.GetBytes(0x7FFFFFFF).CopyTo(bytes, 16);
            using (var s = provider.OpenStream(spvPath, VirtualFileMode.Create, VirtualFileAccess.Write))
                s.Write(bytes);

            Assert.False(new FileShaderCache(provider).TryLoadFromCache("Texturing", null, [], out _, out _),
                "a cache file with a mismatched format version must be rejected (recompiled), not served stale");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void TextureDecorateStringWarning()
    {
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
{
new ShaderClassSource("ShaderBase"),
new ShaderClassSource("ShadingBase"),
new ShaderClassSource("TransformationBase"),
new ShaderClassSource("NormalStream"),
new ShaderClassSource("TransformationWAndVP"),
new ShaderClassSource("NormalFromNormalMapping"),
new ShaderClassSource("MaterialSurfacePixelStageCompositor"),
},
            Compositions =
{
["directLightGroups"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightClusteredPointGroup")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightClusteredSpotGroup")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
["environmentLights"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightSimpleAmbient")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("EnvironmentLight")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
["materialPixelStage"] = new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceArray")},
Compositions =
{
["layers"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceDiffuse")},
Compositions ={["diffuseMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.DiffuseMap","TEXCOORD0","Material.Sampler.i0","rgba","Material.TextureScale","Material.TextureOffset")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceNormalMap","true","true")},
Compositions ={["normalMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.NormalMap","TEXCOORD0","Material.Sampler.i0","rgba","Material.TextureScale.i1","Material.TextureOffset.i1")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceGlossinessMap","false")},
Compositions ={["glossinessMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.GlossinessMap","TEXCOORD0","Material.Sampler.i0","r","Material.TextureScale.i2","Material.TextureOffset.i2")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceMetalness")},
Compositions ={["metalnessMap"] = new ShaderClassSource("ComputeColorConstantFloatLink","Material.MetalnessValue")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceLightingAndShading")},
Compositions =
{
["surfaces"] = new ShaderArraySource
{
new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert","false"),
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceShadingSpecularMicrofacet")},
Compositions =
{
["environmentFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetEnvironmentGGXLUT"),
["fresnelFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetFresnelSchlick"),
["geometricShadowingFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetVisibilitySmithSchlickGGX"),
["normalDistributionFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetNormalDistributionGGX"),
},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
["streamInitializerPixelStage"] = new ShaderMixinSource
{
Mixins =
{
new ShaderClassSource("MaterialStream"),
new ShaderClassSource("MaterialPixelShadingStream"),
},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
            Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
        };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/Stride/SDSL"));
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        var warnings = log.Messages
            .Where(m => m.Type == Stride.Core.Diagnostics.LogMessageType.Warning && m.Text.Contains("Mismatched decorations"))
            .Select(m => m.Text).ToList();
        Assert.Empty(warnings);
    }


    [Fact]
    public void Tessellation()
    {
        // Dumped from TessellationTest using ShaderSource.ToCode()
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
{
new ShaderClassSource("ShaderBase"),
new ShaderClassSource("ShadingBase"),
new ShaderClassSource("TransformationBase"),
new ShaderClassSource("NormalStream"),
new ShaderClassSource("TransformationWAndVP"),
new ShaderClassSource("NormalFromMesh"),
new ShaderClassSource("TessellationPN"),
new ShaderClassSource("TessellationAE4","PositionWS"),
new ShaderClassSource("MaterialSurfacePixelStageCompositor"),
},
            Compositions =
{
["environmentLights"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightSimpleAmbient")},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
["materialPixelStage"] = new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceArray")},
Compositions =
{
["layers"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceDiffuse")},
Compositions ={["diffuseMap"] = new ShaderClassSource("ComputeColorConstantColorLink","Material.DiffuseValue")},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceLightingAndShading")},
Compositions =
{
["surfaces"] = new ShaderArraySource
{
new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert","false"),
},
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
["streamInitializerPixelStage"] = new ShaderMixinSource
{
Mixins =
{
new ShaderClassSource("MaterialStream"),
new ShaderClassSource("MaterialPixelShadingStream"),
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
            Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
        };

        TestCore("StrideTessellation", shaderSource, "./assets/Stride/SDSL");
    }

    // Issue #3236: the MSAA resolve shaders use Texture2DMS<float4, N>. Make sure the SDSL
    // compiler can build them for each sample count (the editor enables them when MSAA > 0).
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void MsaaResolverShaderCompiles(int samples)
    {
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
            {
                new ShaderClassSource("MSAAResolverShader", samples, 1, 2.0f),
            },
            Macros =
            {
                new ShaderMacro("INPUT_MSAA_SAMPLES", samples.ToString()),
                new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
                new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", samples.ToString()),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
            },
        };

        TestCore($"MSAAResolverShader{samples}", shaderSource, "./assets/Stride/SDSL");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void MsaaDepthResolverShaderCompiles(int samples)
    {
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
            {
                new ShaderClassSource("MSAADepthResolverShader"),
            },
            Macros =
            {
                new ShaderMacro("INPUT_MSAA_SAMPLES", samples.ToString()),
                new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
                new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", samples.ToString()),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
            },
        };

        TestCore($"MSAADepthResolverShader{samples}", shaderSource, "./assets/Stride/SDSL");
    }

    // Issue #3323: LightProbeShader truncates a float3x4 to a float3x3, and it is the only
    // engine shader casting a non-square matrix. That cast used to emit invalid SPIR-V, so
    // any scene with enough light probes to form a tetrahedron failed to render.
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void LightProbeShaderCompiles(int samples)
    {
        // LightProbeRenderer puts LightProbeShader in the environmentLights slot that holds
        // EnvironmentLight when light probes are off.
        var shaderSource = EnvironmentLightMixin(samples, new ShaderClassSource("LightProbeShader", 3));

        TestCore($"LightProbeShader{samples}", shaderSource, "./assets/Stride/SDSL");
    }

    /// <summary>
    /// Builds the pixel side of a forward shading effect around a single environment light, the
    /// way ForwardLightingRenderFeature composes one. Only what an environment light needs is
    /// included: the material streams it reads, and the shading stage it writes into.
    /// </summary>
    private static ShaderMixinSource EnvironmentLightMixin(int multisampleCount, ShaderSource environmentLight)
    {
        ShaderMixinSource Compose(params ShaderClassCode[] mixins)
        {
            var source = new ShaderMixinSource();
            source.Mixins.AddRange(mixins);
            source.Macros.AddRange(
            [
                new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
                new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", multisampleCount.ToString()),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
                new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
                new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
                new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
                new ShaderMacro("class", "shader"),
            ]);
            return source;
        }

        var root = Compose(
            new ShaderClassSource("ShaderBase"),
            new ShaderClassSource("ShadingBase"),
            new ShaderClassSource("TransformationBase"),
            new ShaderClassSource("NormalStream"),
            new ShaderClassSource("TransformationWAndVP"),
            new ShaderClassSource("NormalFromNormalMapping"),
            new ShaderClassSource("MaterialSurfacePixelStageCompositor"));

        var environmentLights = new ShaderArraySource();
        environmentLights.Add(environmentLight);
        root.Compositions.Add("environmentLights", environmentLights);

        var shadingSurfaces = new ShaderArraySource();
        shadingSurfaces.Add(new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert", "false"));
        var lightingAndShading = Compose(new ShaderClassSource("MaterialSurfaceLightingAndShading"));
        lightingAndShading.Compositions.Add("surfaces", shadingSurfaces);

        var materialLayers = new ShaderArraySource();
        materialLayers.Add(Compose(new ShaderClassSource("MaterialSurfaceDiffuse")));
        materialLayers.Add(lightingAndShading);

        var materialPixelStage = Compose(new ShaderClassSource("MaterialSurfaceArray"));
        materialPixelStage.Compositions.Add("layers", materialLayers);
        root.Compositions.Add("materialPixelStage", materialPixelStage);

        root.Compositions.Add("streamInitializerPixelStage", Compose(
            new ShaderClassSource("MaterialStream"),
            new ShaderClassSource("MaterialPixelShadingStream")));

        return root;
    }

    private static void TestCore(string shaderName, ShaderMixinSource shaderSource, params string[] searchPaths)
    {
        var shaderMixer = new ShaderMixer(new ShaderLoader(searchPaths));
        var log = new Stride.Core.Diagnostics.LoggerResult();
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        if (log.HasErrors)
            Assert.Fail(string.Join(Environment.NewLine, log.Messages.Where(m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error).Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Validate SPIR-V
        var validationResult = Spv.ValidateFile($"{shaderName}.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);

        if (SpirvCrossSupport.Available)
        {
            var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
            var entryPoints = translator.GetEntryPoints();
            foreach (var entryPoint in entryPoints)
            {
                var hlsl = translator.Translate(Backend.Hlsl, entryPoint);
                Console.WriteLine(hlsl);
            }
        }
    }

    // A composition whose shader derives from the same base as its host must not contribute that
    // base's virtual method over the root's own override.
    [Fact]
    public void CompositionSharingABaseDoesNotOverrideTheRootsOverride()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        foreach (var name in new[] { "ComposeSharedBase", "ComposeSharedHelper", "ComposeSharedRoot" })
            shaderMixer.ShaderLoader.LoadExternalBuffer(name, [], out _, out _, out _);

        var shaderSource = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("ComposeSharedRoot") },
            Compositions = { ["helper"] = new ShaderClassSource("ComposeSharedHelper") },
        };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var reflection, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        // The body survived, so its texture is still live and reflected.
        var disassembly = Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        Assert.True(reflection.ResourceBindings.Any(b => b.RawName.EndsWith("WriteTex")), disassembly);
        Assert.Contains("OpImageWrite", disassembly);
    }

    // Control for the above: the very same shader, composing a helper that does not derive from
    // ComposeSharedBase. This one has always worked, and pins the difference to the shared base.
    [Fact]
    public void CompositionWithoutASharedBaseKeepsTheRootsOverride()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        foreach (var name in new[] { "ComposeSharedBase", "ComposePlainHelper", "ComposePlainRoot" })
            shaderMixer.ShaderLoader.LoadExternalBuffer(name, [], out _, out _, out _);

        var shaderSource = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("ComposePlainRoot") },
            Compositions = { ["helper"] = new ShaderClassSource("ComposePlainHelper") },
        };

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var reflection, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var disassembly = Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        Assert.True(reflection.ResourceBindings.Any(b => b.RawName.EndsWith("WriteTex")), disassembly);
        Assert.Contains("OpImageWrite", disassembly);
    }
    // `streams = input[i]` in a geometry shader assigns the members the stage input carries and must
    // leave every other stream member as it was. Checked after LegalizeForHlsl: the branch on the
    // carried value must still be a branch there, not a folded constant.
    [Fact]
    public void GeometryStreamsAssignKeepsMembersTheInputDoesNotCarry()
    {
        SpirvCrossSupport.SkipUnlessAvailable();

        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("GeometryStreamsAssignKeepsOthers", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("GeometryStreamsAssignKeepsOthers"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var legalized = SpirvTools.LegalizeForHlsl(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode.ToArray()));
        var translator = new SpirvTranslator(legalized.AsMemory());
        var geometry = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.Geometry);
        var hlsl = translator.Translate(Backend.Hlsl, geometry);

        Assert.DoesNotContain("if (true)", hlsl);
        Assert.DoesNotContain("if (false)", hlsl);
    }

    // A method with two geometry stream parameters loses both, and its call both arguments.
    [Fact]
    public void GeometryStreamsMethodWithTwoStreamParameters()
    {
        SpirvCrossSupport.SkipUnlessAvailable();

        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("GeometryStreamsTwoStreamParameters", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("GeometryStreamsTwoStreamParameters"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var legalized = SpirvTools.LegalizeForHlsl(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode.ToArray()));
        var translator = new SpirvTranslator(legalized.AsMemory());
        var geometry = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.Geometry);
        var hlsl = translator.Translate(Backend.Hlsl, geometry);

        Assert.Contains("Append", hlsl);
    }

    // A static call (Utils.Method(x)) from a stage method is not a non-stage member access.
    [Fact]
    public void StaticCallFromStageMethodDoesNotForceFullImport()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        Assert.True(shaderMixer.ShaderLoader.LoadExternalBuffer("StaticCallRoot", [], out var buffer, out _, out _));

        foreach (var i in buffer.Buffer)
        {
            if (i.Op == Stride.Shaders.Spirv.Specification.Op.OpFunctionMetadataSDSL && (Stride.Shaders.Spirv.Core.OpFunctionMetadataSDSL)i is { } metadata)
                Assert.False(metadata.Flags.HasFlag(Stride.Shaders.Spirv.Specification.FunctionFlagsMask.ReferencesNonStage),
                    "A static call is not an instance access: the stage method must stay stage-only importable.");
        }
    }

    // A non-stage variable read through its shader name (Base.Value) from a stage method is an instance access.
    [Fact]
    public void QualifiedNonStageReadFromStageMethodForcesFullImport()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        Assert.True(shaderMixer.ShaderLoader.LoadExternalBuffer("QualifiedNonStageRoot", [], out var buffer, out _, out _));

        var needsFullImport = false;
        foreach (var i in buffer.Context)
        {
            if (i.Op == Stride.Shaders.Spirv.Specification.Op.OpMixinInheritSDSL && (Stride.Shaders.Spirv.Core.OpMixinInheritSDSL)i is { } inherit
                && inherit.Flags.HasFlag(Stride.Shaders.Spirv.Specification.MixinInheritFlagsMask.NeedsFullImport)
                && buffer.Context.ReverseTypes.TryGetValue(inherit.Shader, out var inheritType) && inheritType is Stride.Shaders.Core.ShaderSymbol { Name: "QualifiedNonStageBase" })
                needsFullImport = true;
        }

        Assert.True(needsFullImport, "QualifiedNonStageBase.Value is read from a stage method: the base must be fully imported.");
    }

    // [loop] and [unroll] must reach OpLoopMerge's loop control; SPIRV-Cross turns them back into HLSL attributes.
    [Fact]
    public void LoopAttributesReachSpirvLoopControl()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        Assert.True(shaderMixer.ShaderLoader.LoadExternalBuffer("LoopControlRoot", [], out var buffer, out _, out _));

        var controls = new List<Stride.Shaders.Spirv.Specification.LoopControlMask>();
        foreach (var i in buffer.Buffer)
        {
            if (i.Op == Stride.Shaders.Spirv.Specification.Op.OpLoopMerge)
                controls.Add(((Stride.Shaders.Spirv.Core.OpLoopMerge)i).LoopControl);
        }

        Assert.Equal(
            [Stride.Shaders.Spirv.Specification.LoopControlMask.DontUnroll, Stride.Shaders.Spirv.Specification.LoopControlMask.Unroll, Stride.Shaders.Spirv.Specification.LoopControlMask.DontUnroll],
            controls);
    }

    // Writing only part of a stream the stage input does not carry must leave the rest defined.
    // Checked with fxc, which rejects an undefined read with X4000; the HLSL text alone looks fine.
    [Fact]
    public void GeometryStreamsAssignThenPartialWriteCompilesWithFxc()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var shaderMixer = new ShaderMixer(loader);
        shaderMixer.ShaderLoader.LoadExternalBuffer("GeometryStreamsAssignPartialWrite", [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("GeometryStreamsAssignPartialWrite"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var legalized = SpirvTools.LegalizeForHlsl(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode.ToArray()));
        var translator = new SpirvTranslator(legalized.AsMemory());
        var geometry = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.Geometry);
        var hlsl = translator.Translate(Backend.Hlsl, geometry);

        var errors = FxcSupport.Compile(hlsl, "gs_5_0");
        Assert.True(errors is null, errors + Environment.NewLine + hlsl);
    }
}
