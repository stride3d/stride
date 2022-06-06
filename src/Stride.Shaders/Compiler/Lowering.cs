using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Compiling;

public static class Lowering
{
    public static IEnumerable<Register> LowerToken(ShaderToken token)
    {
        
        return token switch 
        {
            DeclareAssign t => Lower(t),
            ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    static IEnumerable<Register> Lower(DeclareAssign s)
    {
        var v = LowerToken(s.Value);
        return v.Append(
            new AssignRegister{
                Name = s.VariableName,
                Value = v.Last(),
                Op = s.AssignOp
            }
        );
    }

    static IEnumerable<Register> Lower(ConditionalExpression cexp)
    {
        var result = new List<Register>();
        return result;
    }

    static IEnumerable<Register> Lower(Operation o)
    {
        IEnumerable<Register> l = LowerToken(o.Left);
        IEnumerable<Register> r = LowerToken(o.Right);

        return l.Concat(r).Append(
            new OperationRegister
            {
                Op = o.Op,
                Left = l.Last(),
                Right = r.Last()
            }
        );
    }

    static IEnumerable<Register> Lower(ShaderLiteral lit)
    {
        return new List<Register>{
            lit switch {
                NumberLiteral nl => new ValueRegister(nl),
                _ => new ValueRegister(lit)
            }
        };
    }
    
}