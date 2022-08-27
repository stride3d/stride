using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Spirv;
using Stride.Shaders.ThreeAddress;

namespace Stride.Shaders.ThreeAddress;

public partial class Snippet
{

    public IEnumerable<Register> LowerToken(ShaderToken token, bool isHead = true)
    {

        return token switch
        {
            // BlockStatement t => Lower(t),
            // AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            // ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ArrayAccessor t => Lower(t, isHead),
            ChainAccessor t => Lower(t, isHead),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    public IEnumerable<Register> Lower(DeclareAssign d)
    {
        var value = LowerToken(d.Value);
        var r = new Copy(value.Last().Name){Name = d.VariableName};
        Add(r);
        return value.Append(r);
    }


    public IEnumerable<Register> Lower(Operation o)
    {
        var left = LowerToken(o.Left);
        var right = LowerToken(o.Right);
        var r = new Assign(left.Last().Name, (Operator)o.Op, right.Last().Name);
        Add(r);
        return left.Concat(right).Append(r);
    }

    public IEnumerable<Register> Lower(ChainAccessor ca, bool isHead = true)
    {
        if(!isHead)
        {
            return ca.Field.SelectMany(x => LowerToken(x,false));
        }
        else 
        {
            var accessors = ca.Field.SelectMany(x => LowerToken(x, false));
            var r = new ChainAccessorRegister(){Accessors = accessors.Select(x => x.Name)};
            Add(r);
            return new List<Register>{r};
        }
    }
    public IEnumerable<Register> Lower(ArrayAccessor aa, bool isHead = true)
    {
        if(!isHead)
        {
            return aa.Accessors.SelectMany(x => LowerToken(x,false));
        }
        else 
        {
            var accessors = aa.Accessors.SelectMany(x => LowerToken(x, false));
            var r = new ChainAccessorRegister(){Accessors = accessors.Select(x => x.Name)};
            Add(r);
            return new List<Register>{r};
        }
    }

    public IEnumerable<Register> Lower(ShaderLiteral l)
    {
        var result = l switch {
            NumberLiteral n => new List<Register>{new Constant<NumberLiteral>(n)},
            VariableNameLiteral n => new List<Register>{new Constant<VariableNameLiteral>(n){Name = n.Name}},
            _ => throw new NotImplementedException()
        };
        foreach(var e in result) Add(e);
        return result;
    }

    

}