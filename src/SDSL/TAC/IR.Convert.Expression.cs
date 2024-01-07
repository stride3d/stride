using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using SDSL.Parsing.AST.Shader;
using SDSL.Symbols;

namespace SDSL.TAC;

public sealed partial class IR
{
    int Convert(Expression expr)
    {
        if (expr is NumberLiteral nl)
        {
            Add(
                new(
                    Operator.Declare,
                    Result: new(
                        "constant" + nl.Value,
                        Kind.Constant,
                        nl.InferredType
                    )
                )
            );
            return Count;
        }
        else if (expr is BoolLiteral bl)
        {
            Add(
                new(
                    Operator.Declare,
                    Result: new(
                        "constant" + bl.Value,
                        Kind.Constant,
                        SymbolType.Scalar("bool")
                    )
                )
            );
            return Count;
        }
        else if (expr is UnaryExpression ue)
            return Convert(ue);
        else if (expr is Operation op)
        {
            var indexLeft = Convert(op.Left as Expression ?? throw new NotImplementedException());
            var indexRight = Convert(op.Left as Expression ?? throw new NotImplementedException());
            var resultLeft = this[indexLeft].Result;
            var resultRight = this[indexRight].Result;
            Add(
                new(
                    op.Op.Convert(),
                    resultLeft,
                    resultRight,
                    new($"t_{resultLeft?.Value}{op.Op}{resultRight?.Value}", Kind.Variable)
                )
            );
            return Count;
        }
        else
            throw new NotImplementedException();
    }

    int Convert(UnaryExpression ue)
    {
        throw new NotImplementedException();
    }
}