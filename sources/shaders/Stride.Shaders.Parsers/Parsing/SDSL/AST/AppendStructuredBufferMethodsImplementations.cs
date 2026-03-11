using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Parsing.Analysis;
using SpirvStorageClass = Stride.Shaders.Spirv.Specification.StorageClass;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL;

public class AppendStructuredBufferMethodsImplementations : AppendStructuredBufferMethodsDeclarations
{
    public static AppendStructuredBufferMethodsImplementations Instance { get; } = new();

    public override SpirvValue CompileAppend(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue appendStructuredBuffer, SpirvValue value)
    {
        // Buffer struct is { T[] }, member 0 is the runtime array
        // We write to element 0 as a placeholder (no atomic counter implemented)
        var const0 = context.CompileConstant((int)0);
        var baseType = functionType.ParameterTypes[0].Type;
        var ptrTType = context.GetOrRegister(new PointerType(baseType, SpirvStorageClass.StorageBuffer));
        var ptrToData = builder.InsertData(new OpAccessChain(ptrTType, context.Bound++, appendStructuredBuffer.Id, [const0.Id, const0.Id]));
        builder.Insert(new OpStore(ptrToData.IdResult!.Value, value.Id, null, []));
        return default;
    }

    public override SpirvValue CompileGetDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue appendStructuredBuffer, SpirvValue count, SpirvValue stride)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var arrayLen = builder.Insert(new OpArrayLength(uintType, context.Bound++, appendStructuredBuffer.Id, 0));
        builder.Insert(new OpStore(count.Id, arrayLen.ResultId, null, []));
        var baseType = functionType.ParameterTypes[0].Type;
        var elementSize = SpirvBuilder.TypeSizeInBuffer(baseType, TypeModifier.None, SpirvBuilder.AlignmentRules.StructuredBuffer).Size;
        var strideConst = context.CompileConstant((uint)elementSize);
        builder.Insert(new OpStore(stride.Id, strideConst.Id, null, []));
        return default;
    }
}

public class ConsumeStructuredBufferMethodsImplementations : ConsumeStructuredBufferMethodsDeclarations
{
    public static ConsumeStructuredBufferMethodsImplementations Instance { get; } = new();

    public override SpirvValue CompileConsume(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue consumeStructuredBuffer)
    {
        // Read from element 0 as a placeholder (no atomic counter implemented)
        var const0 = context.CompileConstant((int)0);
        var returnType = functionType.ReturnType;
        var ptrTType = context.GetOrRegister(new PointerType(returnType, SpirvStorageClass.StorageBuffer));
        var ptrToData = builder.InsertData(new OpAccessChain(ptrTType, context.Bound++, consumeStructuredBuffer.Id, [const0.Id, const0.Id]));
        var loadResult = builder.Insert(new OpLoad(context.GetOrRegister(returnType), context.Bound++, ptrToData.IdResult!.Value, null, []));
        return new(loadResult.ResultId, loadResult.ResultType);
    }

    public override SpirvValue CompileGetDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue consumeStructuredBuffer, SpirvValue count, SpirvValue stride)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var arrayLen = builder.Insert(new OpArrayLength(uintType, context.Bound++, consumeStructuredBuffer.Id, 0));
        builder.Insert(new OpStore(count.Id, arrayLen.ResultId, null, []));
        return default;
    }
}
