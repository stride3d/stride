
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Silk.NET.OpenGL;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stride.Graphics.RHI;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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
            return sdslc.Compile(text, out buffer);
        }
    }

    [Theory]
    [MemberData(nameof(GetTestFiles))]
    public void RenderTest1(string shaderName, string methodName, string args)
    {
        // Compiler shader
        var shaderMixer = new ShaderMixer(new ShaderLoader());
        shaderMixer.MergeSDSL(shaderName, out var bytecode);
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

        // Setup parameters
        var parameters = TestHeaderParser.ParseParameters(args);
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
        Assert.Equal(expectedColor, pixel);
    }

    public static IEnumerable<object[]> GetTestFiles()
    {
        foreach (var filename in Directory.EnumerateFiles("./assets/SDSL/RenderTests"))
        {
            // Parse header
            var code = File.ReadAllLines(filename);

            foreach (var test in TestHeaderParser.ParseHeaders(code))
            {
                var shadername = Path.GetFileNameWithoutExtension(filename);
                yield return [shadername, test.Name, test.Parameters];
            }
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

// Note: generated with ChatGPT
public sealed class TestHeader
{
    public string Name { get; }
    public string Parameters { get; }

    public TestHeader(string name, string parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public override string ToString() =>
        $"{Name}: {string.Join(", ", Parameters)}";
}

public static class TestHeaderParser
{
    // Matches: // TestName (Param1=..., Param2=..., ...)
    //    name  = "Test" in your example
    //    args  = "Param1=1, Param2=1, ExpectedResult=0x7F7F7F7F"
    private static readonly Regex HeaderRegex =
        new Regex(@"^\s*//\s*(?<name>[^(]+?)\s*\((?<args>.*)\)\s*$",
                  RegexOptions.Compiled);

    /// <summary>
    /// Parse all headers from the provided lines.
    /// </summary>
    public static IEnumerable<TestHeader> ParseHeaders(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var m = HeaderRegex.Match(line);
            if (!m.Success) continue;

            var name = m.Groups["name"].Value.Trim();
            var args = m.Groups["args"].Value;

            var parameters = ParseParameters(args);
            yield return new TestHeader(name, args);
        }
    }

    /// <summary>
    /// Splits "A=1, B=foo, ExpectedResult=0xFF" into a dictionary.
    /// Supports quoted values with commas: A="hello, world".
    /// </summary>
    public static Dictionary<string, string> ParseParameters(string args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in SplitArgs(args))
        {
            if (string.IsNullOrWhiteSpace(piece)) continue;

            var eqIndex = piece.IndexOf('=');
            if (eqIndex < 0)
            {
                // Parameter without value; store empty string
                var keyOnly = piece.Trim();
                if (!result.ContainsKey(keyOnly))
                    result[keyOnly] = string.Empty;
                continue;
            }

            var key = piece.Substring(0, eqIndex).Trim();
            var value = piece.Substring(eqIndex + 1).Trim();

            // Strip matching quotes if present
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Last-in wins on duplicate keys
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Splits by commas but ignores commas inside quotes.
    /// Accepts both single- and double-quoted values.
    /// </summary>
    private static IEnumerable<string> SplitArgs(string args)
    {
        if (string.IsNullOrEmpty(args))
            yield break;

        var current = new List<char>(args.Length);
        bool inSingle = false, inDouble = false;

        for (int i = 0; i < args.Length; i++)
        {
            char c = args[i];

            if (c == '\'' && !inDouble)
            {
                inSingle = !inSingle;
                current.Add(c);
                continue;
            }

            if (c == '"' && !inSingle)
            {
                inDouble = !inDouble;
                current.Add(c);
                continue;
            }

            if (c == ',' && !inSingle && !inDouble)
            {
                yield return new string(current.ToArray()).Trim();
                current.Clear();
                continue;
            }

            current.Add(c);
        }

        if (current.Count > 0)
            yield return new string(current.ToArray()).Trim();
    }
}