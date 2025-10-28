using CommunityToolkit.HighPerformance;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using System.Diagnostics.CodeAnalysis;
using Stride.Shaders.Spirv.Core.Buffers;
using SourceLanguage = Silk.NET.Shaderc.SourceLanguage;
using Silk.NET.SPIRV;

namespace Stride.Shaders.Experiments;

public record struct TextPosition(int Line, int Character)
{
    public static implicit operator TextPosition((int, int) pos) => new TextPosition(pos.Item1, pos.Item2);
}
public static class ASTExtensions
{
    public static bool Intersects<N>(this N node, TextPosition position)
        where N : Node
    {

        if (
            position.Line + 1 >= node.Info.Line
            && position.Line + 1 <= node.Info.EndLine
            && position.Character + 1 >= node.Info.Column
            && position.Character + 1 < node.Info.Column + node.Info.Length
        )
        {
            return true;
        }
        return false;
    }
}

public static partial class Examples
{
    static uint[] words = [
            // Offset 0x00000000 to 0x0000016F
            0x03022307, 0x00050100, 0x00000E00,
            0x0C000000, 0x00000000, 0x11000200,
            0x01000000, 0x0E000300, 0x00000000,
            0x01000000, 0x0F000700, 0x04000000,
            0x01000000, 0x50534D61, 0x696E0000,
            0x02000000, 0x03000000, 0x10000300,
            0x01000000, 0x07000000, 0x03000300,
            0x05000000, 0x58020000, 0x05000600,
            0x02000000, 0x696E2E76, 0x61722E43,
            0x4F4C4F52, 0x00000000, 0x05000700,
            0x03000000, 0x6F75742E, 0x7661722E,
            0x53565F54, 0x41524745, 0x54000000,
            0x05000400, 0x01000000, 0x50534D61,
            0x696E0000, 0x47000400, 0x02000000,
            0x1E000000, 0x00000000, 0x47000400,
            0x03000000, 0x1E000000, 0x00000000,
            0x16000300, 0x04000000, 0x20000000,
            0x17000400, 0x05000000, 0x04000000,
            0x04000000, 0x20000400, 0x06000000,
            0x01000000, 0x05000000, 0x20000400,
            0x07000000, 0x03000000, 0x05000000,
            0x13000200, 0x08000000, 0x21000300,
            0x09000000, 0x08000000, 0x3B000400,
            0x06000000, 0x02000000, 0x01000000,
            0x3B000400, 0x07000000, 0x03000000,
            0x03000000, 0x36000500, 0x08000000,
            0x01000000, 0x00000000, 0x09000000,
            0xF8000200, 0x0A000000, 0x3D000400,
            0x05000000, 0x0B000000, 0x02000000,
            0x3E000300, 0x03000000, 0x0B000000,
            0xFD000100, 0x38000100
        ];
    public static void UseSpirvCross()
    {
        unsafe
        {
            var code = new SpirvTranslator(words.AsMemory());
            File.WriteAllBytes("shader.bin", words.SelectMany(x => BitConverter.GetBytes(x).Reverse()).ToArray());
            Console.WriteLine(code.Translate(Backend.Hlsl));
        }
    }
    public static void TranslateHLSL()
    {

        Console.WriteLine(SpirvOptimizer.CompileAssembly(DXCompiler.sampleCode, "PSMain", SourceLanguage.Hlsl, OptimizationLevel.Zero));
        Console.WriteLine(SpirvOptimizer.Translate(DXCompiler.sampleCode, "PSMain", SourceLanguage.Hlsl, Backend.Hlsl));
    }

    public static void CompileHLSL()
    {
        var dxc = new DXCompiler();
        dxc.Compile(DXCompiler.sampleCode, out var compiled);
    }
    public static void CompileOldHLSL()
    {

        var fxc = new FXCompiler();
        fxc.Compile(DXCompiler.sampleCode, out var compiled);
    }
    public static void SpvOpt()
    {

        // var spvopt = new SpirvOptimpizer();
        // spvopt.Optimize(words);
    }

    public static void ParseSDSL()
    {
        var text = MonoGamePreProcessor.OpenAndRun("./assets/SDSL/Test.sdsl");
        var parsed = SDSLParser.Parse(text);
        Console.WriteLine(parsed.AST);
        if (parsed.Errors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var e in parsed.Errors)
                Console.WriteLine(e);
        }
    }

    public static void TryAllFiles()
    {
        foreach (var f in Directory.EnumerateFiles("./assets/Stride/SDSL"))
        {
            // var text = File.ReadAllText(f);
            if (f.Contains("BasicMixin.sdsl"))
                continue;
            var preprocessed = MonoGamePreProcessor.OpenAndRun(f);
            var parsed = SDSLParser.Parse(preprocessed);
            if (parsed.Errors.Count > 0)
            {
                Console.WriteLine(preprocessed);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Join("; ", parsed.Errors.Select(x => x.ToString())));
                Console.WriteLine(f);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            }
            else
            {
                Console.WriteLine(f);
            }
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
    static bool ComputeIntersection(TextPosition position, Node node, out Node n)
    {
        n = null!;
        if (node is ShaderFile sf)
        {
            foreach (var ns in sf.Namespaces)
                if (ns.Intersects(position))
                    return ComputeIntersection(position, ns, out n);
            foreach (var e in sf.RootDeclarations)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out n);
        }
        else if (node is ShaderNamespace sn)
        {
            if (sn.Namespace is not null && sn.Namespace.Intersects(position))
            {
                n = sn.Namespace;
                return true;
            }
            foreach (var decl in sn.Declarations)
            {
                if (decl.Intersects(position))
                    return ComputeIntersection(position, decl, out n);
            }
        }
        else if (node is ShaderClass sc)
        {
            if (sc.Name.Intersects(position))
            {
                n = sc.Name;
                return true;
            }
            foreach (var parent in sc.Mixins)
                if (parent.Intersects(position))
                {
                    n = parent;
                    return true;
                }
            foreach (var e in sc.Elements)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out n);
        }
        else if (node is ShaderMethod method)
        {
            if (method.Name.Intersects(position))
            {
                n = method.Name;
                return true;
            }
            foreach (var arg in method.Parameters)
                if (arg.Intersects(position))
                {
                    n = arg;
                    return true;
                }
            if (method.Body is not null)
                foreach (var s in method.Body.Statements)
                    if (s.Intersects(position))
                        return ComputeIntersection(position, s, out n);
        }
        return false;
    }

    public class ShaderLoader : ShaderLoaderBase
    {
        public override bool LoadExternalFile(string name, [MaybeNullWhen(false)] out NewSpirvBuffer buffer)
        {
            var filename = $"./assets/SDSL/{name}.sdsl";
            if (!File.Exists(filename))
            {
                buffer = null;
                return false;
            }
            var text = MonoGamePreProcessor.OpenAndRun(filename);
            var sdslc = new SDSLC
            {
                ShaderLoader = this
            };
            return sdslc.Compile(text, out buffer);
        }
    }

    public static void CompileSDSL(string shaderName)
    {
        // if(Directory.GetCurrentDirectory().Contains("bin\\Debug"))
        // {
        //     var info = new DirectoryInfo(Directory.GetCurrentDirectory());
        //     while(!info.GetDirectories().Any(d => d.Name is "assets") || !info.GetFiles().Any(d => d.Name is "SDSL.sln") )
        //         info = info.Parent!;
        //     Directory.SetCurrentDirectory(info.FullName);
        // }
        var text = MonoGamePreProcessor.OpenAndRun($"./assets/SDSL/{shaderName}.sdsl");

        var sdslc = new SDSLC
        {
            ShaderLoader = new ShaderLoader()
        };
        if (sdslc.Compile(text, out var buffer) && buffer is not null)
        {
            Spirv.Tools.Spv.Dis(buffer, writeToConsole: true);
            var bytecode = buffer.ToBytecode();
            File.WriteAllBytes("TestBasic.sdspv", bytecode);
            var code = new SpirvTranslator(bytecode.AsMemory().Cast<byte, uint>());
        }

        // Console.WriteLine(code.Translate(Backend.Hlsl));

    }
}