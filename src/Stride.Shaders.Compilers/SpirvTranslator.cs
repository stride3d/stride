using Silk.NET.Core.Native;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace Stride.Shaders.Compilers;

using Compiler = Silk.NET.SPIRV.Cross.Compiler;

public unsafe record struct SpirvTranslator(ReadOnlyMemory<uint> Words)
{
    static readonly Cross cross = Cross.GetApi();

    public List<(string RealName, string TranslatedName, ExecutionModel ExecutionModel)> GetEntryPoints(Backend backend = Backend.Hlsl)
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

        var result = new List<(string RealName, string TranslatedName, ExecutionModel ExecutionModel)>();
        EntryPoint * entry_points = null;
        nuint num_entry_points = 0;
        bool entryPointFound = false;
        cross.CompilerGetEntryPoints(compiler, &entry_points, &num_entry_points);
        for (int i = 0; i < (int)num_entry_points; ++i)
        {
            var entryPointModel = entry_points[i].ExecutionModel;
            var entryPointName = Marshal.PtrToStringAnsi((IntPtr)entry_points[i].Name)!;
            result.Add((entryPointName, "main", entryPointModel));
        }


        cross.ContextReleaseAllocations(context);
        cross.ContextDestroy(context);

        return result;
    }

    public readonly string Translate(Backend backend = Backend.Hlsl, (string RealName, string TranslatedName, ExecutionModel ExecutionModel)? entryPoint = null)
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

        if (backend == Backend.Hlsl)
        {
            CompilerOptions* compilerOptions = null;
            cross.CompilerCreateCompilerOptions(compiler, ref compilerOptions);
            cross.CompilerOptionsSetUint(compilerOptions, CompilerOption.HlslShaderModel, 50);
            cross.CompilerInstallCompilerOptions(compiler, compilerOptions);
        }

        if (entryPoint != null)
        {
            if (cross.CompilerSetEntryPoint(compiler, entryPoint.Value.RealName, entryPoint.Value.ExecutionModel) != Result.Success)
                throw new Exception($"{cross.CompilerSetEntryPoint(compiler, entryPoint.Value.RealName, entryPoint.Value.ExecutionModel)} : could not set entry point");
        }

        if (cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
            throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");

        if (cross.CompilerBuildCombinedImageSamplers(compiler) != Result.Success)
            throw new Exception($"{cross.CompilerBuildCombinedImageSamplers(compiler)} : Could not enable combined image samplers");

        // HLSL: remove type_ prefix from cbuffer (they get names from struct instead of cbuffer variable itself)
        if (backend == Backend.Hlsl)
        {
            ReflectedResource* resourcesList;
            nuint resourcesCount;
            cross.ResourcesGetResourceListForType(resources, ResourceType.UniformBuffer, &resourcesList, &resourcesCount);
            for (uint i = 0; i < resourcesCount; ++i)
            {
                var resource = resourcesList[i];
                var cbufferName = Marshal.PtrToStringAnsi((IntPtr)resource.Name);
                if (cbufferName.StartsWith("type."))
                {
                    cbufferName = cbufferName.Substring("type.".Length);
                    cross.CompilerSetName(compiler, resource.BaseTypeId, cbufferName);
                }
            }

            // Inputs: Apply UserSemantic (instead of TEXCOORD)
            // This is a workaround until SPIRV-Cross supports UserSemantic
            cross.ResourcesGetResourceListForType(resources, ResourceType.StageInput, &resourcesList, &resourcesCount);
            var vertexInputRemap = stackalloc HlslVertexAttributeRemap[(int)resourcesCount];
            var vertexInputRemapCount = 0;
            for (uint i = 0; i < resourcesCount; ++i)
            {
                if (cross.CompilerHasDecoration(compiler, resourcesList[i].Id, Decoration.Location) != 0
                    && cross.CompilerHasDecoration(compiler, resourcesList[i].Id, Decoration.UserSemantic) != 0)
                {
                    vertexInputRemap[vertexInputRemapCount].Location = cross.CompilerGetDecoration(compiler, resourcesList[i].Id, Decoration.Location);
                    vertexInputRemap[vertexInputRemapCount].Semantic = cross.CompilerGetDecorationString(compiler, resourcesList[i].Id, Decoration.UserSemantic);
                    vertexInputRemapCount++;
                }
            }
            cross.CompilerHlslAddVertexAttributeRemap(compiler, vertexInputRemap, (nuint)vertexInputRemapCount);
        }

        nuint numSamplers = 0;
        CombinedImageSampler* combinedImageSamplers = null;
        if (cross.CompilerGetCombinedImageSamplers(compiler, &combinedImageSamplers, ref numSamplers) != Result.Success)
            throw new Exception($"{cross.CompilerGetCombinedImageSamplers(compiler, &combinedImageSamplers, ref numSamplers)}");

        for (uint i = 0; i < numSamplers; ++i)
        {
            var textureName = cross.CompilerGetNameS(compiler, combinedImageSamplers[i].ImageId);
            var samplerName = cross.CompilerGetNameS(compiler, combinedImageSamplers[i].SamplerId);
            cross.CompilerSetName(compiler, combinedImageSamplers[i].CombinedId, $"SPIRV_Cross_Combined{textureName}{samplerName}");
        }

        if (cross.CompilerCompile(compiler, &translated) != Result.Success)
            throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");

        translatedCode = SilkMarshal.PtrToString((nint)translated);
        cross.ContextReleaseAllocations(context);
        cross.ContextDestroy(context);
        return translatedCode ?? throw new Exception("Could not translate code");
    }
}

