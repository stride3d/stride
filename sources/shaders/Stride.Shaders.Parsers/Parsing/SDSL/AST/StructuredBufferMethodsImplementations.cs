using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using SpirvStorageClass = Stride.Shaders.Spirv.Specification.StorageClass;

namespace Stride.Shaders.Parsing.SDSL;

/// <summary>
/// Methods of StructuredBuffer and RWStructuredBuffer. The buffer value is the pointer to its struct { T[] }.
/// </summary>
public class StructuredBufferMethodsImplementations : StructuredBufferMethodsDeclarations
{
    public static StructuredBufferMethodsImplementations Instance { get; } = new();

    // GetDimensions(out uint count, out uint stride): the stride is a constant, as with DXC. It is Direct3D's,
    // the layout the buffer has on every API (Vulkan and Direct3D 12 use it when it differs from std430: ShaderMixer.UseTightLayout)
    public override SpirvValue CompileGetDimensions(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue structuredBuffer, SpirvValue count, SpirvValue stride, TextLocation location = default)
    {
        var bufferType = (StructuredBufferType)((PointerType)context.ReverseTypes[structuredBuffer.TypeId]).BaseType;

        var uintType = context.GetOrRegister(ScalarType.UInt);
        var arrayLength = builder.Insert(new OpArrayLength(uintType, context.Bound++, structuredBuffer.Id, 0));
        builder.Insert(new OpStore(count.Id, arrayLength.ResultId, null, []));
        var strideConstant = context.CompileConstant((uint)SpirvBuilder.StorageBufferArrayStride(bufferType.BaseType, TypeModifier.None, SpirvBuilder.AlignmentRules.StructuredBufferTight));
        builder.Insert(new OpStore(stride.Id, strideConstant.Id, null, []));
        return default;
    }

    // Load(int index): the same as buffer[index]
    public override SpirvValue CompileLoad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue structuredBuffer, SpirvValue x, SpirvValue? status = null, TextLocation location = default)
    {
        if (status != null)
            throw new NotImplementedException("Load with status is not supported on a structured buffer");

        var elementType = functionType.ReturnType;
        var const0 = context.CompileConstant((int)0);
        var elementPointerType = context.GetOrRegister(new PointerType(elementType, SpirvStorageClass.StorageBuffer));
        var elementPointer = builder.InsertData(new OpAccessChain(elementPointerType, context.Bound++, structuredBuffer.Id, [const0.Id, x.Id]));
        var loadResult = builder.Insert(new OpLoad(context.GetOrRegister(elementType), context.Bound++, elementPointer.IdResult!.Value, null, []));
        return new(loadResult.ResultId, loadResult.ResultType);
    }
}
