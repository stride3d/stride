using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;


public ref partial struct FunctionBuilder
{

    public readonly Instruction FindVariable(string name)
    {
        if (mixer.LocalVariables.TryGet(name, out var local))
            return local;
        else if (mixer.GlobalVariables.TryGet(name, out var global))
            return global;
        else
            throw new Exception($"Variable {name} was not found");
    }

    public readonly Instruction Constant<T>(T value)
        where T : struct
    {
        return mixer.CreateConstant(value).Instruction;
    }

    public readonly Instruction Load(string name)
    {
        var variable = FindVariable(name);
        var rtype = Instruction.Empty;
        foreach (var i in mixer.Buffer.Declarations.UnorderedInstructions)
        {
            if (i.ResultId != null && i.ResultId == variable.ResultType && i.OpCode != SDSLOp.OpTypePointer)
            {
                rtype = i;
                break;
            }
            else if (i.ResultId != null && i.ResultId == variable.ResultType && i.OpCode == SDSLOp.OpTypePointer)
            {
                var toFind = i.GetOperand<IdRef>("type");
                foreach (var j in mixer.Buffer.Declarations.UnorderedInstructions)
                {
                    if (j.ResultId != null && j.ResultId == toFind && j.OpCode != SDSLOp.OpTypePointer)
                    {
                        rtype = j;
                        break;
                    }
                }
                break;
            }
        }
        if (rtype.IsEmpty)
            throw new Exception("type of variable was not found");

        return mixer.Buffer.AddOpLoad(rtype, variable, null);
    }
    public readonly Instruction FindById(int id)
    {
        foreach (var i in mixer.Buffer.Instructions)
            if (i.ResultId == id)
                return i;
        return Instruction.Empty;
    }

    public readonly Instruction Add(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "byte" or "sbyte"
            or "ushort" or "short"
            or "uint" or "int"
            or "long" or "ulong" => mixer.Buffer.AddOpIAdd(rtype, a, b),
            "half" or "float" or "double" => mixer.Buffer.AddOpFAdd(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }
    public readonly Instruction Sub(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "byte" or "sbyte"
            or "ushort" or "short"
            or "uint" or "int"
            or "long" or "ulong" => mixer.Buffer.AddOpISub(rtype, a, b),
            "half" or "float" or "double" => mixer.Buffer.AddOpFSub(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }
    public readonly Instruction Div(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "sbyte"
            or "short"
            or "int"
            or "long" => mixer.Buffer.AddOpSDiv(rtype, a, b),
            "byte"
            or "ushort"
            or "uint"
            or "ulong" => mixer.Buffer.AddOpUDiv(rtype, a, b),
            "half" or "float" or "double" => mixer.Buffer.AddOpFDiv(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }
    public readonly Instruction Mul(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "byte" or "sbyte"
            or "ushort" or "short"
            or "uint" or "int"
            or "long" or "ulong" => mixer.Buffer.AddOpIMul(rtype, a, b),
            "half" or "float" or "double" => mixer.Buffer.AddOpFMul(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }

    public readonly Instruction Mod(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "sbyte"
            or "short"
            or "int"
            or "long" => mixer.Buffer.AddOpSMod(rtype, a, b),
            "byte"
            or "ushort"
            or "uint"
            or "ulong" => mixer.Buffer.AddOpUMod(rtype, a, b),
            "half" or "float" or "double" => mixer.Buffer.AddOpFMod(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }
    public readonly Instruction Rem(string resultType, IdRef a, IdRef b)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return resultType switch
        {
            "sbyte"
            or "short"
            or "int"
            or "long" => mixer.Buffer.AddOpSRem(rtype, a, b),
            "byte"
            or "ushort"
            or "uint"
            or "ulong" => throw new Exception("Cannot compute remainder of unsigned number"),
            "half" or "float" or "double" => mixer.Buffer.AddOpFRem(rtype, a, b),
            _ => throw new NotImplementedException($"{resultType} not yet implemented for this")
        };
    }
    public readonly Instruction VectorTimesScalar(string resultType, IdRef vector, IdRef scalar)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpVectorTimesScalar(rtype, vector, scalar);
    }
    public readonly Instruction VectorTimesMatrix(string resultType, IdRef vector, IdRef matrix)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpVectorTimesMatrix(rtype, vector, matrix);
    }
    public readonly Instruction MatrixTimesScalar(string resultType, IdRef matrix, IdRef scalar)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpMatrixTimesScalar(rtype, matrix, scalar);
    }
    public readonly Instruction MatrixTimesVector(string resultType, IdRef matrix, IdRef vector)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpMatrixTimesVector(rtype, matrix, vector);
    }
    public readonly Instruction MatrixTimesMatrix(string resultType, IdRef matrix, IdRef matrix2)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpMatrixTimesMatrix(rtype, matrix, matrix2);
    }

    public readonly Instruction OuterProduct(string resultType, IdRef vector1, IdRef vector2)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpOuterProduct(rtype, vector1, vector2);
    }
    public readonly Instruction Dot(string resultType, IdRef vector1, IdRef vector2)
    {
        var rtype = mixer.GetOrCreateBaseType(resultType.AsMemory());
        return mixer.Buffer.AddOpDot(rtype, vector1, vector2);
    }


    public readonly Instruction And(string resultType, IdRef operand1, IdRef operand2)
    {
        return mixer.Buffer.AddOpBitwiseAnd(mixer.GetOrCreateBaseType(resultType.AsMemory()), operand1, operand2);
    }

    public readonly Instruction Or(string resultType, IdRef operand1, IdRef operand2)
    {
        return mixer.Buffer.AddOpBitwiseOr(mixer.GetOrCreateBaseType(resultType.AsMemory()), operand1, operand2);
    }
    public readonly Instruction Xor(string resultType, IdRef operand1, IdRef operand2)
    {
        return mixer.Buffer.AddOpBitwiseXor(mixer.GetOrCreateBaseType(resultType.AsMemory()), operand1, operand2);
    }

    public readonly Instruction VectorShuffle(string resultType, IdRef vector1, IdRef vector2, Span<int> values)
    {
        return mixer.Buffer.AddOpVectorShuffle(mixer.GetOrCreateBaseType(resultType.AsMemory()), vector1, vector2, MemoryMarshal.Cast<int,LiteralInteger>(values));
    }


    public readonly Instruction ShiftRightLogical(string resultType, IdRef baseId, IdRef shift)
    {
        return mixer.Buffer.AddOpShiftRightLogical(mixer.GetOrCreateBaseType(resultType.AsMemory()), baseId, shift);
    }
    public readonly Instruction ShiftRightArithmetic(string resultType, IdRef baseId, IdRef shift)
    {
        return mixer.Buffer.AddOpShiftRightArithmetic(mixer.GetOrCreateBaseType(resultType.AsMemory()), baseId, shift);
    }
    public readonly Instruction ShiftLeft(string resultType, IdRef baseId, IdRef shift)
    {
        return mixer.Buffer.AddOpShiftLeftLogical(mixer.GetOrCreateBaseType(resultType.AsMemory()), baseId, shift);
    }

    public readonly Instruction GreaterThan(string resultType, IdRef value1, IdRef value2)
    {
        return resultType switch
        {

            string f when
                f.StartsWith("half")
                || f.StartsWith("float")
                || f.StartsWith("double")
                => mixer.Buffer.AddOpFOrdGreaterThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("sbyte")
                || f.StartsWith("short")
                || f.StartsWith("int")
                || f.StartsWith("long")
                => mixer.Buffer.AddOpSGreaterThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("byte")
                || f.StartsWith("ushort")
                || f.StartsWith("uint")
                || f.StartsWith("ulong")
                => mixer.Buffer.AddOpUGreaterThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            _ => throw new NotImplementedException()
        };
    }
    public readonly Instruction LessThan(string resultType, IdRef value1, IdRef value2)
    {
        return resultType switch
        {

            string f when
                f.StartsWith("half")
                || f.StartsWith("float")
                || f.StartsWith("double")
                => mixer.Buffer.AddOpFOrdLessThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("sbyte")
                || f.StartsWith("short")
                || f.StartsWith("int")
                || f.StartsWith("long")
                => mixer.Buffer.AddOpSLessThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("byte")
                || f.StartsWith("ushort")
                || f.StartsWith("uint")
                || f.StartsWith("ulong")
                => mixer.Buffer.AddOpULessThan(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            _ => throw new NotImplementedException()
        };
    }
    public readonly Instruction GreaterThanEqual(string resultType, IdRef value1, IdRef value2)
    {
        return resultType switch
        {

            string f when
                f.StartsWith("half")
                || f.StartsWith("float")
                || f.StartsWith("double")
                => mixer.Buffer.AddOpFOrdGreaterThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("sbyte")
                || f.StartsWith("short")
                || f.StartsWith("int")
                || f.StartsWith("long")
                => mixer.Buffer.AddOpSGreaterThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("byte")
                || f.StartsWith("ushort")
                || f.StartsWith("uint")
                || f.StartsWith("ulong")
                => mixer.Buffer.AddOpUGreaterThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            _ => throw new NotImplementedException()
        };
    }
    public readonly Instruction LessThanEqual(string resultType, IdRef value1, IdRef value2)
    {
        return resultType switch
        {

            string f when
                f.StartsWith("half")
                || f.StartsWith("float")
                || f.StartsWith("double")
                => mixer.Buffer.AddOpFOrdLessThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("sbyte")
                || f.StartsWith("short")
                || f.StartsWith("int")
                || f.StartsWith("long")
                => mixer.Buffer.AddOpSLessThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            string f when
                f.StartsWith("byte")
                || f.StartsWith("ushort")
                || f.StartsWith("uint")
                || f.StartsWith("ulong")
                => mixer.Buffer.AddOpULessThanEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2),
            _ => throw new NotImplementedException()
        };
    }

    public readonly Instruction LogicalEqual(string resultType, IdRef value1, IdRef value2)
    {
        return mixer.Buffer.AddOpLogicalEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2);
    }
    public readonly Instruction LogicalNotEqual(string resultType, IdRef value1, IdRef value2)
    {
        return mixer.Buffer.AddOpLogicalNotEqual(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2);
    }
    public readonly Instruction LogicalAnd(string resultType, IdRef value1, IdRef value2)
    {
        return mixer.Buffer.AddOpLogicalAnd(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2);
    }
    public readonly Instruction LogicalOr(string resultType, IdRef value1, IdRef value2)
    {
        return mixer.Buffer.AddOpLogicalAnd(mixer.GetOrCreateBaseType(resultType.AsMemory()), value1, value2);
    }
    public readonly Instruction LogicalNot(string resultType, IdRef value)
    {
        return mixer.Buffer.AddOpLogicalNot(mixer.GetOrCreateBaseType(resultType.AsMemory()), value);
    }

}