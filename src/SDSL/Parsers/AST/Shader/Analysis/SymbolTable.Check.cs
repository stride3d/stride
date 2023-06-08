using Eto.Parse;

namespace SDSL.Parsing.AST.Shader.Analysis;


public partial class SymbolTable
{
    public void CheckVar(Statement s)
    {
        if (s is IVariableCheck v)
        {
            v.CheckVariables(this);
        }
        s.TypeCheck(this, SymbolType.Void);
    }

    public SymbolType TokenizeScalar(string name)
    {
        return name switch
        {
            "byte" => new(this, "byte", SymbolQuantifier.Scalar),
            "sbyte" => new(this, "byte", SymbolQuantifier.Scalar),
            "short" => new(this, "short", SymbolQuantifier.Scalar),
            "half" => new(this, "half", SymbolQuantifier.Scalar),
            "int" => new(this, "int", SymbolQuantifier.Scalar),
            "uint" => new(this, "uint", SymbolQuantifier.Scalar),
            "float" => new(this, "float", SymbolQuantifier.Scalar),
            "double" => new(this, "double", SymbolQuantifier.Scalar),
            _ => throw new NotImplementedException()
        };
    }
    public SymbolType Tokenize(Match m)
    {
        return (m.Name, m.HasMatches) switch
        {
            ("ReturnType", _) => Tokenize(m.Matches[0]),
            ("ValueTypes", true) => Tokenize(m.Matches[0]),
            ("ArrayTypes", true) => Tokenize(m.Matches[0]),
            ("ValueTypes", false) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("ScalarType", false) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("bool", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("sbyte", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("byte", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("short", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("int", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("uint", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("half", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("float", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("double", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("BoolScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("SbyteScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("ByteScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("ShortScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("IntScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("UintScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("HalfScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("FloatScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("DoubleScalar", _) => new(this, m.StringValue, SymbolQuantifier.Scalar),
            ("BoolVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("SbyteVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("ByteVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("ShortVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("IntVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("UintVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("HalfVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("FloatVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            ("DoubleVec", _) => new(this, m["ScalarType"].StringValue, SymbolQuantifier.Vector, new((int)m["Size1"].Value)),
            _ => throw new NotImplementedException()
        };
    }

}