using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.SPIRV.Cross;

namespace Stride.Shaders.Compilers;

public record struct SpirvTranslator(ReadOnlyMemory<uint> Words)
{
    static readonly Cross cross = Cross.GetApi();
    
    public unsafe readonly string Translate(Backend backend = Backend.Hlsl)
    {
        Context* context = null;
        ParsedIr* ir = null;
        Compiler* compiler = null;
        Resources* resources = null;
        byte* translated = null;
        if (cross.ContextCreate(&context) != Result.Success)
            throw new Exception($"{cross.ContextCreate(&context)} : Could not create spirv context");
        
        fixed(uint* w = Words.Span)
            if(cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir) != Result.Success)
                throw new Exception($"{cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir)} : Could not parse spirv");
        
        if(cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
            throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");
        if(cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
            throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");
        if (cross.CompilerCompile(compiler, &translated) != Result.Success)
            throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");
        var translatedCode = SilkMarshal.PtrToString((nint)translated);

        return translatedCode ?? throw new Exception("Could not translate code");
    }
    public unsafe readonly void TranslateWithoutReturn(Backend backend = Backend.Hlsl)
    {
        Context* context = null;
        ParsedIr* ir = null;
        Compiler* compiler = null;
        Resources* resources = null;
        byte* translated = null;
        if (cross.ContextCreate(&context) != Result.Success)
            throw new Exception($"{cross.ContextCreate(&context)} : Could not create spirv context");
        fixed(uint* w = Words.Span)
            if(cross.ContextParseSpirv(context, w, (nuint)Words.Length, ref ir) != Result.Success)
                throw new Exception($"{cross.ContextParseSpirv(context, w, (nuint)Words.Length, ref ir)} : Could not parse spirv");
        Marshal.FreeHGlobal((nint)context);
        Marshal.FreeHGlobal(ir);
        SilkMarshal.Free((nint)ir);
        
        // if(cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
        //     throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");
        // if(cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
        //     throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");
        // if (cross.CompilerCompile(compiler, &translated) != Result.Success)
        //     throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");

        // Marshal.FreeHGlobal((nint)compiler);
        // Marshal.FreeHGlobal((nint)resources);
        // Marshal.FreeHGlobal((nint)ir);
        // Marshal.FreeHGlobal((nint)context);
        // Marshal.FreeHGlobal((nint)translated);
        // var translatedCode = SilkMarshal.PtrToString((nint)translated);

        // return translatedCode ?? throw new Exception("Could not translate code");
    }
}