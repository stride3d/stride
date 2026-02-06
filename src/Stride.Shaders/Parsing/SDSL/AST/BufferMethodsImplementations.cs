using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;

public class BufferMethodsImplementations : BufferMethodsDeclarations
{
    public static BufferMethodsImplementations Instance { get; } = new();

    public override SpirvValue CompileLoad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue buffer, SpirvValue x, SpirvValue? status = null)
    {
        var loadResult = builder.Insert(new OpImageRead(context.GetOrRegister(functionType.ReturnType), context.Bound++, buffer.Id, x.Id, null, []));
        return new(loadResult.ResultId, loadResult.ResultType);
    }
}