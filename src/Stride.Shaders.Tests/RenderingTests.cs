
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

    class ShaderLoader : ShaderLoaderBase
    {
        public override bool LoadExternalFile(string name, [MaybeNullWhen(false)] out NewSpirvBuffer buffer)
        {
            var filename = $"./assets/SDSL/RenderTests/{name}.sdsl";
            if (!File.Exists(filename))
            {
                buffer = null;
                return false;
            }
            var text = MonoGamePreProcessor.OpenAndRun(filename);
            var sdslc = new SDSLC();
            sdslc.ShaderLoader = this;

            var result = sdslc.Compile(text, out buffer);
#if DEBUG
            if (result)
            {
                Spv.Dis(buffer, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
            }
#endif
            return result;
        }
    }

    [Theory]
    [MemberData(nameof(GetTestFiles))]
    public void RenderTest1(string shaderName)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader());
        shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), out var bytecode);
        File.WriteAllBytes($"{shaderName}.spv", bytecode);

        // Convert to GLSL
        var translator = new SpirvTranslator(bytecode.AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codePS = translator.Translate(Backend.Glsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Fragment));
        var codeVS = (entryPoints.Any(x => x.ExecutionModel == ExecutionModel.Vertex))
            ? translator.Translate(Backend.Glsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.Vertex))
            : null;

        if (codeVS != null)
            Console.WriteLine(codeVS);
        Console.WriteLine(codePS);

        // Execute test
        var renderer = new OpenGLFrameRenderer((uint)width, (uint)height);

        var code = File.ReadAllLines($"./assets/SDSL/RenderTests/{shaderName}.sdsl");
        foreach (var test in TestHeaderParser.ParseHeaders(code))
        {
            renderer.Parameters.Clear();

            // Setup parameters
            var parameters = TestHeaderParser.ParseParameters(test.Parameters);
            foreach (var param in parameters)
                renderer.Parameters.Add(param.Key, param.Value);

            renderer.FragmentShaderSource = codePS;
            if (codeVS != null)
                renderer.VertexShaderSource = codeVS;
            using var frameBuffer = MemoryOwner<byte>.Allocate(width * height * 4);
            renderer.RenderFrame(frameBuffer.Span);
            var pixels = Image.LoadPixelData<Rgba32>(frameBuffer.Span, width, height);
            Assert.Equal(width, pixels.Width);
            Assert.Equal(height, pixels.Height);

            // Check output color value against expected result
            var expectedColor = StringToRgba(parameters["ExpectedResult"]);
            var pixel = pixels[0, 0].PackedValue;
            Assert.Equal(expectedColor.ToString("X8"), pixel.ToString("X8"));
        }
    }

    public static IEnumerable<object[]> GetTestFiles()
    {
        foreach (var filename in Directory.EnumerateFiles("./assets/SDSL/RenderTests"))
        {
            // Parse header
            var code = File.ReadAllLines(filename);
            var shadername = Path.GetFileNameWithoutExtension(filename);
            yield return [shadername];
        }

        yield break;
    }

    public static uint StringToRgba(string? stringColor)
    {
        var intValue = 0xFF000000;
        if (stringColor?.StartsWith('#') == true)
        {
            if (stringColor.Length == "#00000000".Length && uint.TryParse(stringColor.AsSpan(1, 8), NumberStyles.HexNumber, null, out intValue))
            {
                intValue = ((intValue & 0x000000FF) << 24)
                           | (intValue & 0x0000FF00) << 8
                           | ((intValue & 0x00FF0000) >> 8)
                           | (intValue & 0xFF000000) >> 24;
            }
        }
        return intValue;
    }
}
