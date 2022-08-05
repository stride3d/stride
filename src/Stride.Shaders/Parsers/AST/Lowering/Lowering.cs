using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.AST.Shader;

public static class Lowering
{
    public static  IEnumerable<Register> LowerToken(ShaderToken token)
    {
        
        return token switch 
        {
            BlockStatement t => Lower(t),
            AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ChainAccessor t => Lower(t),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    static IEnumerable<Register> Lower(BlockStatement b)
    {
        b.LowCode = b.Statements.SelectMany(LowerToken);
        return b.LowCode;
    }
    static IEnumerable<Register> Lower(AssignChain ac)
    {
        var v = LowerToken(ac.Value);
        ac.LowCode = v.Append(
            new AssignChainRegister{
                Chain = ac.AccessNames,
                Value = v.Last(),
                Op = ac.AssignOp
            }
        );
        return ac.LowCode;
    }
    static IEnumerable<Register> Lower(DeclareAssign s)
    {
        var v = LowerToken(s.Value);
        s.LowCode = v.Append(
            new AssignRegister{
                Name = s.VariableName,
                Value = v.Last(),
                Op = s.AssignOp
            }
        );
        return s.LowCode;
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
    static IEnumerable<Register> Lower(ChainAccessor lit)
    {
        throw new NotImplementedException();
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