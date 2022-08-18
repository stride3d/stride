using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.AST.Shader;

public static class Lowering
{
    public static IEnumerable<Register> LowerToken(ShaderToken token, bool isHead = true)
    {

        return token switch
        {
            BlockStatement t => Lower(t),
            AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ArrayAccessor t => Lower(t, isHead),
            ChainAccessor t => Lower(t, isHead),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    static IEnumerable<Register> Lower(BlockStatement b)
    {
        b.LowCode = b.Statements.SelectMany(x => LowerToken(x));
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
    static IEnumerable<Register> Lower(ChainAccessor lit, bool isHead = true)
    {
        // var a = streams.a.b.c[0][5]
        if (!isHead)
        {
            var result =
                LowerToken(lit.Value, false)
                .Concat(lit.Field.SelectMany(x => LowerToken(x, false)));
            return result;
        }
        else
        {
            var value = LowerToken(lit.Value, false);
            var accessors = lit.Field.SelectMany(x => LowerToken(x, false)).Select(AccessorTypes.From);
            return new Register[]{
                new AccessorRegister{Variable = value.Last(), AccessorList = accessors}
            };
        }
    }
    static IEnumerable<Register> Lower(ArrayAccessor lit, bool isHead = true)
    {
        if (!isHead)
        {
            var accessors = lit.Accessors.SelectMany(x => LowerToken(x, false));
            return LowerToken(lit.Value, false).Concat(accessors);
        }
        else
        {
            var array = LowerToken(lit.Value, false);
            var accessors = lit.Accessors.SelectMany(x => LowerToken(x, false)).Select(AccessorTypes.From);
            return
                array
                .Concat(
                    new Register[]{
                    new AccessorRegister{Variable = array.Last() , AccessorList = accessors}
                    }
                );
        }
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