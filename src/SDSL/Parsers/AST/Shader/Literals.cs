using Eto.Parse;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.Parsing.AST.Shader.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDSL.Parsing.AST.Shader;


public class ShaderLiteral : Expression
{
    public override SymbolType? InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public object Value { get; set; }
}

public class NumberLiteral : ShaderLiteral
{
    public bool Negative { get; set; } = false;
    public string? Suffix { get; set; }
    public override SymbolType? InferredType
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
            InferredType = Value switch
            {
                int => s.Scalar("int"),
                long => s.Scalar("int"),
                float => s.Scalar("float"),
                double => s.Scalar("float"),
                _ => throw new NotImplementedException()
            };
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
                InferredType = Suffix switch
                {
                    "l" => s.Scalar("long"),
                    "d" => s.Scalar("double"),
                    "f" => s.Scalar("float"),
                    "u" => s.Scalar("uint"),
                    "L" => s.Scalar("long"),
                    "D" => s.Scalar("double"),
                    "F" => s.Scalar("float"),
                    "U" => s.Scalar("uint"),
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
    public override void TypeCheck(SymbolTable symbols, in SymbolType? expected)
    {
        if (!expected.Equals(InferredType))
        {
            inferredType = (InferredType, expected) switch
            {
                (Scalar s, Scalar { Name : "void"}) => s,
                (Scalar { Name: "float" }, Scalar { Name: "int" or "half" or "double" }) => expected,
                (Scalar { Name: "int" }, Scalar { Name: "byte" or "sbyte" or "short" or "ushort" or "uint" or "long" or "ulong" or "float" or "double" }) => expected,
                _ => throw new Exception($"cannot implictely cast {inferredType} to {expected}")
            };
        }
    }
    public override string ToString()
    {
        return new StringBuilder().Append(InferredType.ToString()).Append('(').Append(Value.ToString()).Append(')').ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is NumberLiteral literal &&
               EqualityComparer<SymbolType?>.Default.Equals(inferredType, literal.inferredType) &&
               EqualityComparer<object>.Default.Equals(Value, literal.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(inferredType, Value);
    }
}
public class HexLiteral : NumberLiteral
{
    public override SymbolType? InferredType
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
    public override SymbolType? InferredType { get => SymbolType.String(); set => throw new NotImplementedException(); }

    public StringLiteral() { }

    public StringLiteral(Match match, SymbolTable s)
    {
        Match = match;
        Value = match.StringValue;
    }
}

public class BoolLiteral : ShaderLiteral
{
    public override SymbolType? InferredType { get => SymbolType.Scalar("bool"); set => throw new NotImplementedException(); }

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

    public override SymbolType? InferredType { get => inferredType ?? throw new NotImplementedException(); set => inferredType = value; }

    public VariableNameLiteral(string name)
    {
        Name = name;
    }

    public VariableNameLiteral(Match m, SymbolTable s)
    {
        Name = m.StringValue;
    }

    public override void TypeCheck(SymbolTable symbols, in SymbolType? expected)
    {
        if(symbols.Variables.TryGetVariable(Name, out var variable))
        {
            if(!(variable.Type == expected))
                throw new Exception("Type is not matching");
        }
        else throw new Exception($"Use of undeclared variable \"{Name}\"");
    }

    public void CheckVariables(SymbolTable s)
    {
        if (!s.Variables.IsDeclared(Name))
            throw new Exception("Not a variable");
    }
    public override string ToString()
    {
        return $"{{ Variable : {Name} }}";
    }
}