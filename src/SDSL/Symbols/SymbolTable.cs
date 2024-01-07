using System.Runtime.CompilerServices;
using Eto.Parse;
using SDSL.Symbols;

namespace SDSL.Parsing.AST.Shader.Symbols;

public partial class SymbolTable
{
    public Dictionary<string, SymbolType> Types { get; } = [];
    public VariableScope Variables;
    public Dictionary<string, MethodSymbol> Methods;
    public SymbolTable()
    {
        Variables = new(this);
        Methods = [];
    }

    public SymbolType ParseType(string type, Dictionary<string,string>? fields = null)
    {
        return type switch
        {
            string s when s.IsScalar() => Scalar(s),
            string s when s.IsVector() => Vector(s),
            string s when s.IsMatrix() => Matrix(s),
            string s when s.IsArray() => Array(s),
            string s when s.IsStruct() && fields != null => Struct(s, fields.ToDictionary(x => x.Key, x => ParseType(x.Value))),
            string s when s.IsStruct() && fields == null => Struct(s),
            _ => throw new NotImplementedException($"Type {type} cannot be parsed")
        };
    }

    public Scalar Scalar(string name)
    {
        if (Types.TryGetValue(name, out var t))
            return (Scalar)t;
        var symb = SymbolType.Scalar(name);
        Types[name] = symb;
        return symb;
    }
    public VectorSymbol Vector(string name)
    {
        if (Types.TryGetValue(name, out var t))
            return (VectorSymbol)t;
        var symb = SymbolType.Vector(Scalar(name[..^1]), name[^1] - '0');
        Types[name] = symb;
        return symb;
    }
    public VectorSymbol Vector(string baseType, int size)
    {
        if (Types.TryGetValue(baseType + size, out var t))
            return (VectorSymbol)t;
        var symb = SymbolType.Vector(Scalar(baseType),size);
        Types[baseType + size] = symb;
        return symb;
    }
    public VectorSymbol Vector(Scalar scalar, int size)
    {
        if (Types.TryGetValue(scalar.Name + size, out var t))
            return (VectorSymbol)t;
        var symb = SymbolType.Vector(scalar, size);
        Types[scalar.Name + size] = symb;
        return symb;
    }
    public MatrixSymbol Matrix(string name)
    {
        if (Types.TryGetValue(name, out var t))
            return (MatrixSymbol)t;
        var symb = SymbolType.Matrix(Vector(name[..^2]), name[^1] - '0');
        Types[name] = symb;
        return symb;
    }
    public ArraySymbol Array(string name)
    {
        if (Types.TryGetValue(name, out var t))
            return (ArraySymbol)t;
        var symb = SymbolType.Array(Scalar(name[..^2]), null);
        Types[name] = symb;
        return symb;
    }
    public StructSymbol Struct(string name, Dictionary<string, SymbolType>? fields = null)
    {
        if (Types.TryGetValue(name, out var t))
            return (StructSymbol)t;
        var symb = SymbolType.Struct(name, fields ?? []);
        Types[name] = symb;
        return symb;
    }
}

file static class StringTypeExtensions
{
    public static bool IsScalar(this string t)
    {
        return t switch
        {
            "byte" or "sbyte"
            or "short" or "ushort" or "half"
            or "int" or "uint" or "float"
            or "long" or "ulong" or "double" => true,
            _ => false
        };
    }
    public static bool IsVector(this string t)
    {
        return t switch
        {
            string v when v[..^1].IsScalar() && char.IsDigit(v[^1]) => true,
            _ => false
        };
    }
    public static bool IsMatrix(this string t)
    {
        return t switch
        {
            string v when v[..^2].IsVector() && v[^2] == 'x' && char.IsDigit(v[^1]) => true,
            _ => false
        };
    }
    public static bool IsArray(this string t)
    {
        return t switch
        {
            string v when v[..^2].Any(x => !(char.IsLetterOrDigit(x) || x == '_')) && v[^2..] == "[]" => true,
            _ => false
        };
    }
    public static bool IsStruct(this string t)
    {
        return t switch
        {
            string v when !v.IsArray() && !v.IsMatrix() && !v.IsVector() && !v.IsScalar() => true,
            _ => false
        };
    }

}
