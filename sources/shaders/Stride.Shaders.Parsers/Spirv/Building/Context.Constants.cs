using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];

    public object GetConstantValue(int constantId)
    {
        if (Buffer.TryGetInstructionById(constantId, out var constant))
        {
            return ResolveConstantValue(constant);
        }

        throw new Exception("Cannot find constant instruction for id " + constantId);
    }

    public bool TryGetConstantValue(int constantId, [MaybeNullWhen(false)] out object value, out int typeId)
    {
        if (Buffer.TryGetInstructionById(constantId, out var constant))
        {
            return TryGetConstantValue(constant, out value, out typeId);
        }

        typeId = 0;
        value = null;
        return false;
    }

    public object ResolveConstantValue(OpDataIndex i)
    {
        if (!TryGetConstantValue(i, out var value, out _))
            throw new InvalidOperationException($"Can't process constant {i.Data.IdResult}");

        return value;
    }

    // Note: this will return false if constant can't be resolved yet (i.e. due to unresolved generics). If it is not meant to become a constant (even later), behavior is undefined.
    public bool TryGetConstantValue(OpDataIndex i, [MaybeNullWhen(false)] out object value, out int typeId)
    {
        typeId = 0;
        value = null;

        // Check for unresolved values
        if (i.Op == Specification.Op.OpGenericParameterSDSL || i.Op == Specification.Op.OpGenericReferenceSDSL)
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
            // Layout: [header, resultType, resultId, opcode, operands...]
            var span = i.Data.Memory.Span;
            var op = (Specification.Op)span[3];
            typeId = span[1];

            // Component of a constant vector (e.g. `v.x`)
            if (op == Specification.Op.OpCompositeExtract)
            {
                if (span.Length != 6 || !TryGetConstantValue(span[4], out var composite, out _)
                    || composite is not ConstantVector vector || (uint)span[5] >= vector.Values.Length)
                    return false;
                value = vector.Values[span[5]];
                return true;
            }

            // Note: the operation decides how many operands are ids, the length of the instruction does not
            switch (ConstantEvaluator.GetEvaluatedOperandCount(op))
            {
                case 1:
                    return TryGetConstantValue(span[4], out var unaryOperand, out _)
                        && ConstantEvaluator.TryEvaluateUnary(op, unaryOperand, ReverseTypes[typeId], out value);
                case 2:
                    return TryGetConstantValue(span[4], out var left, out _)
                        && TryGetConstantValue(span[5], out var right, out _)
                        && ConstantEvaluator.TryEvaluateBinary(op, left, right, out value);
                case 3:
                    return TryGetConstantValue(span[4], out var condition, out _) && condition is bool conditionValue
                        && TryGetConstantValue(conditionValue ? span[5] : span[6], out value, out _);
                default:
                    return false;
            }
        }

        if ((i.Op == Specification.Op.OpConstantComposite || i.Op == Specification.Op.OpSpecConstantComposite) &&
            (OpConstantComposite)i is { } constantComposite)
        {
            var values = constantComposite.Constituents;
            var constants = new object[values.WordCount];
            for (int j = 0; j < values.WordCount; ++j)
            {
                if (!TryGetConstantValue(values.Elements.Span[j], out constants[j]!, out _))
                    return false;
            }

            value = new ConstantVector { Values = constants };

            return true;
        }

        if (i.Op == Specification.Op.OpConstantTrue)
        {
            value = true;
            return true;
        }
        if (i.Op == Specification.Op.OpConstantFalse)
        {
            value = false;
            return true;
        }

        if (i.Op is not (Specification.Op.OpConstant or Specification.Op.OpSpecConstant))
        {
            value = null;
            return false;
        }
        typeId = i.Data.Memory.Span[1];
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

    /// <summary>
    /// Creates the constant instruction holding a scalar value, as returned by <see cref="TryGetConstantValue(int, out object, out int)"/>.
    /// </summary>
    public static OpData CreateConstantInstruction(int resultType, int resultId, object value)
    {
        return value switch
        {
            true => new OpData(new OpConstantTrue(resultType, resultId).InstructionMemory),
            false => new OpData(new OpConstantFalse(resultType, resultId).InstructionMemory),
            int v => new OpData(new OpConstant<int>(resultType, resultId, v).InstructionMemory),
            uint v => new OpData(new OpConstant<uint>(resultType, resultId, v).InstructionMemory),
            long v => new OpData(new OpConstant<long>(resultType, resultId, v).InstructionMemory),
            ulong v => new OpData(new OpConstant<ulong>(resultType, resultId, v).InstructionMemory),
            Half v => new OpData(new OpConstant<Half>(resultType, resultId, v).InstructionMemory),
            float v => new OpData(new OpConstant<float>(resultType, resultId, v).InstructionMemory),
            double v => new OpData(new OpConstant<double>(resultType, resultId, v).InstructionMemory),
            _ => throw new NotSupportedException($"Cannot create a constant of type {value.GetType()}"),
        };
    }

    public SpirvValue CreateDefaultConstantComposite(SymbolType type)
    {
        // TODO: cache results (either here or even more generally for any composite constant even if non-zero)
        return new(Buffer.AddData(new OpConstantNull(GetOrRegister(type), Bound++)));
    }

    public SpirvValue CreateConstantCompositeVectorRepeat(Literal literal, int size)
    {
        var value = CompileConstantLiteral(literal);
        if (size == 1)
            return value;

        var type = new VectorType((ScalarType)ReverseTypes[value.TypeId], size);
        return CreateConstantCompositeRepeat(type, value, size);
    }

    public unsafe SpirvValue CreateConstantCompositeRepeat(SymbolType type, SpirvValue value, int size)
    {
        Span<int> values = stackalloc int[size];
        for (int i = 0; i < size; ++i)
            values[i] = value.Id;

        return new(Buffer.AddData(new OpConstantComposite(GetOrRegister(type), Bound++, new(values))));
    }

    /// <summary>
    /// Gets the constant of a scalar value (bool, int, uint, long, ulong, half, float or double), which has the type of the value.
    /// </summary>
    public SpirvValue CompileConstant(object value)
    {
        var type = value switch
        {
            bool => ScalarType.Boolean,
            int => ScalarType.Int,
            uint => ScalarType.UInt,
            long => ScalarType.Int64,
            ulong => ScalarType.UInt64,
            Half => ScalarType.Half,
            float => ScalarType.Float,
            double => ScalarType.Double,
            _ => throw new NotSupportedException($"Unsupported constant type: {value.GetType()}"),
        };
        return CompileConstant(type, value);
    }

    public SpirvValue CompileConstantLiteral(Literal literal)
    {
        // Note: first cast to object is important, otherwise all the cases are converted to a common type (a float to a double)
        object literalValue = literal switch
        {
            BoolLiteral lit => lit.Value,
            IntegerLiteral lit => lit.Suffix switch
            {
                { Size: > 32, Signed: false } => (object)lit.ULongValue,
                { Size: > 32, Signed: true } => lit.LongValue,
                { Signed: false } => lit.UIntValue,
                _ => lit.IntValue,
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => (object)lit.DoubleValue,
                _ => (float)lit.DoubleValue,
            },
            _ => throw new NotImplementedException()
        };

        literal.Type ??= ComputeLiteralType(literal);

        return CompileConstant(literal.Type, literalValue);
    }

    /// <summary>
    /// Gets the constant of a scalar value with a given type, shared by all the uses of this value with this type.
    /// </summary>
    public SpirvValue CompileConstant(SymbolType type, object value)
    {
        if (LiteralConstants.TryGetValue((type, value), out var result))
            return result;

        result = new(Buffer.Add(CreateConstantInstruction(GetOrRegister(type), Bound++, value)).Data);
        LiteralConstants.Add((type, value), result);
        AddName(result.Id, $"{type}_{Convert.ToString(value, CultureInfo.InvariantCulture)}");
        return result;
    }

    public static ScalarType ComputeLiteralType(Literal literal)
    {
        return literal switch
        {
            BoolLiteral lit => ScalarType.Boolean,
            IntegerLiteral lit => lit.Suffix switch
            {
                //{ Signed: true, Size: 8 } => ScalarType.SByte,
                //{ Signed: true, Size: 16 } => ScalarType.Short,
                { Signed: true, Size: 32 } => ScalarType.Int,
                { Signed: true, Size: 64 } => ScalarType.Int64,
                //{ Signed: false, Size: 8 } => ScalarType.UByte,
                //{ Signed: false, Size: 16 } => ScalarType.UShort,
                { Signed: false, Size: 32 } => ScalarType.UInt,
                { Signed: false, Size: 64 } => ScalarType.UInt64,
                _ => throw new NotImplementedException("Unsupported integer suffix")
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                //16 => ScalarType.Half,
                32 => ScalarType.Float,
                64 => ScalarType.Double,
                _ => throw new NotImplementedException("Unsupported float")
            },
            _ => throw new NotSupportedException($"Unsupported literal type: {literal.GetType()}"),
        };
    }
}
