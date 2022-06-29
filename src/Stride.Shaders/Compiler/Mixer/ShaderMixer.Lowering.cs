using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Spirv;

namespace Stride.Shaders;

public partial class ShaderMixer
{
    public IEnumerable<Register> LowerToken(ShaderToken token)
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

    IEnumerable<Register> Lower(DeclareAssign s)
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

    IEnumerable<Register> Lower(ConditionalExpression cexp)
    {
        var result = new List<Register>();
        return result;
    }

    IEnumerable<Register> Lower(Operation o)
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

    IEnumerable<Register> Lower(ShaderLiteral lit)
    {
        return new List<Register>{
            lit switch {
                NumberLiteral nl => new ValueRegister(nl),
                _ => new ValueRegister(lit)
            }
        };
    }
    
}