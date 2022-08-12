using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.AST.Shader;

public static class Lowering
{
    public static IEnumerable<Register> LowerToken(ShaderToken token)
    {

        return token switch
        {
            BlockStatement t => Lower(t),
            AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ArrayAccessor t => Lower(t),
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
            new AssignChainRegister
            {
                NameChain = ac.AccessNames,
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
            new DeclareAssignRegister
            {
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
        return new Register[]{
            new ChainAccessorRegister{Left = LowerToken(lit.Value), Right = LowerToken(lit.Field)}
        };
    }
    static IEnumerable<Register> Lower(ArrayAccessor lit)
    {
        var array = LowerToken(lit.Value);
        var accessors = lit.Accessors.SelectMany(LowerToken);
        return
            array
            .Concat(
                new Register[]{
                    new ArrayAccessorRegister{Array = array.Last() , Indices = accessors}
                }
            );
    }
    static IEnumerable<Register> Lower(ShaderLiteral lit)
    {
        return new List<Register>{
            lit switch {
                NumberLiteral nl => new LiteralRegister{Value = nl},
                VariableNameLiteral vn => new VariableRegister{Name = vn.Name},
                _ => throw new NotImplementedException()
            }
        };
    }

}