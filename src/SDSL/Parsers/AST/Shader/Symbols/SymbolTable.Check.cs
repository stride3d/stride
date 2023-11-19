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

    public SymbolType TokenizeScalar(string name)
        => Scalar(name);
    public SymbolType Tokenize(Match m)
    {
        return (m.Name, m.HasMatches) switch
        {
            ("ReturnType", _) => Tokenize(m.Matches[0]),
            ("ValueTypes", true) => Tokenize(m.Matches[0]),
            ("ArrayTypes", true) => Tokenize(m.Matches[0]),
            ("ValueTypes", false) => Scalar(m.StringValue),
            ("ScalarType", false) => Scalar(m.StringValue),
            ("bool", _) => Scalar(m.StringValue),
            ("sbyte", _) => Scalar(m.StringValue),
            ("byte", _) => Scalar(m.StringValue),
            ("short", _) => Scalar(m.StringValue),
            ("int", _) => Scalar(m.StringValue),
            ("uint", _) => Scalar(m.StringValue),
            ("half", _) => Scalar(m.StringValue),
            ("float", _) => Scalar(m.StringValue),
            ("double", _) => Scalar(m.StringValue),
            ("BoolScalar", _) => Scalar(m.StringValue),
            ("SbyteScalar", _) => Scalar(m.StringValue),
            ("ByteScalar", _) => Scalar(m.StringValue),
            ("ShortScalar", _) => Scalar(m.StringValue),
            ("IntScalar", _) => Scalar(m.StringValue),
            ("UintScalar", _) => Scalar(m.StringValue),
            ("HalfScalar", _) => Scalar(m.StringValue),
            ("FloatScalar", _) => Scalar(m.StringValue),
            ("DoubleScalar", _) => Scalar(m.StringValue),
            ("BoolVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("SbyteVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("ByteVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("ShortVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("IntVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("UintVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("HalfVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("FloatVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            ("DoubleVec", _) => Vector(Scalar(m["ScalarType"].StringValue),(int)m["Size1"].Value),
            _ => throw new NotImplementedException()
        };
    }

}