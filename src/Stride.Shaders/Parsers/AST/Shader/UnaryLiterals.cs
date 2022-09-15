using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public class UnaryExpression : Expression
{
    public override ISymbolType InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

public class ChainAccessor : UnaryExpression, IStreamCheck, IVariableCheck
{
    public ShaderToken Value { get; set; }
    public IEnumerable<ShaderToken> Field { get; set; }

    public ChainAccessor(Match m, SymbolTable s)
    {
        Match = m;
        Value = GetToken(m.Matches["Identifier"],s);
        Field = m.Matches.GetRange(1,m.Matches.Count-1).Select(x => GetToken(x,s));
    }

    public IEnumerable<string> GetUsedStream()
    {
        if(Value is VariableNameLiteral vn && vn.Name == "streams")
            return new List<string>{((VariableNameLiteral)Field.First()).Name};
        return Enumerable.Empty<string>();
    }
    public IEnumerable<string> GetAssignedStream()
    {
        return Enumerable.Empty<string>();
    }
    public bool CheckStream(SymbolTable symbols)
    {
        return GetUsedStream().Any();
    }
    public void CheckVariables(SymbolTable s)
    {
        if(Value is IVariableCheck n) n.CheckVariables(s);
    }
}

public class ArrayAccessor : UnaryExpression, IVariableCheck
{
    public ShaderToken Value { get; set; }
    public IEnumerable<ShaderToken> Accessors { get; set; }
    
    public ArrayAccessor(Match m, SymbolTable s)
    {
        Match = m;
        Value= GetToken(m.Matches[0],s);
        throw new NotImplementedException();
        // Accessors = m.Matches.GetRange(1,m.Matches.Count-1).Select(GetToken);
    }
    public void CheckVariables(SymbolTable s)
    {
        if(Value is IVariableCheck n) n.CheckVariables(s);
    }
}


public class PostfixIncrement : UnaryExpression, IVariableCheck
{
    public string Operator { get; set; }
    public ShaderToken Value { get; set; }
    public PostfixIncrement(Match m, SymbolTable s)
    {
        Match = m;
        Value = GetToken(m.Matches[0],s);
        Operator = m.Matches[1].StringValue;
    }

    public override string ToString()
    {
        return $"{{ PostfixIncrement :  [\"{Value}\", \"{Operator}\"] }}";
    }
    public void CheckVariables(SymbolTable s)
    {
        if(Value is VariableNameLiteral n) n.CheckVariables(s);
    }
}

public class PrefixIncrement : UnaryExpression
{
    public string Operator { get; set; }
    public ShaderToken Value { get; set; }
    public PrefixIncrement(Match m, SymbolTable s)
    {
        Match = m;
        Operator = m.Matches[0].StringValue;
        Value = GetToken(m.Matches[1],s);
    }
}

public class CastExpression : UnaryExpression
{
    public TypeNameLiteral Target { get; set; }
    public ShaderToken From { get; set; }
    public CastExpression(Match m, SymbolTable s)
    {
        Target = new TypeNameLiteral(m.Matches[0],s);
        From = GetToken(m.Matches[1],s);
    }
}