using System.Runtime.InteropServices;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Shaderc;

namespace Stride.Shaders.Compilers;

public static class SpirvOptimpizer
{
    static Shaderc shaderc = Shaderc.GetApi();

    public static void Compile(string code, string entrypoint, SourceLanguage language, OptimizationLevel level, string filename = "source.shader")
    {
        unsafe
        {
            var compiler = shaderc.CompilerInitialize();
            var options = shaderc.CompileOptionsInitialize();
            shaderc.CompileOptionsSetSourceLanguage(options, language);
            shaderc.CompileOptionsSetOptimizationLevel(options, level);
            var compResult = shaderc.CompileIntoSpvAssembly(compiler, code, (nuint)code.Length, ShaderKind.FragmentShader, filename, entrypoint, options);
            var err = shaderc.ResultGetErrorMessageS(compResult);
            if (string.IsNullOrEmpty(err))
            {
                var res = shaderc.ResultGetBytesS(compResult);
                Console.WriteLine(res);
            }
        }
    }
    
    public static void Optimize(ReadOnlyMemory<uint> words)
    {
        unsafe
        {
            var bytes = MemoryMarshal.AsBytes(words.Span);
            var compiler = shaderc.CompilerInitialize();
            var options = shaderc.CompileOptionsInitialize();
            shaderc.CompileOptionsSetOptimizationLevel(options, OptimizationLevel.Size);
            var compResult = shaderc.CompileIntoSpvAssembly(compiler, DXCompiler.sampleCode, (nuint)DXCompiler.sampleCode.Length, ShaderKind.FragmentShader, "main.hlsl", "PSMain", options);
            var err = shaderc.ResultGetErrorMessageS(compResult);
            if (string.IsNullOrEmpty(err))
            {
                var res = shaderc.ResultGetBytesS(compResult);
                Console.WriteLine(res);
            }
        }
    }
}