using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Parsing.Analysis;

public static class OperatorTable
{

    public static bool CheckBinaryOperation(SymbolType left, SymbolType right, Operator op)
    {
        int a = 0;
        float b = 0;
        var c = b * a;
        return (left, right, op) switch
        {
            // Scalar operations
            (
                Scalar { TypeName: "int" or "long" }, Scalar { TypeName: "int" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
                or Operator.LeftShift or Operator.RightShift
                or Operator.OR or Operator.XOR or Operator.AND
            ) => true,
            (
                Scalar { TypeName: "float" or "double" }, Scalar { TypeName: "double" or "float" or "int" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            (
                Scalar { TypeName: "float" } or Scalar { TypeName: "int" }, Scalar { TypeName: "float" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,

            // Vector operations
            (
                Vector { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Scalar { TypeName: "int" or "float" or "long" or "double" },
                Vector { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Scalar { TypeName: "int" or "float" or "long" or "double" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            // Matrix operations
            (
                Matrix { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Scalar { TypeName: "int" or "float" or "long" or "double" },
                Matrix { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Scalar { TypeName: "int" or "float" or "long" or "double" },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,
            (
                Matrix { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Vector { BaseType: Scalar { TypeName: "int" or "float" or "long" or "double" } },
                Matrix { BaseType: Scalar { TypeName: "int" or "long" or "float" } } or Vector { BaseType: Scalar { TypeName: "int" or "float" or "long" or "double" } },
                Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod
            ) => true,

            _ => false,
        };
    }
    public static bool BinaryOperationResultingType(SymbolType left, SymbolType right, Operator op, out SymbolType? result)
    {
        long a = 0;
        float b = 0;
        float c = a * b;
        // TODO : correct that part
        result = ((int)op, left, right) switch
        {
            // Boolean operations
            (>= 22 and < 26, Scalar{ TypeName : "bool"}, Scalar {TypeName: "bool"}) => left,
            // Linear algebra
            (>=8 and < 13, Scalar {TypeName: "int" or "uint" or "float" or "long" or "ulong" or "double"} l, Scalar r) when l.TypeName == r.TypeName => right,
            (>=8 and < 13, Scalar { TypeName: "int" or "uint" or "long" or "ulong" }, Scalar { TypeName: "float" or "double"}) => right,
            (>=8 and < 13, Scalar { TypeName: "float" }, Scalar { TypeName: "int" or "float" }) => left,
            (>=8 and < 13, Vector l, Vector r) when l.BaseType == r.BaseType => right,
            (>=8 and < 13, Vector, Scalar) => left,
            (>=8 and < 13, Matrix l, Matrix r) when l.BaseType == r.BaseType => right,
            (>=8 and < 13, Matrix l, Scalar r) => l,
            (>=8 and < 13, Matrix l, Vector r) => l,
            (>=8 and < 13, Matrix { BaseType: Scalar { TypeName: "int" } } l, Matrix { BaseType: Scalar { TypeName: "int" or "float" } } r) => l,
            // Comparison
            (>=18 and < 22, Scalar {TypeName: "int" or "uint" or "float" or "long" or "ulong" or "double"} l, Scalar r) when l.TypeName == r.TypeName => Scalar.From("bool"),
            _ => null,
        };
        return result != null;
    }
}