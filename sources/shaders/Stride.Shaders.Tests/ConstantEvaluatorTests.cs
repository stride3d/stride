// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsers.Tests;

public class ConstantEvaluatorTests
{
    public static TheoryData<Op, object, ScalarType, object> UnaryCases => new()
    {
        { Op.OpSNegate, 3, ScalarType.Int, -3 },
        { Op.OpSNegate, 1u, ScalarType.UInt, uint.MaxValue },
        { Op.OpNot, 0u, ScalarType.UInt, uint.MaxValue },
        { Op.OpFNegate, 1.5f, ScalarType.Float, -1.5f },
        { Op.OpFNegate, 1.5, ScalarType.Double, -1.5 },
        { Op.OpLogicalNot, true, ScalarType.Boolean, false },
        // `static const uint X = 1` converts its int literal this way
        { Op.OpBitcast, 1, ScalarType.UInt, 1u },
        { Op.OpBitcast, -1, ScalarType.UInt, uint.MaxValue },
        { Op.OpBitcast, uint.MaxValue, ScalarType.Int, -1 },
        { Op.OpBitcast, 1.0f, ScalarType.UInt, 0x3F800000u },
        { Op.OpBitcast, 0x3F800000u, ScalarType.Float, 1.0f },
        { Op.OpBitcast, -1L, ScalarType.UInt64, ulong.MaxValue },
        { Op.OpConvertFToS, -2.5f, ScalarType.Int, -2 },
        { Op.OpConvertFToU, 2.5f, ScalarType.UInt, 2u },
        { Op.OpConvertSToF, -2, ScalarType.Float, -2.0f },
        { Op.OpConvertUToF, uint.MaxValue, ScalarType.Double, 4294967295.0 },
        // The operation decides how the operand is read, not its type
        { Op.OpConvertSToF, uint.MaxValue, ScalarType.Float, -1.0f },
        { Op.OpSConvert, -1, ScalarType.Int64, -1L },
        { Op.OpUConvert, -1, ScalarType.UInt64, (ulong)uint.MaxValue },
        { Op.OpUConvert, ulong.MaxValue, ScalarType.UInt, uint.MaxValue },
        { Op.OpFConvert, 1.5f, ScalarType.Double, 1.5 },
    };

    [Theory]
    [MemberData(nameof(UnaryCases))]
    public void EvaluatesUnary(Op op, object operand, ScalarType resultType, object expected)
    {
        Assert.Equal(1, ConstantEvaluator.GetEvaluatedOperandCount(op));
        Assert.True(ConstantEvaluator.TryEvaluateUnary(op, operand, resultType, out var result));
        // Assert.Equal on object compares the type too: a uint result must not come back as an int
        Assert.Equal(expected, result);
    }

    public static TheoryData<Op, object, object, object> BinaryCases => new()
    {
        { Op.OpIAdd, 2, 3, 5 },
        { Op.OpIAdd, 2u, 3u, 5u },
        { Op.OpISub, 2u, 3u, uint.MaxValue },
        { Op.OpIMul, 3L, 4L, 12L },
        { Op.OpSDiv, -7, 2, -3 },
        { Op.OpUDiv, 7u, 2u, 3u },
        { Op.OpSRem, -7, 2, -1 },
        { Op.OpSMod, -7, 2, 1 },
        { Op.OpUMod, 7u, 2u, 1u },
        { Op.OpBitwiseAnd, 0xFFu, 0x0Fu, 0x0Fu },
        { Op.OpBitwiseOr, 0xF0, 0x0F, 0xFF },
        { Op.OpShiftLeftLogical, 1u, 4, 16u },
        { Op.OpShiftRightLogical, -1, 28, 15 },
        { Op.OpShiftRightArithmetic, -16, 2, -4 },
        { Op.OpFAdd, 1.5f, 2.0f, 3.5f },
        { Op.OpFDiv, 1.0, 4.0, 0.25 },
        { Op.OpFMod, -1.0f, 3.0f, 2.0f },
        { Op.OpFRem, -1.0f, 3.0f, -1.0f },
        { Op.OpIEqual, 2u, 2u, true },
        { Op.OpSLessThan, -1, 1, true },
        { Op.OpULessThan, uint.MaxValue, 1u, false },
        { Op.OpLogicalAnd, true, false, false },
    };

    [Theory]
    [MemberData(nameof(BinaryCases))]
    public void EvaluatesBinary(Op op, object left, object right, object expected)
    {
        Assert.Equal(2, ConstantEvaluator.GetEvaluatedOperandCount(op));
        Assert.True(ConstantEvaluator.TryEvaluateBinary(op, left, right, out var result));
        Assert.Equal(expected, result);
    }

    // Their operands are not all ids (literal indices), so a caller must not read them as such
    [Theory]
    [InlineData(Op.OpCompositeExtract)]
    [InlineData(Op.OpCompositeInsert)]
    [InlineData(Op.OpVectorShuffle)]
    [InlineData(Op.OpAccessChain)]
    public void OperationWithLiteralOperandsHasNoOperandCount(Op op)
    {
        Assert.Equal(0, ConstantEvaluator.GetEvaluatedOperandCount(op));
    }

    [Fact]
    public void DoesNotEvaluateWhatIsNotDefined()
    {
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpSDiv, 1, 0, out _));
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpUMod, 1u, 0u, out _));
        // The signed division that overflows, which throws in C#
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpSDiv, int.MinValue, -1, out _));
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpSRem, long.MinValue, -1L, out _));
        // A shift by the width of the value or more, of which C# would only keep the low bits
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpShiftLeftLogical, 1, 32, out _));
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpShiftRightLogical, 1u, -1, out _));
        // Operands of different types
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpIAdd, 1, 1u, out _));
        // A signed operation on an unsigned value, and the other way around
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpSLessThan, 1u, 2u, out _));
        Assert.False(ConstantEvaluator.TryEvaluateBinary(Op.OpUDiv, 4, 2, out _));
        // A bitcast keeps the width
        Assert.False(ConstantEvaluator.TryEvaluateUnary(Op.OpBitcast, 1, ScalarType.UInt64, out _));
        Assert.False(ConstantEvaluator.TryEvaluateUnary(Op.OpFNegate, 1, ScalarType.Int, out _));
    }
}
