using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public class ShaderLiteral : Expression
{
    public override ISymbolType InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public object Value { get; set; }
}

public class NumberLiteral : ShaderLiteral
{
    public bool Negative { get; set; } = false;
    public string? Suffix { get; set; }
    public override ISymbolType InferredType
    {
        get => inferredType ?? throw new NotImplementedException();
        set => inferredType = value;
    }

    public NumberLiteral() { }

    public NumberLiteral(Match match, SymbolTable s)
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
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        throw new NotImplementedException();
        if (Suffix is null)
        {
        //     if (expected != string.Empty)
        //     {
        //         inferredType = (Value, expected) switch
        //         {
        //             (_, "double") => "double",
        //             (_, "float") => "float",
        //             (_, "half") => "half",
        //             (long l, "long") => "long",
        //             (long l, "int") => "int",
        //             (long l, "uint") => "uint",
        //             (long l, "short") => "short",
        //             (long l, "byte") => "byte",
        //             (long l, "sbyte") => "sbyte",
        //             _ => throw new NotImplementedException()
        //         };
        //     }
        //     else
        //     {
        //         inferredType = "int";
        //     }
        // }
        // else
        // {
        //     if (expected != string.Empty)
        //     {
        //         inferredType = Suffix switch
        //         {
        //             "l" => "long",
        //             "u" => "uint",
        //             "f" => "float",
        //             "d" => "double",
        //             _ => throw new NotImplementedException()
        //         };
        //         if (expected != inferredType)
        //             throw new NotImplementedException();
        //     }
        //     else
        //     {
        //         inferredType = Suffix switch
        //         {
        //             "l" => "long",
        //             "u" => "uint",
        //             "f" => "float",
        //             "d" => "double",
        //             _ => throw new NotImplementedException()
        //         };
        //     }
        }
    }
}
public class HexLiteral : NumberLiteral
{
    public override ISymbolType InferredType
    {
        get => inferredType;
        set => inferredType = value;
    }


    public HexLiteral() { }

    public HexLiteral(Match match, SymbolTable s)
    {
        Match = match;
        Value = Convert.ToUInt64(match.StringValue, 16);
    }
}
public class StringLiteral : ShaderLiteral
{
    public override ISymbolType InferredType { get => new ScalarType("string"); set => throw new NotImplementedException(); }

    public StringLiteral() { }

    public StringLiteral(Match match, SymbolTable s)
    {
        Match = match;
        Value = match.StringValue;
    }
}

public class BoolLiteral : ShaderLiteral
{
    public override ISymbolType InferredType { get => new ScalarType("bool"); set => throw new NotImplementedException(); }

    public BoolLiteral() { }

    public BoolLiteral(Match match, SymbolTable s)
    {
        Match = match;
        Value = (bool)match.Value;
    }
}


public class TypeNameLiteral : ShaderLiteral
{
    public string Name { get; set; }

    public TypeNameLiteral(Match m, SymbolTable s)
    {
        Name = m.StringValue;
    }
}

public class VariableNameLiteral : ShaderLiteral, IVariableCheck
{
    public string Name { get; set; }

    public override ISymbolType InferredType { get => inferredType ?? throw new NotImplementedException(); set => inferredType = value; }

    public VariableNameLiteral(string name)
    {
        Name = name;
    }

    public VariableNameLiteral(Match m, SymbolTable s)
    {
        Name = m.StringValue;
    }

    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        if (symbols.TryGetType(Name, out var type))
        {
            this.inferredType = type;
        }
        else throw new NotImplementedException();
    }

    public void CheckVariables(SymbolTable s)
    {
        if(!s.Any(x => x.ContainsKey(Name)))
            throw new Exception("Not a variable");
    }
    public override string ToString()
    {
        return $"{{ Variable : {Name} }}";
    }
}