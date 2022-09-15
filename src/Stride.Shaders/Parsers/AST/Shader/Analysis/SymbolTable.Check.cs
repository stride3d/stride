using Eto.Parse;

namespace Stride.Shaders.Parsing.AST.Shader.Analysis;


public partial class SymbolTable
{
    public void CheckVar(Statement s)
    {
        if(s is IVariableCheck v)
            v.CheckVariables(this);
    }

    public ISymbolType TokenizeScalar(string name)
    {
        return name switch
        {
            "byte" => new ScalarType("byte"),
            "sbyte" => new ScalarType("byte"),
            "short" => new ScalarType("short"),
            "half" => new ScalarType("half"),
            "int" => new ScalarType("int"),
            "uint" => new ScalarType("uint"),
            "float" => new ScalarType("float"),
            "double" => new ScalarType("double"), 
            _ => throw new NotImplementedException()
        };
    }
    public ISymbolType Tokenize(Match m)
    {
        return (m.Name, m.HasMatches) switch 
        {
            ("ReturnType", _) => Tokenize(m.Matches[0]),
            ("ValueTypes", true) => Tokenize(m.Matches[0]),
            ("ArrayTypes", true) => Tokenize(m.Matches[0]),
            ("ValueTypes", false) => new ScalarType(m.StringValue),
            ("ScalarType", false) => new ScalarType(m.StringValue),
            ("bool", _) => new ScalarType(m.StringValue),
            ("sbyte", _) => new ScalarType(m.StringValue),
            ("byte", _) => new ScalarType(m.StringValue),
            ("short", _) => new ScalarType(m.StringValue),
            ("int", _) => new ScalarType(m.StringValue),
            ("uint", _) => new ScalarType(m.StringValue),
            ("half", _) => new ScalarType(m.StringValue),
            ("float", _) => new ScalarType(m.StringValue),
            ("double", _) => new ScalarType(m.StringValue),
            ("BoolScalar", _) => new ScalarType(m.StringValue),
            ("SbyteScalar", _) => new ScalarType(m.StringValue),
            ("ByteScalar", _) => new ScalarType(m.StringValue),
            ("ShortScalar", _) => new ScalarType(m.StringValue),
            ("IntScalar", _) => new ScalarType(m.StringValue),
            ("UintScalar", _) => new ScalarType(m.StringValue),
            ("HalfScalar", _) => new ScalarType(m.StringValue),
            ("FloatScalar", _) => new ScalarType(m.StringValue),
            ("DoubleScalar", _) => new ScalarType(m.StringValue),
            ("BoolVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("SbyteVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("ByteVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("ShortVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("IntVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("UintVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("HalfVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("FloatVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            ("DoubleVec", _) => new VectorType(m["Size1"].StringValue, PushType(m["ScalarType"].StringValue,m["ScalarType"])),
            _ => throw new NotImplementedException()
        };
    }
}