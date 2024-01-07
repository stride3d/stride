using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using SDSL.Parsing.AST.Shader;
using SDSL.Symbols;
using shortid;

namespace SDSL.TAC;

public sealed partial class IR
{
    Operand? Convert(Expression expr)
    {
        if (expr is NumberLiteral nl)
        {
            return 
                new(
                    nl.Value.ToString(),
                    Kind.Constant,
                    nl.InferredType
                );
            
        }
        else if (expr is BoolLiteral bl)
        {
            return 
                new(
                    bl.Value.ToString(),
                    Kind.Constant,
                    SymbolType.Scalar("bool")
                );
        }
        else if (expr is VariableNameLiteral vnl)
        {
            return
                new(
                    vnl.Name,
                    Kind.Variable,
                    vnl.InferredType
                );
        }
        else if (expr is UnaryExpression ue)
            return Convert(ue);
        else if (expr is Operation op)
        {
            var resultL = Convert(op.Left as Expression ?? throw new NotImplementedException());
            var resultR = Convert(op.Right as Expression ?? throw new NotImplementedException());
           
            Add(
                new(
                    op.Op.Convert(),
                    resultL,
                    resultR,
                    new(ShortId.Generate(), Kind.Variable)
                )
            );
            return this[Count - 1].Result;
        }
        else
            throw new NotImplementedException();
    }

    Operand? Convert(UnaryExpression ue)
    {
        throw new NotImplementedException();
    }
}