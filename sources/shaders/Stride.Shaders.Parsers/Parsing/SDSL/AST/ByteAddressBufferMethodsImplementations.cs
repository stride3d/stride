using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL;

public class ByteAddressBufferMethodsImplementations : ByteAddressBufferMethodsDeclarations
{
    public static ByteAddressBufferMethodsImplementations Instance { get; } = new();

    /// <summary>
    /// Compute the uint element index from a byte offset: index = byteOffset >> 2
    /// </summary>
    private SpirvValue ComputeElementIndex(SpirvContext context, SpirvBuilder builder, SpirvValue byteOffset)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var const2 = context.CompileConstant((uint)2);
        return new(builder.InsertData(new OpShiftRightLogical(uintType, context.Bound++, byteOffset.Id, const2.Id)));
    }

    /// <summary>
    /// Get a pointer to a uint element at a given byte offset in the buffer.
    /// The buffer is a pointer to a struct { uint[] }, so we access chain: buffer -> member 0 (runtime array) -> element index.
    /// </summary>
    private SpirvValue AccessChainAtByteOffset(SpirvContext context, SpirvBuilder builder, SpirvValue bufferPtr, SpirvValue byteOffset)
    {
        var index = ComputeElementIndex(context, builder, byteOffset);
        var ptrUintType = context.GetOrRegister(new PointerType(ScalarType.UInt, StorageClass.StorageBuffer));
        var const0 = context.CompileConstant((int)0);
        return new(builder.InsertData(new OpAccessChain(ptrUintType, context.Bound++, bufferPtr.Id, [const0.Id, index.Id])));
    }

    /// <summary>
    /// Get a pointer to a uint element at a given byte offset + additional uint offset.
    /// </summary>
    private SpirvValue AccessChainAtByteOffsetPlus(SpirvContext context, SpirvBuilder builder, SpirvValue bufferPtr, SpirvValue byteOffset, int extraUintOffset)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var index = ComputeElementIndex(context, builder, byteOffset);
        if (extraUintOffset > 0)
        {
            var offset = context.CompileConstant((uint)extraUintOffset);
            index = new(builder.InsertData(new OpIAdd(uintType, context.Bound++, index.Id, offset.Id)));
        }
        var ptrUintType = context.GetOrRegister(new PointerType(ScalarType.UInt, StorageClass.StorageBuffer));
        var const0 = context.CompileConstant((int)0);
        return new(builder.InsertData(new OpAccessChain(ptrUintType, context.Bound++, bufferPtr.Id, [const0.Id, index.Id])));
    }

    // Load(uint byteOffset) -> uint
    public override SpirvValue CompileLoad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException("Load with status is not yet supported for ByteAddressBuffer");

        var ptr = AccessChainAtByteOffset(context, builder, byteAddressBuffer, byteOffset);
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var loadResult = builder.Insert(new OpLoad(uintType, context.Bound++, ptr.Id, null, []));

        // If the return type is different from uint (e.g., $funcT resolved to something else), bitcast
        var returnType = functionType.ReturnType;
        if (returnType != ScalarType.UInt)
        {
            var result = builder.Insert(new OpBitcast(context.GetOrRegister(returnType), context.Bound++, loadResult.ResultId));
            return new(result.ResultId, result.ResultType);
        }

        return new(loadResult.ResultId, loadResult.ResultType);
    }

    // Load2(uint byteOffset) -> uint2
    public override SpirvValue CompileLoad2(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException("Load with status is not yet supported for ByteAddressBuffer");

        return LoadN(context, builder, functionType, byteAddressBuffer, byteOffset, 2);
    }

    // Load3(uint byteOffset) -> uint3
    public override SpirvValue CompileLoad3(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException("Load with status is not yet supported for ByteAddressBuffer");

        return LoadN(context, builder, functionType, byteAddressBuffer, byteOffset, 3);
    }

    // Load4(uint byteOffset) -> uint4
    public override SpirvValue CompileLoad4(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue? status = null)
    {
        if (status != null)
            throw new NotImplementedException("Load with status is not yet supported for ByteAddressBuffer");

        return LoadN(context, builder, functionType, byteAddressBuffer, byteOffset, 4);
    }

    private SpirvValue LoadN(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue bufferPtr, SpirvValue byteOffset, int count)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        Span<int> values = stackalloc int[count];
        for (int i = 0; i < count; i++)
        {
            var ptr = AccessChainAtByteOffsetPlus(context, builder, bufferPtr, byteOffset, i);
            values[i] = builder.Insert(new OpLoad(uintType, context.Bound++, ptr.Id, null, [])).ResultId;
        }

        var returnTypeId = context.GetOrRegister(functionType.ReturnType);
        var composite = builder.InsertData(new OpCompositeConstruct(returnTypeId, context.Bound++, [.. values]));
        return new(composite);
    }

    // Store(uint byteOffset, T value)
    public override SpirvValue CompileStore(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value)
    {
        // If the value is not uint, bitcast to uint first
        var valueType = context.ReverseTypes[value.TypeId];
        if (valueType != ScalarType.UInt)
        {
            var uintType = context.GetOrRegister(ScalarType.UInt);
            value = new(builder.InsertData(new OpBitcast(uintType, context.Bound++, value.Id)));
        }

        var ptr = AccessChainAtByteOffset(context, builder, byteAddressBuffer, byteOffset);
        builder.Insert(new OpStore(ptr.Id, value.Id, null, []));
        return default;
    }

    // Store2(uint byteOffset, uint2 value)
    public override SpirvValue CompileStore2(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value)
    {
        StoreN(context, builder, byteAddressBuffer, byteOffset, value, 2);
        return default;
    }

    // Store3(uint byteOffset, uint3 value)
    public override SpirvValue CompileStore3(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value)
    {
        StoreN(context, builder, byteAddressBuffer, byteOffset, value, 3);
        return default;
    }

    // Store4(uint byteOffset, uint4 value)
    public override SpirvValue CompileStore4(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value)
    {
        StoreN(context, builder, byteAddressBuffer, byteOffset, value, 4);
        return default;
    }

    private void StoreN(SpirvContext context, SpirvBuilder builder, SpirvValue bufferPtr, SpirvValue byteOffset, SpirvValue value, int count)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        for (int i = 0; i < count; i++)
        {
            var component = builder.Insert(new OpCompositeExtract(uintType, context.Bound++, value.Id, [i]));
            var ptr = AccessChainAtByteOffsetPlus(context, builder, bufferPtr, byteOffset, i);
            builder.Insert(new OpStore(ptr.Id, component.ResultId, null, []));
        }
    }

    // GetDimensions(out uint width) - returns buffer size in bytes
    public override SpirvValue CompileGetDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue width)
    {
        var uintType = context.GetOrRegister(ScalarType.UInt);
        // OpArrayLength returns number of elements in the runtime array (member 0)
        var arrayLen = builder.Insert(new OpArrayLength(uintType, context.Bound++, byteAddressBuffer.Id, 0));
        // Multiply by 4 to convert from element count to byte count
        var const4 = context.CompileConstant((uint)4);
        var byteSize = builder.Insert(new OpIMul(uintType, context.Bound++, arrayLen.ResultId, const4.Id));
        // Store result to the out parameter
        builder.Insert(new OpStore(width.Id, byteSize.ResultId, null, []));
        return default;
    }

    // InterlockedAdd(uint byteOffset, uint value [, out uint original])
    public override SpirvValue CompileInterlockedAdd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.Add);
    }

    // InterlockedMin(uint byteOffset, uint/int value [, out uint original])
    public override SpirvValue CompileInterlockedMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.UMin);
    }

    // InterlockedMax(uint byteOffset, uint/int value [, out uint original])
    public override SpirvValue CompileInterlockedMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.UMax);
    }

    // InterlockedAnd(uint byteOffset, uint value [, out uint original])
    public override SpirvValue CompileInterlockedAnd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.And);
    }

    // InterlockedOr(uint byteOffset, uint value [, out uint original])
    public override SpirvValue CompileInterlockedOr(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.Or);
    }

    // InterlockedXor(uint byteOffset, uint value [, out uint original])
    public override SpirvValue CompileInterlockedXor(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue? original = null)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.Xor);
    }

    // InterlockedExchange(uint byteOffset, uint value, out uint original)
    public override SpirvValue CompileInterlockedExchange(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue value, SpirvValue original)
    {
        return CompileAtomicOp(context, builder, byteAddressBuffer, byteOffset, value, original, AtomicOp.Exchange);
    }

    // InterlockedCompareStore(uint byteOffset, uint compare, uint value)
    public override SpirvValue CompileInterlockedCompareStore(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue compare, SpirvValue value)
    {
        var ptr = AccessChainAtByteOffset(context, builder, byteAddressBuffer, byteOffset);
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var scopeDevice = context.CompileConstant((uint)Scope.Device);
        var memSemanticsNone = context.CompileConstant((uint)0);
        // OpAtomicCompareExchange: result = (original == compare) ? value : original
        builder.Insert(new OpAtomicCompareExchange(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, memSemanticsNone.Id, value.Id, compare.Id));
        return default;
    }

    // InterlockedCompareExchange(uint byteOffset, uint compare, uint value, out uint original)
    public override SpirvValue CompileInterlockedCompareExchange(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue byteAddressBuffer, SpirvValue byteOffset, SpirvValue compare, SpirvValue value, SpirvValue original)
    {
        var ptr = AccessChainAtByteOffset(context, builder, byteAddressBuffer, byteOffset);
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var scopeDevice = context.CompileConstant((uint)Scope.Device);
        var memSemanticsNone = context.CompileConstant((uint)0);
        var result = builder.Insert(new OpAtomicCompareExchange(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, memSemanticsNone.Id, value.Id, compare.Id));
        builder.Insert(new OpStore(original.Id, result.ResultId, null, []));
        return default;
    }

    private enum AtomicOp { Add, UMin, UMax, And, Or, Xor, Exchange }

    private SpirvValue CompileAtomicOp(SpirvContext context, SpirvBuilder builder, SpirvValue bufferPtr, SpirvValue byteOffset, SpirvValue value, SpirvValue? original, AtomicOp op)
    {
        var ptr = AccessChainAtByteOffset(context, builder, bufferPtr, byteOffset);
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var scopeDevice = context.CompileConstant((uint)Scope.Device);
        var memSemanticsNone = context.CompileConstant((uint)0);

        var resultId = op switch
        {
            AtomicOp.Add => builder.Insert(new OpAtomicIAdd(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.UMin => builder.Insert(new OpAtomicUMin(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.UMax => builder.Insert(new OpAtomicUMax(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.And => builder.Insert(new OpAtomicAnd(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.Or => builder.Insert(new OpAtomicOr(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.Xor => builder.Insert(new OpAtomicXor(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            AtomicOp.Exchange => builder.Insert(new OpAtomicExchange(uintType, context.Bound++, ptr.Id, scopeDevice.Id, memSemanticsNone.Id, value.Id)).ResultId,
            _ => throw new NotImplementedException($"Atomic operation {op} not implemented"),
        };

        if (original != null)
        {
            builder.Insert(new OpStore(original.Value.Id, resultId, null, []));
        }

        return default;
    }
}
