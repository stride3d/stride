using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives;

public class NumberLiteral : DirectiveToken
{
    public bool Negative { get; set; } = false;
    public object? Value { get; set; }
    public string? Suffix { get; set; }


    public NumberLiteral() { }

    public NumberLiteral(Match match)
    {
        Match = match;
        if (!match.HasMatches)
        {
            Value = match.Value;
        }
        else
        {
            if (match.Name == "SignedTermExpression")
            {

            }
            else
            {
                Value = match.Matches[0].Value;
                Suffix = match["Suffix"].StringValue;
            }
        }
    }
}
public class HexLiteral : DirectiveToken
{
    public ulong Value { get; set; }

    public HexLiteral() { }

    public HexLiteral(Match match)
    {
        Match = match;
        Value = Convert.ToUInt64(match.StringValue, 16);
    }
}
public class StringLiteral : DirectiveToken
{
    public string? Value { get; set; }


    public StringLiteral() { }

    public StringLiteral(Match match)
    {
        Match = match;
        Value = match.StringValue;
    }
}

public class BoolLiteral : DirectiveToken
{
    public bool Value { get; set; }

    public BoolLiteral() { }

    public BoolLiteral(Match match)
    {
        Match = match;
        Value = (bool)match.Value;
    }
}


public class TypeNameLiteral : DirectiveToken
{
    public string Name { get; set; }

    public TypeNameLiteral(Match m)
    {
        Name = m.StringValue;
    }
}

public class VariableNameLiteral : DirectiveToken
{
    public string Name { get; set; }

    public VariableNameLiteral(Match m)
    {
        Name = m.StringValue;
    }
    public override string ToString()
    {
        return $"{{ Variable : {Name} }}" ;
    }
}