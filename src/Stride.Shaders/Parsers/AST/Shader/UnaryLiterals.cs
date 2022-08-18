using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public class UnaryExpression : Projector
{
    public override string InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

public class ChainAccessor : UnaryExpression
{
    public ShaderToken Value { get; set; }
    public IEnumerable<ShaderToken> Field { get; set; }

    public ChainAccessor(Match m)
    {
        Match = m;
        Value = GetToken(m.Matches["Identifier"]);
        Field = m.Matches.GetRange(1,m.Matches.Count-1).Select(GetToken);
    }
}

public class ArrayAccessor : UnaryExpression
{
    public ShaderToken Value { get; set; }
    public IEnumerable<ShaderToken> Accessors { get; set; }
    
    public ArrayAccessor(Match m)
    {
        Match = m;
        Value= GetToken(m.Matches[0]);
        Accessors = m.Matches.GetRange(1,m.Matches.Count-1).Select(GetToken);
    }
}


public class PostfixIncrement : UnaryExpression
{
    public string Operator { get; set; }
    public ShaderToken Value { get; set; }
    public PostfixIncrement(Match m)
    {
        Match = m;
        Value = GetToken(m.Matches[0]);
        Operator = m.Matches[1].StringValue;
    }

    public override string ToString()
    {
        return $"{{ PostfixIncrement :  [\"{Value}\", \"{Operator}\"] }}";
    }
}

public class PrefixIncrement : UnaryExpression
{
    public string Operator { get; set; }
    public ShaderToken Value { get; set; }
    public PrefixIncrement(Match m)
    {
        Match = m;
        Operator = m.Matches[0].StringValue;
        Value = GetToken(m.Matches[1]);
    }
}

public class CastExpression : UnaryExpression
{
    public TypeNameLiteral Target { get; set; }
    public ShaderToken From { get; set; }
    public CastExpression(Match m)
    {
        Target = new TypeNameLiteral(m.Matches[0]);
        From = GetToken(m.Matches[1]);
    }
}