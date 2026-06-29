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
        var resultTypeId = context.GetOrRegister(functionType.ReturnType);
        if (bufferType.WriteAllowed)
        {
            var loadResult = builder.Insert(new OpImageRead(resultTypeId, context.Bound++, buffer.Id, x.Id, null, []));
            return new(loadResult.ResultId, loadResult.ResultType);
        }
        else
        {
            var loadResult = builder.Insert(new OpImageFetch(resultTypeId, context.Bound++, buffer.Id, x.Id, null, []));
            return new(loadResult.ResultId, loadResult.ResultType);
        }
    }

    public override SpirvValue CompileGetDimensions(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue buffer, SpirvValue width, TextLocation location = default)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var sizeResult = builder.Insert(new OpImageQuerySize(uintType, context.Bound++, buffer.Id));
        builder.Insert(new OpStore(width.Id, sizeResult.ResultId, null, []));
        return default;
    }
}
