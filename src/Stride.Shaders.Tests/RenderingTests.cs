
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
using System.Text;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsing.Tests;

public class RenderingTests
{
    static int width = 1;
    static int height = 1;

    class ShaderLoader(string basePath) : ShaderLoaderBase
    {
        protected override bool ExternalFileExists(string name)
        {
            var filename = $"{basePath}/{name}.sdsl";
            return File.Exists(filename);
        }

        protected override bool LoadExternalFileContent(string name, out string filename, out string code)
        {
            filename = $"{basePath}/{name}.sdsl";
            code = File.ReadAllText(filename);
            return true;
        }

        protected override bool LoadFromCode(string filename, string code, ReadOnlySpan<ShaderMacro> macros, out SpirvBytecode buffer)
        {
            var result = base.LoadFromCode(filename, code, macros, out buffer);
            if (result)
            {
                Console.WriteLine($"Loading shader {filename}");
                Spv.Dis(buffer, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
            }
            return result;
        }

        public override void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, SpirvBytecode bytecode)
        {
            base.RegisterShader(name, defines, bytecode);

            Console.WriteLine($"Registering shader {name}");
            Spv.Dis(bytecode, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        }
    }

    [Theory]
    [MemberData(nameof(GetComputeTestFiles))]
    public void ComputeTest1(string shaderName)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/ComputeTests"));
        shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), out var bytecode, out var effectReflection);

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateBufferFromBytecode(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

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

    [Theory]
    [MemberData(nameof(GetRenderTestFiles))]
    public void RenderTest1(string shaderName)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/RenderTests"));
        shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), out var bytecode, out var effectReflection);

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateBufferFromBytecode(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Convert to GLSL
        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codePS = translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Fragment));
        var codeVS = (entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Vertex))
            ? translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Vertex))
            : null;

        if (codeVS != null)
            Console.WriteLine(codeVS);
        Console.WriteLine(codePS);

        // Execute test
        var renderer = new D3D11FrameRenderer((uint)width, (uint)height);

        if (codeVS != null)
            renderer.VertexShaderSource = codeVS;
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
