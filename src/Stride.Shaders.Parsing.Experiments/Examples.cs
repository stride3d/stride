using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Experiments;

public static class Examples
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
            Console.WriteLine(code.Translate(Backend.Hlsl));
        }
    }
    public static void TranslateHLSL()
    {
        
        Console.WriteLine(SpirvOptimizer.CompileAssembly(DXCompiler.sampleCode,"PSMain", SourceLanguage.Hlsl, OptimizationLevel.Zero));
        Console.WriteLine(SpirvOptimizer.Translate(DXCompiler.sampleCode,"PSMain", SourceLanguage.Hlsl, Backend.Hlsl));
    }

    public static void CompileHLSL()
    {
        var dxc = new DXCompiler(DXCompiler.sampleCode);
        dxc.Compile();
    }
    public static void CompileOldHLSL()
    {

        var fxc = new FXCompiler();
        fxc.Compile();
    }
    public static void SpvOpt()
    {

        // var spvopt = new SpirvOptimpizer();
        // spvopt.Optimize(words);
    }

    public static void ParseSDSL()
    {
        var text = MonoGamePreProcessor.Run("./assets/Stride/SDSL/BufferToTextureColumnsEffect.sdsl", []);
        var parsed = SDSLParser.Parse(text);
        Console.WriteLine(parsed.AST);
        if(parsed.Errors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var e in parsed.Errors)
                Console.WriteLine(e);
        }
    }

    public static void TryAllFiles()
    {
        foreach(var f in Directory.EnumerateFiles("./assets/Stride/SDSL"))
        {
            // var text = File.ReadAllText(f);
            if (f.Contains("BasicMixin.sdsl"))
                continue;
            var preprocessed = MonoGamePreProcessor.Run(f, []);
            var parsed = SDSLParser.Parse(preprocessed);
            if(parsed.Errors.Count > 0)
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
                // Console.WriteLine(f);
            }
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
}