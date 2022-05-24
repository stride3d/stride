using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives;


public class ChainAccessor : DirectiveToken
{
    public DirectiveToken Value { get; set; }
    public DirectiveToken Field { get; set; }

    public ChainAccessor(Match m)
    {
        Match = m;
        Value = GetToken(m.Matches[0]);
        Field = GetToken(m.Matches[1]);
    }
}

public class ArrayAccessor : DirectiveToken
{
    public DirectiveToken Value { get; set; }
    public IEnumerable<DirectiveToken> Accessors { get; set; }
    
    public ArrayAccessor(Match m)
    {
        Match = m;
        Value= GetToken(m.Matches[0]);
        Accessors = m.Matches.GetRange(1,m.Matches.Count-1).Select(GetToken);
    }
}


public class PostfixIncrement : DirectiveToken
{
    public string Operator { get; set; }
    public DirectiveToken Value { get; set; }
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

public class PrefixIncrement : DirectiveToken
{
    public string Operator { get; set; }
    public DirectiveToken Value { get; set; }
    public PrefixIncrement(Match m)
    {
        Match = m;
        Operator = m.Matches[0].StringValue;
        Value = GetToken(m.Matches[1]);
    }
}

public class CastExpression : DirectiveToken
{
    public TypeNameLiteral Target { get; set; }
    public DirectiveToken From { get; set; }
    public CastExpression(Match m)
    {
        Target = new TypeNameLiteral(m.Matches[0]);
        From = GetToken(m.Matches[1]);
    }
}