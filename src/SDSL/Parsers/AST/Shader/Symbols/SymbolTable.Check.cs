using Eto.Parse;
using SDSL.Parsing.AST.Shader.Analysis;

namespace SDSL.Parsing.AST.Shader.Symbols;


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

    public static SymbolType TokenizeScalar(string name)
        => SymbolType.Scalar(name);
    public static SymbolType Tokenize(Match m)
    {
        return (m.Name, m.HasMatches) switch
        {
            ("ReturnType", _) => Tokenize(m.Matches[0]),
            ("ValueTypes", true) => Tokenize(m.Matches[0]),
            ("ArrayTypes", true) => Tokenize(m.Matches[0]),
            ("ValueTypes", false) => SymbolType.Scalar(m.StringValue),
            ("ScalarType", false) => SymbolType.Scalar(m.StringValue),
            ("bool", _) => SymbolType.Scalar(m.StringValue),
            ("sbyte", _) => SymbolType.Scalar(m.StringValue),
            ("byte", _) => SymbolType.Scalar(m.StringValue),
            ("short", _) => SymbolType.Scalar(m.StringValue),
            ("int", _) => SymbolType.Scalar(m.StringValue),
            ("uint", _) => SymbolType.Scalar(m.StringValue),
            ("half", _) => SymbolType.Scalar(m.StringValue),
            ("float", _) => SymbolType.Scalar(m.StringValue),
            ("double", _) => SymbolType.Scalar(m.StringValue),
            ("BoolScalar", _) => SymbolType.Scalar(m.StringValue),
            ("SbyteScalar", _) => SymbolType.Scalar(m.StringValue),
            ("ByteScalar", _) => SymbolType.Scalar(m.StringValue),
            ("ShortScalar", _) => SymbolType.Scalar(m.StringValue),
            ("IntScalar", _) => SymbolType.Scalar(m.StringValue),
            ("UintScalar", _) => SymbolType.Scalar(m.StringValue),
            ("HalfScalar", _) => SymbolType.Scalar(m.StringValue),
            ("FloatScalar", _) => SymbolType.Scalar(m.StringValue),
            ("DoubleScalar", _) => SymbolType.Scalar(m.StringValue),
            ("BoolVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("SbyteVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("ByteVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("ShortVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("IntVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("UintVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("HalfVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("FloatVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("DoubleVec", _) => SymbolType.Vector(SymbolType.Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            _ => throw new NotImplementedException()
        };
    }

}