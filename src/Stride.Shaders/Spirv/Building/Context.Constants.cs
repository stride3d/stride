using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
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
            uint v => Buffer.Add(new OpConstant<uint>(GetOrRegister(ScalarType.UInt), Bound++, v)),
            int v => Buffer.Add(new OpConstant<int>(GetOrRegister(ScalarType.Int), Bound++, v)),
            ulong v => Buffer.Add(new OpConstant<ulong>(GetOrRegister(ScalarType.UInt64), Bound++, v)),
            long v => Buffer.Add(new OpConstant<long>(GetOrRegister(ScalarType.Int64), Bound++, v)),
            //Half v => Buffer.Add(new OpConstant<Half>(GetOrRegister(ScalarType.From("half")), Bound++, v)),
            float v => Buffer.Add(new OpConstant<float>(GetOrRegister(ScalarType.Float), Bound++, v)),
            double v => Buffer.Add(new OpConstant<double>(GetOrRegister(ScalarType.Double), Bound++, v)),
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

    public bool TryGetConstantValue(int constantId, [MaybeNullWhen(false)] out object value, out int typeId, bool simplifyInBuffer = false)
    {
        if (Buffer.TryGetInstructionById(constantId, out var constant))
        {
            return TryGetConstantValue(constant, out value, out typeId, simplifyInBuffer);
        }

        typeId = 0;
        value = null;
        return false;
    }

    public object ResolveConstantValue(OpDataIndex i)
    {
        if (!TryGetConstantValue(i, out var value, out _, false))
            throw new InvalidOperationException($"Can't process constant {i.Data.IdResult}");

        return value;
    }

    // Note: this will return false if constant can't be resolved yet (i.e. due to unresolved generics). If it is not meant to become a constant (even later), behavior is undefined.
    public bool TryGetConstantValue(OpDataIndex i, [MaybeNullWhen(false)] out object value, out int typeId, bool simplifyInBuffer = false)
    {
        typeId = 0;
        value = null;

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
                // Conversions
                case Specification.Op.OpConvertFToS:
                case Specification.Op.OpConvertFToU:
                case Specification.Op.OpConvertSToF:
                case Specification.Op.OpConvertUToF:
                    if (!TryGetConstantValue(i.Data.Memory.Span[4], out var convertOperand, out var convertOperandTypeId))
                        return false;
                    value = op switch
                    {
                        // Note: first cast to object is important, otherwise int/float will be cast as float
                        Specification.Op.OpConvertFToS => (object)(int)(float)convertOperand,
                        Specification.Op.OpConvertFToU => (uint)(float)convertOperand,
                        Specification.Op.OpConvertSToF => (float)(int)convertOperand,
                        Specification.Op.OpConvertUToF => (float)(uint)convertOperand,
                    };
                    break;
                // Unary operations
                case Specification.Op.OpSNegate:
                case Specification.Op.OpFNegate:
                    if (!TryGetConstantValue(i.Data.Memory.Span[4], out var unaryOperand, out var unaryOperandTypeId))
                        return false;
                    if (unaryOperandTypeId != resultType)
                        return false;
                    value = op switch
                    {
                        // Note: first cast to object is important, otherwise int/float will be cast as float
                        Specification.Op.OpSNegate => (object)(-(int)unaryOperand),
                        Specification.Op.OpFNegate => -(float)unaryOperand,
                    };
                    break;
                // Binary operations
                case Specification.Op.OpIAdd:
                case Specification.Op.OpISub:
                case Specification.Op.OpIMul:
                case Specification.Op.OpFAdd:
                case Specification.Op.OpFSub:
                case Specification.Op.OpFMul:
                case Specification.Op.OpFDiv:
                    if (!TryGetConstantValue(i.Data.Memory.Span[4], out var left, out var leftTypeId))
                        return false;
                    if (!TryGetConstantValue(i.Data.Memory.Span[5], out var right, out var rightTypeId))
                        return false;
                    if (leftTypeId != resultType || rightTypeId != resultType)
                        return false;
                    value = op switch
                    {
                        // Note: first cast to object is important, otherwise int/float will be cast as float
                        Specification.Op.OpIAdd => (object)((int)left + (int)right),
                        Specification.Op.OpISub => (int)left - (int)right,
                        Specification.Op.OpIMul => (int)left * (int)right,
                        Specification.Op.OpFAdd => (float)left + (float)right,
                        Specification.Op.OpFSub => (float)left - (float)right,
                        Specification.Op.OpFMul => (float)left * (float)right,
                        Specification.Op.OpFDiv => (float)left / (float)right,
                    };
                    break;
                default:
                    throw new NotImplementedException();
            }
            
            if (simplifyInBuffer)
            {
                if (value is int valueI)
                    Buffer.Replace(i.Index, new OpConstant<int>(resultType, resultId, valueI));
                else if (value is float valueF)
                    Buffer.Replace(i.Index, new OpConstant<float>(resultType, resultId, valueF));
                else
                    throw new NotImplementedException();
            }

            return true;
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

    public unsafe SpirvValue CreateConstantCompositeRepeat(Literal literal, int size)
    {
        var value = CompileConstantLiteral(literal);
        if (size == 1)
            return value;

        Span<int> values = stackalloc int[size];
        for (int i = 0; i < size; ++i)
            values[i] = size;
        
        var type = new VectorType((ScalarType)ReverseTypes[value.TypeId], size);
        var instruction = Buffer.Add(new OpConstantComposite(GetOrRegister(type), Bound++, new(values)));

        return new(instruction);
    }

    public Literal CreateLiteral(object value, TextLocation location = default)
    {
        return value switch
        {
            bool i => new BoolLiteral(i, location),
            sbyte i => new IntegerLiteral(new(8, false, true), i, location),
            byte i => new IntegerLiteral(new(8, false, false), i, location),
            short i => new IntegerLiteral(new(16, false, true), i, location),
            ushort i => new IntegerLiteral(new(16, false, false), i, location),
            int i => new IntegerLiteral(new(32, false, true), i, location),
            uint i => new IntegerLiteral(new(32, false, false), i, location),
            long i => new IntegerLiteral(new(64, false, true), i, location),
            ulong i => new IntegerLiteral(new(64, false, false), (long)i, location),
            float i => new FloatLiteral(new(32, true, true), i, null, location),
            double i => new FloatLiteral(new(64, true, true), i, null, location),
        };
    }

    public SpirvValue CompileConstant(object value, TextLocation location = default)
    {
        return CompileConstantLiteral(CreateLiteral(value, location));
    }

    public SpirvValue CompileConstantLiteral(Literal literal)
    {
        object literalValue = literal switch
        {
            BoolLiteral lit => lit.Value,
            IntegerLiteral lit => lit.Suffix.Size switch
            {
                > 32 => lit.LongValue,
                _ => lit.IntValue,
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => lit.DoubleValue,
                _ => (float)lit.DoubleValue,
            },
        };

        if (literal.Type == null)
        {
            literal.Type = literal switch
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
            };
        }

        if (LiteralConstants.TryGetValue((literal.Type, literalValue), out var result))
            return result;

        var instruction = literal switch
        {
            BoolLiteral { Value: true } lit => Buffer.Add(new OpConstantTrue(GetOrRegister(lit.Type), Bound++)),
            BoolLiteral { Value: false } lit => Buffer.Add(new OpConstantFalse(GetOrRegister(lit.Type), Bound++)),
            IntegerLiteral lit => lit.Suffix switch
            {
                { Size: <= 8, Signed: false } => Buffer.Add(new OpConstant<byte>(GetOrRegister(lit.Type), Bound++, (byte)lit.IntValue)),
                { Size: <= 8, Signed: true } => Buffer.Add(new OpConstant<sbyte>(GetOrRegister(lit.Type), Bound++, (sbyte)lit.IntValue)),
                { Size: <= 16, Signed: false } => Buffer.Add(new OpConstant<ushort>(GetOrRegister(lit.Type), Bound++, (ushort)lit.IntValue)),
                { Size: <= 16, Signed: true } => Buffer.Add(new OpConstant<short>(GetOrRegister(lit.Type), Bound++, (short)lit.IntValue)),
                { Size: <= 32, Signed: false } => Buffer.Add(new OpConstant<uint>(GetOrRegister(lit.Type), Bound++, unchecked((uint)lit.IntValue))),
                { Size: <= 32, Signed: true } => Buffer.Add(new OpConstant<int>(GetOrRegister(lit.Type), Bound++, lit.IntValue)),
                { Size: <= 64, Signed: false } => Buffer.Add(new OpConstant<ulong>(GetOrRegister(lit.Type), Bound++, unchecked((uint)lit.LongValue))),
                { Size: <= 64, Signed: true } => Buffer.Add(new OpConstant<long>(GetOrRegister(lit.Type), Bound++, lit.LongValue)),
                _ => throw new NotImplementedException()
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => Buffer.Add(new OpConstant<double>(GetOrRegister(lit.Type), Bound++, lit.DoubleValue)),
                _ => Buffer.Add(new OpConstant<float>(GetOrRegister(lit.Type), Bound++, (float)lit.DoubleValue)),
            },
            _ => throw new NotImplementedException()
        };

        result = new(instruction);
        LiteralConstants.Add((literal.Type, literalValue), result);
        AddName(result.Id, literal switch
        {
            BoolLiteral lit => $"{lit.Type}_{lit.Value}",
            IntegerLiteral lit => $"{lit.Type}_{lit.Value}",
            FloatLiteral lit => $"{lit.Type}_{lit.Value}",
            _ => throw new NotImplementedException()
        });
        return result;
    }
}