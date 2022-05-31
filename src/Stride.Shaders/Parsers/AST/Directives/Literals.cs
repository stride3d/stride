using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Directives;


public class DirectiveLiteral : DirectiveToken
{
    public override Type InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void EvaluateMacros(Dictionary<string, object> macros)
    {
        throw new NotImplementedException();
    }
}

public class NumberLiteral : DirectiveLiteral
{
    public bool Negative { get; set; } = false;
    public object Value { get; set; }
    public string? Suffix { get; set; }

    protected Type? inferredType;

    public override Type InferredType 
    {
        get
        {
            if (inferredType is not null)
                return inferredType;
            if (Suffix is null)
                return Value.GetType();
            else
            {
                return Suffix switch
                {
                    "u" or "l" => typeof(long),
                    "f" or "d" => typeof(double),
                    _ => typeof(long)
                };
            }
        }
        set => inferredType = value; 
    }

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
public class HexLiteral : NumberLiteral
{

    public override Type InferredType 
    {
        get
        {
            return typeof(long);
        }
        set => inferredType = value;
    }


    public HexLiteral() { }

    public HexLiteral(Match match)
    {
        Match = match;
        Value = Convert.ToUInt64(match.StringValue, 16);
    }
}
public class StringLiteral : DirectiveLiteral
{
    public string? Value { get; set; }
    public override Type InferredType { get => typeof(string); set { } }

    public StringLiteral() { }

    public StringLiteral(Match match)
    {
        Match = match;
        Value = match.StringValue;
    }
}

public class BoolLiteral : DirectiveLiteral
{
    public bool Value { get; set; }
    public override Type InferredType { get => typeof(bool); set { } }

    public BoolLiteral() { }

    public BoolLiteral(Match match)
    {
        Match = match;
        Value = (bool)match.Value;
    }
}


public class TypeNameLiteral : DirectiveLiteral
{
    public string Name { get; set; }

    public TypeNameLiteral(Match m)
    {
        Name = m.StringValue;
    }
}

public class VariableNameLiteral : DirectiveLiteral
{
    public string Name { get; set; }
    public object Value { get; set; }

    Type? inferredType;

    public override Type InferredType { get => inferredType ?? typeof(void); set => inferredType = value; }


    public VariableNameLiteral(Match m)
    {
        Name = m.StringValue;
    }
    public override string ToString()
    {
        return $"{{ Variable : {Name} }}" ;
    }
}