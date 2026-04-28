
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core.Storage;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

[Collection("D3D11")]
public partial class RenderingTests
{
    static int width = 1;
    static int height = 1;

    [Theory]
    [MemberData(nameof(GetComputeTestFiles))]
    public void ComputeTest1(string shaderName)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/ComputeTests"));
        var shaderSource = ShaderMixinManager.Contains(shaderName)
            ? new ShaderMixinGeneratorSource(shaderName)
            : (ShaderSource)new ShaderClassSource(shaderName);

        // Force file to be parsed and all its shaders registered
        // (since there are multiple shader/effects in a simple file, simply using the effect would not go through normal load and it wouldn't know about the shaders in the file)
        shaderMixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Validate SPIR-V
        var validationResult = Spv.ValidateFile($"{shaderName}.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);

        // Convert to GLSL
        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codeCS = translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.GLCompute));

        Console.WriteLine(codeCS);

        // Execute test
        var renderer = new D3D11FrameRenderer((uint)width, (uint)height);

        renderer.ComputeShaderSource = codeCS;
        renderer.EffectReflection = effectReflection;

        var code = File.ReadAllLines($"./assets/SDSL/ComputeTests/{shaderName}.sdsl");
        foreach (var test in TestHeaderParser.ParseHeaders(code))
        {
            var parameters = TestHeaderParser.ParseParameters(test.Parameters);
            SetupTestParameters(renderer, parameters);

            renderer.SetupTest();
            renderer.Compute();
            // Present is useful for RenderDoc and other graphics capture programs
            renderer.PresentAndFinish();
        }
    }

    [Fact]
    public void DuplicateCBufferNameSurvivesMixerRename()
    {
        // Regression: when two shader classes declare a cbuffer with the same
        // source-level name (e.g. PerDraw / Settings), MergeCBuffers groups
        // them by GetCBufferRealName but for count==1 (after one is optimized
        // out) it must rewrite the surviving variable's OpName back to the
        // unsuffixed original. Otherwise SPIRV-Cross emits e.g. `Settings_1`
        // which mismatches EffectReflection's `Settings` lookup at
        // ShaderCompiler.UpdateReflection.
        const string shaderName2 = "CSCBufferRename";
        var shaderMixer2 = new ShaderMixer(new ShaderLoader("./assets/SDSL/ComputeTests"));
        shaderMixer2.ShaderLoader.LoadExternalBuffer(shaderName2, [], out _, out _, out _);

        var log2 = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer2.MergeSDSL(new ShaderClassSource(shaderName2), new ShaderMixer.Options(true), log2, out var bytecode2, out _, out _, out _),
            string.Join(Environment.NewLine, log2.Messages.Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName2}.spv", bytecode2);
        File.WriteAllText($"{shaderName2}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode2), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));
        var translator2 = new SpirvTranslator(bytecode2.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoint2 = translator2.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute);
        var hlsl2 = translator2.Translate(Backend.Hlsl, entryPoint2);
        File.WriteAllText($"{shaderName2}.hlsl", hlsl2);

        Assert.Contains("cbuffer Settings ", hlsl2);
        Assert.DoesNotContain("cbuffer Settings_", hlsl2);
    }

    [Fact]
    public void UnsizedConstArrayInfersSizeForIndexing()
    {
        // Regression: `static const uint info[] = {...};` was kept as ArrayType
        // with Size=-1 even after the initializer fixed the count, so indexing
        // `info[i]` allocated a Function temp typed as OpTypeRuntimeArray and
        // then OpStored the OpSpecConstantComposite of OpTypeArray<7> into it —
        // SPIR-V validation rejects the type mismatch.
        const string shaderName3 = "CSConstArrayInfer";
        var shaderMixer3 = new ShaderMixer(new ShaderLoader("./assets/SDSL/ComputeTests"));
        shaderMixer3.ShaderLoader.LoadExternalBuffer(shaderName3, [], out _, out _, out _);

        var log3 = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer3.MergeSDSL(new ShaderClassSource(shaderName3), new ShaderMixer.Options(true), log3, out var bytecode3, out _, out _, out _),
            string.Join(Environment.NewLine, log3.Messages.Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName3}.spv", bytecode3);
        var validation = Spv.ValidateFile($"{shaderName3}.spv");
        Assert.True(validation.IsValid, validation.Output);
    }

    [Fact]
    public void StructuredBufferEmitsStructuredBufferHlsl()
    {
        // Regression: StructuredBuffer<T>/RWStructuredBuffer<T> must reach HLSL as the
        // matching types, not RWByteAddressBuffer. Requires NonWritable + UserTypeGOOGLE
        // decorations to survive ShaderMixer / type-duplicate elimination.
        const string shaderName = "CSStructuredBuffer";
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/ComputeTests"));
        shaderMixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoint = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute);
        var hlsl = translator.Translate(Backend.Hlsl, entryPoint);
        File.WriteAllText($"{shaderName}.hlsl", hlsl);

        Assert.Contains("StructuredBuffer<Entry>", hlsl);
        Assert.Contains("RWStructuredBuffer<Entry>", hlsl);
        Assert.DoesNotContain("ByteAddressBuffer", hlsl);
    }

    [Theory]
    [MemberData(nameof(GetRenderTestFiles))]
    public void RenderTest1(string shaderName)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/RenderTests"));
        var shaderSource = ShaderMixinManager.Contains(shaderName)
            ? new ShaderMixinGeneratorSource(shaderName)
            : (ShaderSource)new ShaderClassSource(shaderName);

        // Force file to be parsed and all its shaders registered
        // (since there are multiple shader/effects in a simple file, simply using the effect would not go through normal load and it wouldn't know about the shaders in the file)
        shaderMixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        if (log.HasErrors)
            Assert.Fail(string.Join(Environment.NewLine, log.Messages.Where(m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error).Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Validate SPIR-V
        var validationResult = Spv.ValidateFile($"{shaderName}.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);

        // Convert to HLSL
        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codePS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Fragment)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Fragment))
            : null;
        var codeVS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Vertex)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Vertex))
            : null;

        if (codeVS != null)
            Console.WriteLine(codeVS);
        if (codePS != null)
            Console.WriteLine(codePS);

        // Execute test
        var renderer = new D3D11FrameRenderer((uint)width, (uint)height);

        if (codeVS != null)
            renderer.VertexShaderSource = codeVS;
        if (codePS != null)
            renderer.PixelShaderSource = codePS;
        renderer.EffectReflection = effectReflection;

        var code = File.ReadAllLines($"./assets/SDSL/RenderTests/{shaderName}.sdsl");
        foreach (var test in TestHeaderParser.ParseHeaders(code))
        {
            var parameters = TestHeaderParser.ParseParameters(test.Parameters);
            SetupTestParameters(renderer, parameters);

            using var frameBuffer = MemoryOwner<byte>.Allocate(width * height * 4);
            renderer.SetupTest();
            renderer.RenderFrame(frameBuffer.Span);
            // Present is useful for RenderDoc and other graphics capture programs
            renderer.PresentAndFinish();
            var pixels = Image.LoadPixelData<Rgba32>(frameBuffer.Span, width, height);
            Assert.Equal(width, pixels.Width);
            Assert.Equal(height, pixels.Height);

            // Check output color value against expected result
            var expectedColor = StringToRgba(parameters["ExpectedResult"]);
            var pixel = pixels[0, 0].PackedValue;
            // Swap endianess
            pixel = ((pixel & 0x000000FF) << 24)
               | (pixel & 0x0000FF00) << 8
               | ((pixel & 0x00FF0000) >> 8)
               | (pixel & 0xFF000000) >> 24;

            Assert.Equal(expectedColor.ToString("X8"), pixel.ToString("X8"));
        }
    }

    [Theory]
    [MemberData(nameof(GetStreamOutTestFiles))]
    public void StreamOutTest1(string shaderName)
    {
        // Compile shader
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/StreamOutTests"));
        var shaderSource = ShaderMixinManager.Contains(shaderName)
            ? new ShaderMixinGeneratorSource(shaderName)
            : (ShaderSource)new ShaderClassSource(shaderName);

        shaderMixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);

        var log = new Stride.Core.Diagnostics.LoggerResult();
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Validate SPIR-V
        var validationResult = Spv.ValidateFile($"{shaderName}.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);

        // Convert to HLSL
        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codeVS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Vertex)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Vertex))
            : null;
        var codeHS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.TessellationControl)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.TessellationControl))
            : null;
        var codeDS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.TessellationEvaluation)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.TessellationEvaluation))
            : null;
        var codeGS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Geometry)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Geometry))
            : null;
        var codePS = entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Fragment)
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Fragment))
            : null;

        if (codeVS != null)
            Console.WriteLine(codeVS);
        if (codeHS != null)
            Console.WriteLine(codeHS);
        if (codeDS != null)
            Console.WriteLine(codeDS);
        if (codeGS != null)
            Console.WriteLine(codeGS);
        if (codePS != null)
            Console.WriteLine(codePS);

        // Execute test
        var renderer = new D3D11FrameRenderer((uint)width, (uint)height);

        if (codeVS != null)
            renderer.VertexShaderSource = codeVS;
        if (codeHS != null)
            renderer.HullShaderSource = codeHS;
        if (codeDS != null)
            renderer.DomainShaderSource = codeDS;
        if (codeGS != null)
            renderer.GeometryShaderSource = codeGS;
        if (codePS != null)
            renderer.PixelShaderSource = codePS;
        renderer.EffectReflection = effectReflection;

        var code = File.ReadAllLines($"./assets/SDSL/StreamOutTests/{shaderName}.sdsl");
        foreach (var test in TestHeaderParser.ParseHeaders(code))
        {
            var parameters = TestHeaderParser.ParseParameters(test.Parameters);
            SetupTestParameters(renderer, parameters);

            renderer.SetupTest();
            renderer.RenderFrameWithStreamOutput(out var soData, out var soVertexCount);
            renderer.PresentAndFinish();

            Console.WriteLine($"SO: {soVertexCount} primitives, {soData.Length} bytes");

            if (parameters.TryGetValue("ExpectedPrimitiveCount", out var expectedPrimCountStr))
            {
                var expectedPrimCount = int.Parse(expectedPrimCountStr);
                Assert.Equal(expectedPrimCount, soVertexCount);
            }
            else
            {
                Assert.True(soVertexCount > 0, "Stream output produced no primitives");
            }
        }
    }

    private static void SetupTestParameters(D3D11FrameRenderer renderer, Dictionary<string, string> parameters)
    {
        // Setup parameters
        renderer.Parameters.Clear();
        foreach (var param in parameters)
            renderer.Parameters.Add(param.Key, param.Value);
    }

    public static IEnumerable<object[]> GetRenderTestFiles()
    {
        foreach (var filename in Directory.EnumerateFiles("./assets/SDSL/RenderTests"))
        {
            // Parse header
            var shadername = Path.GetFileNameWithoutExtension(filename);
            yield return [shadername];
        }
    }

    public static IEnumerable<object[]> GetStreamOutTestFiles()
    {
        foreach (var filename in Directory.EnumerateFiles("./assets/SDSL/StreamOutTests"))
        {
            var shadername = Path.GetFileNameWithoutExtension(filename);
            yield return [shadername];
        }
    }

    public static IEnumerable<object[]> GetComputeTestFiles()
    {
        foreach (var filename in Directory.EnumerateFiles("./assets/SDSL/ComputeTests"))
        {
            // Parse header
            var shadername = Path.GetFileNameWithoutExtension(filename);
            yield return [shadername];
        }
    }

    public static uint StringToRgba(string? stringColor)
    {
        var intValue = 0xFF000000;
        if (stringColor?.StartsWith('#') == true)
        {
            if (stringColor.Length == "#00000000".Length)
                uint.TryParse(stringColor.AsSpan(1, 8), NumberStyles.HexNumber, null, out intValue);
        }
        return intValue;
    }
}
