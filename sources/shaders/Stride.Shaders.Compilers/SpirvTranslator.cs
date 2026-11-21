using Silk.NET.Core.Native;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core;
using Stride.Core.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.Shaders.Compilers;

using Compiler = Silk.NET.SPIRV.Cross.Compiler;
using Result = Silk.NET.SPIRV.Cross.Result;

public unsafe record struct SpirvTranslator(ReadOnlyMemory<uint> Words)
{
    static readonly Cross cross;
    static readonly Logger Log = GlobalLogger.GetLogger("SpirvTranslator");
    static readonly Regex StructuredBufferOfMatrix = new(@"\b((?:RW|Append|Consume)?StructuredBuffer\s*<\s*)((?:float|half|double|int|uint|min16float|min16int|min16uint)[1-4]x[1-4])\s*>", RegexOptions.Compiled);

    static SpirvTranslator()
    {
        NativeLibraryHelper.PreloadLibrary("spirv-cross", typeof(SpirvTranslator));
        cross = Cross.GetApi();
    }

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
            if (error != null)
                Log.Error(error);
        }), null);

        if (cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
            throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");

        var result = new List<(string RealName, string TranslatedName, ExecutionModel ExecutionModel)>();
        EntryPoint* entry_points = null;
        nuint num_entry_points = 0;
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
            if (error != null)
                Log.Error(error);
        }), null);

        if (cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler) != Result.Success)
            throw new Exception($"{cross.ContextCreateCompiler(context, backend, ir, CaptureMode.Copy, &compiler)} : could not create compiler");

        {
            CompilerOptions* compilerOptions = null;
            cross.CompilerCreateCompilerOptions(compiler, ref compilerOptions);
            // Don't emit #line directives — they reference original .sdsl files which confuse
            // RenderDoc's shader viewer when displaying the cross-compiled HLSL/GLSL.
            // OpLine in the SPIR-V is preserved for Vulkan/RenderDoc callstacks.
            cross.CompilerOptionsSetBool(compilerOptions, CompilerOption.EmitLineDirectives, 0);
            if (backend == Backend.Hlsl)
            {
                cross.CompilerOptionsSetUint(compilerOptions, CompilerOption.HlslShaderModel, 50);
                cross.CompilerOptionsSetBool(compilerOptions, CompilerOption.HlslPreserveStructuredBuffers, 1);
            }
            cross.CompilerInstallCompilerOptions(compiler, compilerOptions);
        }

        if (entryPoint != null)
        {
            if (cross.CompilerSetEntryPoint(compiler, entryPoint.Value.RealName, entryPoint.Value.ExecutionModel) != Result.Success)
                throw new Exception($"{cross.CompilerSetEntryPoint(compiler, entryPoint.Value.RealName, entryPoint.Value.ExecutionModel)} : could not set entry point");
        }

        if (cross.CompilerCreateShaderResources(compiler, &resources) != Result.Success)
            throw new Exception($"{cross.CompilerCreateShaderResources(compiler, &resources)} : could not create shader resources");

        // HLSL: remove type_ prefix from cbuffer (they get names from struct instead of cbuffer variable itself)
        if (backend == Backend.Hlsl)
        {

            ReflectedResource* resourcesList;
            nuint resourcesCount;
            cross.ResourcesGetResourceListForType(resources, ResourceType.UniformBuffer, &resourcesList, &resourcesCount);
            for (uint i = 0; i < resourcesCount; ++i)
            {
                var resource = resourcesList[i];
                var cbufferName = Marshal.PtrToStringAnsi((IntPtr)resource.Name)!;
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

            cross.CompilerHlslSetResourceBindingFlags(compiler, 0);
        }

        if (backend == Backend.Glsl)
        {
            if (cross.CompilerBuildCombinedImageSamplers(compiler) != Result.Success)
                throw new Exception($"{cross.CompilerBuildCombinedImageSamplers(compiler)} : Could not enable combined image samplers");

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
        }

        if (cross.CompilerCompile(compiler, &translated) != Result.Success)
            throw new Exception($"{cross.CompilerCompile(compiler, &translated)} : could not compile code");

        translatedCode = SilkMarshal.PtrToString((nint)translated);
        cross.ContextReleaseAllocations(context);
        cross.ContextDestroy(context);
        if (translatedCode == null)
            throw new Exception("Could not translate code");
        return backend == Backend.Hlsl ? WithRowMajorStructuredBufferElements(translatedCode) : translatedCode;
    }

    // SPIRV-Cross writes no layout on a matrix that is the element of a structured buffer (always SDSL's default, row_major),
    // and FXC then reads it column_major: only a typedef can say row_major there. Remove once SPIRV-Cross writes that layout.
    static string WithRowMajorStructuredBufferElements(string hlsl)
    {
        var elementTypes = new SortedSet<string>(StringComparer.Ordinal);
        var result = StructuredBufferOfMatrix.Replace(hlsl, match =>
        {
            elementTypes.Add(match.Groups[2].Value);
            return $"{match.Groups[1].Value}RowMajor_{match.Groups[2].Value}>";
        });
        return elementTypes.Count == 0
            ? hlsl
            : string.Concat(elementTypes.Select(type => $"typedef row_major {type} RowMajor_{type};\n")) + result;
    }
}

