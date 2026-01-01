using System.Numerics;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];

    public int AddConstant<TScalar>(TScalar value)
        where TScalar : INumber<TScalar>
    {
        var data = value switch
        {
            byte v => Buffer.Add(new OpConstant<byte>(GetOrRegister(ScalarType.From("byte")), Bound++, v)),
            sbyte v => Buffer.Add(new OpConstant<sbyte>(GetOrRegister(ScalarType.From("sbyte")), Bound++, v)),
            ushort v => Buffer.Add(new OpConstant<ushort>(GetOrRegister(ScalarType.From("ushort")), Bound++, v)),
            short v => Buffer.Add(new OpConstant<short>(GetOrRegister(ScalarType.From("short")), Bound++, v)),
            uint v => Buffer.Add(new OpConstant<uint>(GetOrRegister(ScalarType.From("uint")), Bound++, v)),
            int v => Buffer.Add(new OpConstant<int>(GetOrRegister(ScalarType.From("int")), Bound++, v)),
            ulong v => Buffer.Add(new OpConstant<ulong>(GetOrRegister(ScalarType.From("ulong")), Bound++, v)),
            long v => Buffer.Add(new OpConstant<long>(GetOrRegister(ScalarType.From("long")), Bound++, v)),
            Half v => Buffer.Add(new OpConstant<Half>(GetOrRegister(ScalarType.From("half")), Bound++, v)),
            float v => Buffer.Add(new OpConstant<float>(GetOrRegister(ScalarType.From("float")), Bound++, v)),
            double v => Buffer.Add(new OpConstant<double>(GetOrRegister(ScalarType.From("bdouble")), Bound++, v)),
            _ => throw new NotImplementedException()
        };
        if (InstructionInfo.GetInfo(data).GetResultIndex(out var index))
            return data.Memory.Span[index + 1];
        throw new Exception("Constant has no result id");
    }

    public object GetConstantValue(int constantId)
    {
        if (Buffer.TryGetInstructionById(constantId, out var constant))
        {
            return ResolveConstantValue(constant);
        }

        throw new Exception("Cannot find constant instruction for id " + constantId);
    }

    public bool TryGetConstantValue(int constantId, out object value, out int typeId, bool simplifyInBuffer = false)
    {
        if (Buffer.TryGetInstructionById(constantId, out var constant))
        {
            return TryGetConstantValue(constant, out value, out typeId, simplifyInBuffer);
        }

        typeId = default;
        value = default;
        return false;
    }

    public object ResolveConstantValue(OpDataIndex i)
    {
        if (!TryGetConstantValue(i, out var value, out _, false))
            throw new InvalidOperationException($"Can't process constant {i.Data.IdResult}");

        return value;
    }

    // Note: this will return false if constant can't be resolved yet (i.e. due to unresolved generics). If it is not meant to become a constant (even later), behavior is undefined.
    public bool TryGetConstantValue(OpDataIndex i, out object value, out int typeId, bool simplifyInBuffer = false)
    {
        typeId = default;
        value = default;

        // Check for unresolved values
        if (i.Op == Specification.Op.OpSDSLGenericParameter || i.Op == Specification.Op.OpSDSLGenericReference)
        {
            return false;
        }

        if (i.Op == Specification.Op.OpConstantStringSDSL)
        {
            var operand2 = i.Data.Get("literalString");
            value = operand2.ToLiteral<string>();
            return true;
        }

        if (i.Op == Specification.Op.OpSpecConstantOp)
        {
            var resultType = i.Data.Memory.Span[1];
            var resultId = i.Data.Memory.Span[2];
            var op = (Specification.Op)i.Data.Memory.Span[3];
            switch (op)
            {
                case Specification.Op.OpIMul:
                    if (!TryGetConstantValue(i.Data.Memory.Span[4], out var left, out var leftTypeId))
                        return false;
                    if (!TryGetConstantValue(i.Data.Memory.Span[5], out var right, out var rightTypeId))
                        return false;
                    if (leftTypeId != resultType || rightTypeId != resultType)
                        return false;
                    value = (int)left * (int)right;
                    if (simplifyInBuffer)
                        Buffer.Replace(i.Index, new OpConstant<int>(resultType, resultId, (int)value));
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        if ((i.Op == Specification.Op.OpConstantComposite || i.Op == Specification.Op.OpSpecConstantComposite) &&
            (OpConstantComposite)i is { } constantComposite)
        {
            var values = constantComposite.Values;
            var constants = new object[values.WordCount];
            for (int j = 0; j < values.WordCount; ++j)
            {
                if (!TryGetConstantValue(values.Elements.Span[j], out constants[j], out _))
                    return false;
            }

            // For now we assume it's a vector type (but we would need to revisit that later if we handle more advanced constants such as matrix or arrays)
            value = new ConstantVector { Values = constants };

            return true;
        }

        typeId = i.Op switch
        {
            Specification.Op.OpConstant or Specification.Op.OpSpecConstant => i.Data.Memory.Span[1],
        };
        var operand = i.Data.Get("value");
        if (Buffer.TryGetInstructionById(typeId, out var typeInst))
        {
            if (typeInst.Op == Specification.Op.OpTypeInt)
            {
                var type = (OpTypeInt)typeInst;
                value = type switch
                {
                    { Width: <= 32, Signedness: 0 } => operand.ToLiteral<uint>(),
                    { Width: <= 32, Signedness: 1 } => operand.ToLiteral<int>(),
                    { Width: 64, Signedness: 0 } => operand.ToLiteral<ulong>(),
                    { Width: 64, Signedness: 1 } => operand.ToLiteral<long>(),
                    _ => throw new NotImplementedException($"Unsupported int width {type.Width}"),
                };
                return true;
            }
            else if (typeInst.Op == Specification.Op.OpTypeFloat)
            {
                var type = new OpTypeFloat(typeInst);
                value = type switch
                {
                    { Width: 16 } => operand.ToLiteral<Half>(),
                    { Width: 32 } => operand.ToLiteral<float>(),
                    { Width: 64 } => operand.ToLiteral<double>(),
                    _ => throw new NotImplementedException($"Unsupported float width {type.Width}"),
                };
                return true;
            }
            else
                throw new NotImplementedException($"Unsupported context dependent number with type {typeInst.Op}");
        }

        throw new Exception("Cannot find type instruction for id " + typeId);
    }
}