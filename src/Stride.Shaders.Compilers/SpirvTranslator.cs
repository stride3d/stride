using Silk.NET.Core.Native;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace Stride.Shaders.Compilers;

public unsafe record struct SpirvTranslator(ReadOnlyMemory<uint> Words)
{
    static readonly Cross cross = Cross.GetApi();

    public List<(string Name, ExecutionModel ExecutionModel)> GetEntryPoints(Backend backend = Backend.Hlsl)
    {
        Context* context = null;
        ParsedIr* ir = null;
        Compiler* compiler = null;
        if (cross.ContextCreate(&context) != Result.Success)
            throw new Exception($"{cross.ContextCreate(&context)} : Could not create spirv context");
        fixed (uint* w = Words.Span)
            if (cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir) != Result.Success)
                throw new Exception($"{cross.ContextParseSpirv(context, w, (nuint)Words.Length, &ir)} : Could not parse spirv");

        cross.ContextSetErrorCallback(context, new((void* userData, byte* errorData) =>
        {
            var error = Marshal.PtrToStringAnsi((IntPtr)errorData);
            Console.WriteLine(error);
        }), null);

        if (cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
            throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");

        var result = new List<(string Name, ExecutionModel ExecutionModel)>();
        EntryPoint * entry_points = null;
        nuint num_entry_points = 0;
        bool entryPointFound = false;
        cross.CompilerGetEntryPoints(compiler, &entry_points, &num_entry_points);
        for (int i = 0; i < (int)num_entry_points; ++i)
        {
            var entryPointModel = entry_points[i].ExecutionModel;
            var entryPointName = Marshal.PtrToStringAnsi((IntPtr)entry_points[i].Name)!;
            result.Add((entryPointName, entryPointModel));
        }


        cross.ContextReleaseAllocations(context);
        cross.ContextDestroy(context);

        return result;
    }

    public readonly string Translate(Backend backend = Backend.Hlsl, (string Name, ExecutionModel ExecutionModel)? entryPoint = null)
    {
        string? translatedCode = null;
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

        cross.ContextSetErrorCallback(context, new((void* userData, byte* errorData) =>
        {
            var error = Marshal.PtrToStringAnsi((IntPtr)errorData);
            Console.WriteLine(error);
        }), null);

        if (cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
            throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");

        if (entryPoint != null)
        {
            if (cross.CompilerSetEntryPoint(compiler, entryPoint.Value.Name, entryPoint.Value.ExecutionModel) != Result.Success)
                throw new Exception($"{cross.CompilerSetEntryPoint(compiler, entryPoint.Value.Name, entryPoint.Value.ExecutionModel)} : could not set entry point");
        }

        if (cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
            throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");
        if (cross.CompilerCompile(compiler, &translated) != Result.Success)
            throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");

        translatedCode = SilkMarshal.PtrToString((nint)translated);
        cross.ContextReleaseAllocations(context);
        cross.ContextDestroy(context);
        return translatedCode ?? throw new Exception("Could not translate code");
    }
}

