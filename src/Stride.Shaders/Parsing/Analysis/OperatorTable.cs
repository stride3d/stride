using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Parsing.Analysis;

public static class OperatorTable
{
    public static bool CheckBinaryOperation(SymbolType left, SymbolType right, Operator op)
    {
        return (left, right, op) switch
        {
            // Scalar operations
            (
                ScalarType { TypeName: "int" or "long" }, ScalarType { TypeName: "int" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
                or Operator.LeftShift or Operator.RightShift
                or Operator.OR or Operator.XOR or Operator.AND
            ) => true,
            (
                ScalarType { TypeName: "float" or "double" }, ScalarType { TypeName: "double" or "float" or "int" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            (
                ScalarType { TypeName: "float" } or ScalarType { TypeName: "int" }, ScalarType { TypeName: "float" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,

            // Vector operations
            (
                VectorType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or ScalarType { TypeName: "int" or "float" or "long" or "double" },
                VectorType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or ScalarType { TypeName: "int" or "float" or "long" or "double" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            // Matrix operations
            (
                MatrixType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or ScalarType { TypeName: "int" or "float" or "long" or "double" },
                MatrixType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or ScalarType { TypeName: "int" or "float" or "long" or "double" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            (
                MatrixType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or VectorType { BaseType: ScalarType { TypeName: "int" or "float" or "long" or "double" } },
                MatrixType { BaseType: ScalarType { TypeName: "int" or "long" or "float" } } or VectorType { BaseType: ScalarType { TypeName: "int" or "float" or "long" or "double" } },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,

            _ => false,
        };
    }
    public static bool BinaryOperationResultingType(SymbolType left, SymbolType right, Operator op, out SymbolType? result)
    {
        // TODO : correct that part
        result = ((int)op, left, right) switch
        {
            // Boolean operations
            (>= 22 and < 26, ScalarType{ TypeName : "bool"}, ScalarType {TypeName: "bool"}) => left,
            // Equalities
            (>= 22 and < 26, ScalarType l, ScalarType r) when l == r => ScalarType.From("bool"),
            // Linear algebra
            (>=8 and < 13, ScalarType {TypeName: "int" or "uint" or "float" or "long" or "ulong" or "double"} l, ScalarType r) when l.TypeName == r.TypeName => right,
            (>=8 and < 13, ScalarType { TypeName: "int" or "uint" or "long" or "ulong" }, ScalarType { TypeName: "float" or "double"}) => right,
            (>=8 and < 13, ScalarType { TypeName: "float" }, ScalarType { TypeName: "int" or "float" }) => left,
            (>=8 and < 13, VectorType l, VectorType r) when l.BaseType == r.BaseType => right,
            (>=8 and < 13, VectorType, ScalarType) => left,
            (>=8 and < 13, MatrixType l, MatrixType r) when l.BaseType == r.BaseType => right,
            (>=8 and < 13, MatrixType l, ScalarType r) => l,
            (>=8 and < 13, MatrixType l, VectorType r) => l,
            (>=8 and < 13, MatrixType { BaseType: ScalarType { TypeName: "int" } } l, MatrixType { BaseType: ScalarType { TypeName: "int" or "float" } } r) => l,
            // Comparison
            (>=18 and < 22, SymbolType l, SymbolType r) when l == r => ScalarType.From("bool"),
            _ => null,
        };
        return result != null;
    }
}