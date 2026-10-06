// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Folds an expression made only of unsuffixed literals the way HLSL does: integers in 64 bits, floats in double,
/// and the result then takes the type of its context.
/// </summary>
public static class LiteralFolding
{
    /// <summary>
    /// The value of an unsuffixed literal expression: an integer (<see cref="Int"/>) or a float (<see cref="Float"/>).
    /// </summary>
    public readonly record struct LiteralValue(bool IsFloat, long Int, double Float)
    {
        public double AsDouble => IsFloat ? Float : Int;
        public static LiteralValue FromInt(long value) => new(false, value, 0);
        public static LiteralValue FromFloat(double value) => new(true, 0, value);
    }

    /// <summary>
    /// Evaluates <paramref name="expression"/> when it is made only of unsuffixed literals and operators HLSL folds.
    /// </summary>
    /// <returns><c>false</c> when an operand has a type of its own (suffix, variable, call...) or the operation can't be folded (division by zero).</returns>
    public static bool TryFold(Expression expression, out LiteralValue value)
    {
        value = default;
        switch (expression)
        {
            // A suffix (u, l, f...) or a hexadecimal literal gives its own type; a ulong past long.MaxValue isn't folded
            case IntegerLiteral { Unsuffixed: true, Suffix: not { Size: 64, Signed: false } } integer:
                value = LiteralValue.FromInt(integer.Value);
                return true;
            case FloatLiteral { Unsuffixed: true } floating:
                value = LiteralValue.FromFloat(floating.Value);
                return true;
            case ParenthesisExpression parenthesis:
                return TryFold(parenthesis.Expression, out value);
            case PrefixExpression { Operator: Operator.Plus or Operator.Minus or Operator.BitwiseNot } prefix:
                if (!TryFold(prefix.Expression, out var operand))
                    return false;
                return TryFoldUnary(prefix.Operator, operand, out value);
            case BinaryExpression binary:
                if (!TryFold(binary.Left, out var left) || !TryFold(binary.Right, out var right))
                    return false;
                return TryFoldBinary(binary.Op, left, right, out value);
            default:
                return false;
        }
    }

    static bool TryFoldUnary(Operator op, LiteralValue operand, out LiteralValue value)
    {
        value = (op, operand.IsFloat) switch
        {
            (Operator.Plus, _) => operand,
            (Operator.Minus, false) => LiteralValue.FromInt(unchecked(-operand.Int)),
            (Operator.Minus, true) => LiteralValue.FromFloat(-operand.Float),
            (Operator.BitwiseNot, false) => LiteralValue.FromInt(~operand.Int),
            _ => default,
        };
        return op is not Operator.BitwiseNot || !operand.IsFloat;
    }

    static bool TryFoldBinary(Operator op, LiteralValue left, LiteralValue right, out LiteralValue value)
    {
        value = default;
        if (left.IsFloat || right.IsFloat)
        {
            double l = left.AsDouble, r = right.AsDouble;
            switch (op)
            {
                case Operator.Plus: value = LiteralValue.FromFloat(l + r); return true;
                case Operator.Minus: value = LiteralValue.FromFloat(l - r); return true;
                case Operator.Mul: value = LiteralValue.FromFloat(l * r); return true;
                case Operator.Div: value = LiteralValue.FromFloat(l / r); return true;
                // HLSL's % on floats is fmod, which truncates like C#'s
                case Operator.Mod: value = LiteralValue.FromFloat(l % r); return true;
                default: return false;
            }
        }

        long a = left.Int, b = right.Int;
        // Left to the GPU: a division by zero, and the one quotient that doesn't fit
        if (op is Operator.Div or Operator.Mod && (b == 0 || (a == long.MinValue && b == -1)))
            return false;
        unchecked
        {
            switch (op)
            {
                case Operator.Plus: value = LiteralValue.FromInt(a + b); return true;
                case Operator.Minus: value = LiteralValue.FromInt(a - b); return true;
                case Operator.Mul: value = LiteralValue.FromInt(a * b); return true;
                case Operator.Div: value = LiteralValue.FromInt(a / b); return true;
                case Operator.Mod: value = LiteralValue.FromInt(a % b); return true;
                case Operator.AND: value = LiteralValue.FromInt(a & b); return true;
                case Operator.OR: value = LiteralValue.FromInt(a | b); return true;
                case Operator.XOR: value = LiteralValue.FromInt(a ^ b); return true;
                case Operator.LeftShift: value = LiteralValue.FromInt(a << (int)(b & 63)); return true;
                case Operator.RightShift: value = LiteralValue.FromInt(a >> (int)(b & 63)); return true;
                default: return false;
            }
        }
    }

    /// <summary>
    /// Gives a folded value the type its context expects, or <c>int</c>/<c>float</c> without one.
    /// </summary>
    /// <remarks>An integer takes any numeric context; a float only a floating one, it is converted to an integer by the consumer like any float.</remarks>
    public static (ScalarType Type, object Value) ToConstant(LiteralValue value, SymbolType? expectedType)
    {
        var target = expectedType is ScalarType or VectorType or MatrixType ? expectedType.GetElementType() : null;
        if (value.IsFloat)
        {
            return target is { Type: Scalar.Double }
                ? (ScalarType.Double, (object)value.Float)
                : (ScalarType.Float, (object)(float)value.Float);
        }

        return target?.Type switch
        {
            Scalar.UInt => (ScalarType.UInt, (object)unchecked((uint)value.Int)),
            Scalar.Int64 => (ScalarType.Int64, (object)value.Int),
            Scalar.UInt64 => (ScalarType.UInt64, (object)unchecked((ulong)value.Int)),
            Scalar.Float => (ScalarType.Float, (object)(float)value.Int),
            Scalar.Double => (ScalarType.Double, (object)(double)value.Int),
            _ => (ScalarType.Int, (object)unchecked((int)value.Int)),
        };
    }
}
