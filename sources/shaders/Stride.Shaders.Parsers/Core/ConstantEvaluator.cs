// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Stride.Shaders.Spirv.Building;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Core;

/// <summary>
/// Evaluates the operations a constant can be built from (the inner operations of OpSpecConstantOp) on known values.
/// </summary>
/// <remarks>
/// A value is a boxed CLR scalar matching its SPIR-V type: bool, int, uint, long, ulong, float or double.
/// A vector is a <see cref="ConstantVector"/> of those, on which an operation applies per component.
/// An operation that is not supported, or not defined for its operands (e.g. a division by zero), is not evaluated.
/// Note: the switch expressions cast their first case to object, otherwise the cases are converted to a common type.
/// </remarks>
public static class ConstantEvaluator
{
    public static bool TryEvaluateUnary(Op op, object operand, SymbolType resultType, [NotNullWhen(true)] out object? result)
    {
        // A vector is evaluated per component
        if (operand is ConstantVector vector)
        {
            result = null;
            if (resultType is not VectorType vectorResultType || vectorResultType.Size != vector.Values.Length)
                return false;

            var values = new object[vector.Values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                if (vector.Values[i] is ConstantVector || !TryEvaluateUnary(op, vector.Values[i], vectorResultType.BaseType, out values[i]!))
                    return false;
            }

            result = new ConstantVector { Values = values };
            return true;
        }

        result = (op, operand) switch
        {
            (Op.OpSNegate or Op.OpNot, int v) => EvaluateIntegerUnary(op, v),
            (Op.OpSNegate or Op.OpNot, uint v) => EvaluateIntegerUnary(op, v),
            (Op.OpSNegate or Op.OpNot, long v) => EvaluateIntegerUnary(op, v),
            (Op.OpSNegate or Op.OpNot, ulong v) => EvaluateIntegerUnary(op, v),
            (Op.OpFNegate, float v) => -v,
            (Op.OpFNegate, double v) => -v,
            (Op.OpLogicalNot, bool v) => !v,
            _ when resultType is ScalarType scalarResultType => EvaluateConversion(op, operand, scalarResultType),
            _ => null,
        };
        return result is not null;
    }

    public static bool TryEvaluateBinary(Op op, object left, object right, [NotNullWhen(true)] out object? result)
    {
        // Vectors are evaluated per component
        if (left is ConstantVector leftVector)
        {
            result = null;
            if (right is not ConstantVector rightVector || rightVector.Values.Length != leftVector.Values.Length)
                return false;

            var values = new object[leftVector.Values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                if (leftVector.Values[i] is ConstantVector || !TryEvaluateBinary(op, leftVector.Values[i], rightVector.Values[i], out values[i]!))
                    return false;
            }

            result = new ConstantVector { Values = values };
            return true;
        }

        if (op is Op.OpShiftLeftLogical or Op.OpShiftRightLogical or Op.OpShiftRightArithmetic)
        {
            // The shift amount does not need to have the type of the shifted value
            result = TryGetInteger(right, out var shift, out _)
                ? left switch
                {
                    int l => EvaluateShift(op, l, (int)shift, signed: true),
                    uint l => EvaluateShift(op, l, (int)shift, signed: false),
                    long l => EvaluateShift(op, l, (int)shift, signed: true),
                    ulong l => EvaluateShift(op, l, (int)shift, signed: false),
                    _ => null,
                }
                : null;
        }
        else
        {
            result = (left, right) switch
            {
                (int l, int r) => EvaluateIntegerBinary(op, l, r, signed: true),
                (uint l, uint r) => EvaluateIntegerBinary(op, l, r, signed: false),
                (long l, long r) => EvaluateIntegerBinary(op, l, r, signed: true),
                (ulong l, ulong r) => EvaluateIntegerBinary(op, l, r, signed: false),
                (float l, float r) => EvaluateFloatBinary(op, l, r),
                (double l, double r) => EvaluateFloatBinary(op, l, r),
                (bool l, bool r) => EvaluateLogicalBinary(op, l, r),
                _ => null,
            };
        }

        return result is not null;
    }

    /// <summary>
    /// Number of operands of an operation that can be evaluated, all of them ids: 1 for <see cref="TryEvaluateUnary"/>,
    /// 2 for <see cref="TryEvaluateBinary"/> and 3 for OpSelect, which only needs the operand its condition selects.
    /// 0 for any other operation, whose operands might not be ids (e.g. the literal indices of OpCompositeExtract).
    /// </summary>
    public static int GetEvaluatedOperandCount(Op op)
    {
        return op switch
        {
            Op.OpSNegate or Op.OpNot or Op.OpFNegate or Op.OpLogicalNot
                or Op.OpConvertFToS or Op.OpConvertFToU or Op.OpConvertSToF or Op.OpConvertUToF
                or Op.OpSConvert or Op.OpUConvert or Op.OpFConvert or Op.OpBitcast => 1,
            Op.OpIAdd or Op.OpISub or Op.OpIMul or Op.OpUDiv or Op.OpSDiv or Op.OpUMod or Op.OpSRem or Op.OpSMod
                or Op.OpShiftRightLogical or Op.OpShiftRightArithmetic or Op.OpShiftLeftLogical
                or Op.OpBitwiseOr or Op.OpBitwiseXor or Op.OpBitwiseAnd
                or Op.OpFAdd or Op.OpFSub or Op.OpFMul or Op.OpFDiv or Op.OpFRem or Op.OpFMod
                or Op.OpLogicalOr or Op.OpLogicalAnd or Op.OpLogicalEqual or Op.OpLogicalNotEqual
                or Op.OpIEqual or Op.OpINotEqual
                or Op.OpULessThan or Op.OpSLessThan or Op.OpUGreaterThan or Op.OpSGreaterThan
                or Op.OpULessThanEqual or Op.OpSLessThanEqual or Op.OpUGreaterThanEqual or Op.OpSGreaterThanEqual => 2,
            Op.OpSelect => 3,
            _ => 0,
        };
    }

    /// <summary>
    /// Number of leading operands that are ids, for any operation that can be part of a constant: the ones of
    /// <see cref="GetEvaluatedOperandCount"/>, and the operations on composites, whose other operands are literal indices.
    /// 0 for any other operation.
    /// </summary>
    public static int GetIdOperandCount(Op op)
    {
        return op switch
        {
            Op.OpCompositeExtract => 1,
            Op.OpCompositeInsert or Op.OpVectorShuffle => 2,
            _ => GetEvaluatedOperandCount(op),
        };
    }

    private static object? EvaluateIntegerUnary<T>(Op op, T value) where T : IBinaryInteger<T>
    {
        return op switch
        {
            Op.OpSNegate => (object)(T.Zero - value),
            Op.OpNot => ~value,
            _ => null,
        };
    }

    private static object? EvaluateShift<T>(Op op, T value, int shift, bool signed) where T : IBinaryInteger<T>
    {
        // Not defined in SPIR-V (C# would only keep the low bits of the shift amount)
        if (shift < 0 || shift >= value.GetByteCount() * 8)
            return null;

        return op switch
        {
            Op.OpShiftLeftLogical => (object)(value << shift),
            Op.OpShiftRightLogical => value >>> shift,
            // On an unsigned type, SPIR-V still extends the sign bit where C# does not
            Op.OpShiftRightArithmetic when signed => value >> shift,
            _ => null,
        };
    }

    private static object? EvaluateIntegerBinary<T>(Op op, T left, T right, bool signed) where T : IBinaryInteger<T>, IMinMaxValue<T>
    {
        // Not defined in SPIR-V: a division by zero, and the one signed division that overflows (which throws in C#)
        if (op is Op.OpSDiv or Op.OpSRem or Op.OpSMod or Op.OpUDiv or Op.OpUMod
            && (right == T.Zero || (signed && left == T.MinValue && right == T.Zero - T.One)))
            return null;

        return op switch
        {
            Op.OpIAdd => (object)(left + right),
            Op.OpISub => left - right,
            Op.OpIMul => left * right,
            Op.OpBitwiseAnd => left & right,
            Op.OpBitwiseOr => left | right,
            Op.OpBitwiseXor => left ^ right,
            Op.OpIEqual => left == right,
            Op.OpINotEqual => left != right,
            Op.OpSDiv when signed => left / right,
            Op.OpSRem when signed => left % right,
            // Remainder with the sign of the divisor
            Op.OpSMod when signed => (left % right + right) % right,
            Op.OpSLessThan when signed => left < right,
            Op.OpSGreaterThan when signed => left > right,
            Op.OpSLessThanEqual when signed => left <= right,
            Op.OpSGreaterThanEqual when signed => left >= right,
            Op.OpUDiv when !signed => left / right,
            Op.OpUMod when !signed => left % right,
            Op.OpULessThan when !signed => left < right,
            Op.OpUGreaterThan when !signed => left > right,
            Op.OpULessThanEqual when !signed => left <= right,
            Op.OpUGreaterThanEqual when !signed => left >= right,
            _ => null,
        };
    }

    private static object? EvaluateFloatBinary<T>(Op op, T left, T right) where T : IFloatingPointIeee754<T>
    {
        return op switch
        {
            Op.OpFAdd => (object)(left + right),
            Op.OpFSub => left - right,
            Op.OpFMul => left * right,
            Op.OpFDiv => left / right,
            Op.OpFRem => left % right,
            // Remainder with the sign of the divisor
            Op.OpFMod => (left % right + right) % right,
            _ => null,
        };
    }

    private static object? EvaluateLogicalBinary(Op op, bool left, bool right)
    {
        return op switch
        {
            Op.OpLogicalAnd => (object)(left && right),
            Op.OpLogicalOr => left || right,
            Op.OpLogicalEqual => left == right,
            Op.OpLogicalNotEqual => left != right,
            _ => null,
        };
    }

    private static object? EvaluateConversion(Op op, object operand, ScalarType resultType)
    {
        switch (op)
        {
            case Op.OpConvertFToS or Op.OpConvertFToU or Op.OpFConvert when operand is float or double:
            {
                var value = Convert.ToDouble(operand);
                return (op, resultType.Type) switch
                {
                    (Op.OpConvertFToS, Scalar.Int) => (object)(int)value,
                    (Op.OpConvertFToS, Scalar.Int64) => (long)value,
                    (Op.OpConvertFToU, Scalar.UInt) => (uint)value,
                    (Op.OpConvertFToU, Scalar.UInt64) => (ulong)value,
                    (Op.OpFConvert, Scalar.Float) => (float)value,
                    (Op.OpFConvert, Scalar.Double) => value,
                    _ => null,
                };
            }
            // The operand is read as signed or unsigned depending on the operation, whatever its type
            case Op.OpConvertSToF or Op.OpConvertUToF when TryGetInteger(operand, out var signExtended, out var zeroExtended):
            {
                var value = op == Op.OpConvertSToF ? (double)signExtended : (double)zeroExtended;
                return resultType.Type switch
                {
                    Scalar.Float => (object)(float)value,
                    Scalar.Double => value,
                    _ => null,
                };
            }
            case Op.OpSConvert when TryGetInteger(operand, out var signExtended, out _):
                return FromBits(resultType, unchecked((ulong)signExtended));
            case Op.OpUConvert when TryGetInteger(operand, out _, out var zeroExtended):
                return FromBits(resultType, zeroExtended);
            case Op.OpBitcast when TryGetBits(operand, out var bits, out var width) && width == GetWidth(resultType):
                return FromBits(resultType, bits);
            default:
                return null;
        }
    }

    private static bool TryGetInteger(object value, out long signExtended, out ulong zeroExtended)
    {
        (signExtended, zeroExtended) = value switch
        {
            int v => ((long)v, (ulong)(uint)v),
            uint v => ((long)(int)v, (ulong)v),
            long v => (v, (ulong)v),
            ulong v => ((long)v, v),
            _ => (0L, 0UL),
        };
        return value is int or uint or long or ulong;
    }

    private static bool TryGetBits(object value, out ulong bits, out int width)
    {
        (bits, width) = value switch
        {
            int v => ((ulong)(uint)v, 32),
            uint v => ((ulong)v, 32),
            float v => ((ulong)BitConverter.SingleToUInt32Bits(v), 32),
            long v => ((ulong)v, 64),
            ulong v => (v, 64),
            double v => (BitConverter.DoubleToUInt64Bits(v), 64),
            _ => (0UL, 0),
        };
        return width != 0;
    }

    private static int GetWidth(ScalarType type)
    {
        return type.Type switch
        {
            Scalar.Int or Scalar.UInt or Scalar.Float => 32,
            Scalar.Int64 or Scalar.UInt64 or Scalar.Double => 64,
            _ => 0,
        };
    }

    // Keeps the low bits of the value that fit the type
    private static object? FromBits(ScalarType type, ulong bits)
    {
        return type.Type switch
        {
            Scalar.Int => (object)(int)bits,
            Scalar.UInt => (uint)bits,
            Scalar.Int64 => (long)bits,
            Scalar.UInt64 => bits,
            Scalar.Float => BitConverter.UInt32BitsToSingle((uint)bits),
            Scalar.Double => BitConverter.UInt64BitsToDouble(bits),
            _ => null,
        };
    }
}
