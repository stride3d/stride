using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.SPIRV.Cross;

namespace Stride.Shaders.Compilers;

public record struct SpirvTranslator(ReadOnlyMemory<uint> Words)
{
    static readonly Cross cross = Cross.GetApi();
    
    public readonly string Translate(Backend backend = Backend.Hlsl)
    {
        string? translatedCode = null;
        unsafe
        {
            Context* context = null;
            ParsedIr* ir = null;
            Compiler* compiler = null;
            Resources* resources = null;
            byte* translated = null;
            if (cross.ContextCreate(&context) != Result.Success)
                throw new Exception($"{cross.ContextCreate(&context)} : Could not create spirv context");
            fixed (uint* w = Words.Span)
                if (cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir) != Result.Success)
                    throw new Exception($"{cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir)} : Could not parse spirv");

            if (cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
                throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");
            if (cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
                throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");
            if (cross.CompilerCompile(compiler, &translated) != Result.Success)
                throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");
            translatedCode = SilkMarshal.PtrToString((nint)translated);
            cross.ContextReleaseAllocations(context);
            cross.ContextDestroy(context);
        }
        return translatedCode ?? throw new Exception("Could not translate code");
    }
}