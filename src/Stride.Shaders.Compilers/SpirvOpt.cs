using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using SoftTouch.Spirv.Core;
using SoftTouch.Spirv.Core.Buffers;

namespace Stride.Shaders.Compilers;

public static class SpirvOptimizer
{
    static Shaderc shaderc = Shaderc.GetApi();

    public static string CompileAssembly(string code, string entrypoint, SourceLanguage language, OptimizationLevel level, string filename = "source.shader")
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
                return shaderc.ResultGetBytesS(compResult);
        }
        throw new Exception("Failed to compile shader");
    }
    public static byte[] Compile(string code, string entrypoint, SourceLanguage language, OptimizationLevel level = OptimizationLevel.Size, string filename = "source.shader")
    {
        unsafe
        {
            var compiler = shaderc.CompilerInitialize();
            var options = shaderc.CompileOptionsInitialize();
            shaderc.CompileOptionsSetSourceLanguage(options, language);
            shaderc.CompileOptionsSetOptimizationLevel(options, level);
            var compResult = shaderc.CompileIntoSpv(compiler, code, (nuint)code.Length, ShaderKind.FragmentShader, filename, entrypoint, options);
            var err = shaderc.ResultGetErrorMessageS(compResult);
            if (string.IsNullOrEmpty(err))
            {
                var bytes = shaderc.ResultGetBytes(compResult);
                var length = shaderc.ResultGetLength(compResult);
                var res = new byte[length];
                new Span<byte>(bytes, (int)length).CopyTo(res.AsSpan());
                SilkMarshal.Free((nint)bytes);
                return res;
            }
        }
        throw new Exception("Failed to compile shader");
    }
    public static string Translate(string code, string entrypoint, SourceLanguage from, Backend to, OptimizationLevel level = OptimizationLevel.Zero, string filename = "source.shader")
    {
        unsafe
        {
            var compiler = shaderc.CompilerInitialize();
            var options = shaderc.CompileOptionsInitialize();
            shaderc.CompileOptionsSetSourceLanguage(options, from);
            shaderc.CompileOptionsSetOptimizationLevel(options, level);
            var compResult = shaderc.CompileIntoSpv(compiler, code, (nuint)code.Length, ShaderKind.FragmentShader, filename, entrypoint, options);
            var err = shaderc.ResultGetErrorMessageS(compResult);
            if (string.IsNullOrEmpty(err))
            {
                var bytes = shaderc.ResultGetBytes(compResult);
                var length = shaderc.ResultGetLength(compResult);
                var byteArray = new Span<byte>(bytes, (int)length);
                var res = MemoryMarshal.Cast<byte, uint>(byteArray).ToArray();
                SilkMarshal.Free((nint)bytes);
                return new SpirvTranslator(res.AsMemory()).Translate(to);
            }
        }
        throw new Exception("Failed to translate shader");
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