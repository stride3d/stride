using Stride.Core.Extensions;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.Spirv;
using SDSL.ThreeAddress;
using System.Reflection.Metadata.Ecma335;

namespace SDSL.ThreeAddress;

public partial class TAC
{

    public List<Register> LowerToken(ShaderToken token, bool isHead = true)
    {
        if (token is Declaration d) symbols.PushVar(d);
        else if (token is BlockStatement) symbols.AddScope();
        else if (token is Statement s) symbols.CheckVar(s);
        return token switch
        {
            BlockStatement t => Lower(t),
            AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            ValueMethodCall t => Lower(t),
            // ConditionalExpression t => Lower(t),
            Operation t => Lower(t),
            ArrayAccessor t => Lower(t, isHead),
            ChainAccessor t => Lower(t, isHead),
            ShaderLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    public List<Register> Lower(BlockStatement b)
    {
        return b.Statements.SelectMany(x => LowerToken(x)).ToList();
    }
    
    public List<Register> Lower(ValueMethodCall vm)
    {
        var values = vm.Parameters.SelectMany(x => LowerToken(x)).ToList();
        var r = new CompositeConstant(values.Select(x => x.Name ?? ""));
        Add(r);
        values.Add(r);
        return values;
    }

    public List<Register> Lower(DeclareAssign d)
    {
        var value = LowerToken(d.Value).ToList();
        var r = new Copy(value.Last().Name){Name = d.VariableName};
        Add(r);
        value.Add(r);
        return value;
    }
    public List<Register> Lower(AssignChain a)
    {
        var value = LowerToken(a.Value);
        ISymbolType tmp = ScalarType.VoidType;
        var accessors = new List<int>(a.AccessNames.Count());
        for (int i = 0; i < a.AccessNames.Count(); i++)
        {
            var current = a.AccessNames.ElementAt(i);
            if (i == 0)
                symbols.TryGetVarType(current, out tmp);
            else
            {
                if (tmp is CompositeType ct)
                    accessors.Add(ct.Fields.IndexOfKey(current));
                tmp.TryAccessType(current, out tmp);
            }
        }
        var r = new ChainRegister(accessors) { Name = string.Join(".",a.AccessNames) };
        Add(r);
        var assign = new Copy(value[^1].Name, false) { Name = r.Name };
        Add(assign);
        value.Add(r);
        value.Add(assign);
        return value;
    }


    public List<Register> Lower(Operation o)
    {
        var left = LowerToken(o.Left);
        var right = LowerToken(o.Right);
        var r = new Assign(left.Last().Name, (Operator)o.Op, right.Last().Name);
        Add(r);
        left.AddRange(right);
        left.Add(r);
        return left;
    }

    public List<Register> Lower(ChainAccessor ca, bool isHead = true)
    {
        if(!isHead)
        {
            return ca.Field.SelectMany(x => LowerToken(x,false)).ToList();
        }
        else 
        {
            var accessors = new List<int>(ca.Field.Count());
            symbols.TryGetVarType(((VariableNameLiteral)ca.Value).Name, out var tmp);
            for (int i = 0; i < ca.Field.Count; i++)
            {
                if (ca.Field[i] is VariableNameLiteral fvn)
                {
                    if (tmp is CompositeType ct)
                        accessors.Add(ct.Fields.IndexOfKey(fvn.Name));
                    tmp.TryAccessType(fvn.Name, out tmp);
                }
                else throw new Exception();
            }
            var r = new ChainRegister(accessors) { Name = string.Join(".", Enumerable.Empty<ShaderToken>().Append(ca.Value).Concat(ca.Field).Cast<VariableNameLiteral>().Select(x => x.Name)) };
            Add(r);
            return new List<Register>{r};
        }
    }
    public List<Register> Lower(ArrayAccessor aa, bool isHead = true)
    {
        if(!isHead)
        {
            return aa.Accessors.SelectMany(x => LowerToken(x,false)).ToList();
        }
        else 
        {
            var accessors = aa.Accessors.SelectMany(x => LowerToken(x, false));
            var r = new ChainRegister(new()){};
            Add(r);
            return new List<Register>{r};
        }
    }

    public List<Register> Lower(ShaderLiteral l)
    {
        var result = new List<Register>();
        if (l is NumberLiteral n)
        {
            var c = AddConst(new Constant<NumberLiteral>(n));
            return new List<Register> { c };
        }
        else if (l is VariableNameLiteral vn)
        {
            return new List<Register> { IntermediateCode[LookUp[vn.Name]] };
        }
        else
            throw new NotImplementedException();
    }

    

}