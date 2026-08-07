using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;

public class BufferMethodsImplementations : BufferMethodsDeclarations
{
    public static BufferMethodsImplementations Instance { get; } = new();

    public override SpirvValue CompileLoad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue buffer, SpirvValue x, SpirvValue? status = null, TextLocation location = default)
    {
        var bufferType = (BufferType)context.ReverseTypes[buffer.TypeId];

        // SPIR-V requires OpImageRead/OpImageFetch result types to be a 4-component vector
        var (vec4TypeId, needsExtract) = TextureMethodsImplementations.GetImageSampleResultType(context, functionType);

        int loadResultId;
        if (bufferType.WriteAllowed)
            loadResultId = builder.Insert(new OpImageRead(vec4TypeId, context.Bound++, buffer.Id, x.Id, null, [])).ResultId;
        else
            loadResultId = builder.Insert(new OpImageFetch(vec4TypeId, context.Bound++, buffer.Id, x.Id, null, [])).ResultId;

        if (needsExtract)
            return TextureMethodsImplementations.ExtractFromVec4(context, builder, functionType, loadResultId);
        return new(loadResultId, vec4TypeId);
    }

    public override SpirvValue CompileGetDimensions(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue buffer, SpirvValue width, TextLocation location = default)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var sizeResult = builder.Insert(new OpImageQuerySize(uintType, context.Bound++, buffer.Id));
        builder.Insert(new OpStore(width.Id, sizeResult.ResultId, null, []));
        return default;
    }
}
