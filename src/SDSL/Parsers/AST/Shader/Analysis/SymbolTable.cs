using System.Numerics;
using Eto.Parse;

namespace SDSL.Parsing.AST.Shader.Analysis;

public enum SymbolQuantifier
{
    Void,
    Scalar,
    Vector,
    Matrix,
    Struct,
    Array,
}

public record struct SymbolType(SymbolTable TypeTable, string Name, SymbolQuantifier Quantifier, Vector3? Size = null, SortedList<string, string>? Fields = null, SortedList<string, string>? Semantics = null)
{

    public static SymbolType Void => new(null, "void", SymbolQuantifier.Void);

    public bool IsAccessorValid(string accessor)
    {
        return
            Quantifier == SymbolQuantifier.Struct && Fields != null && Fields.ContainsKey(accessor)
            || Quantifier == SymbolQuantifier.Vector && accessor.IsVectorSwizzling()
            || Quantifier == SymbolQuantifier.Matrix && accessor.IsMatrixSwizzling();
    }
    public bool IsIndexingValid(string index)
    {
        return Quantifier == SymbolQuantifier.Array && int.TryParse(index, out var v) && v > 0;
    }
    public bool TryAccessType(string accessor, out SymbolType typeOfAccessed)
    {
        typeOfAccessed = new(TypeTable, "void", SymbolQuantifier.Void);
        return Fields != null && TypeTable.TryGet(Fields[accessor], out typeOfAccessed);
    }
}

public record struct VariableSymbol(SymbolTable Table, string Name, string TypeName);
public record struct MethodSymbol(SymbolTable Table, string Name, string TypeName);

public partial class SymbolTable
{
    public Dictionary<string, SymbolType> SymbolTypes;
    public VariableScope Variables;
    public Dictionary<string, MethodSymbol> Methods;

    public SymbolType this[string index]
    {
        get => SymbolTypes[index];
        set => SymbolTypes[index] = value;
    }

    public SymbolTable()
    {
        SymbolTypes = new();
        Variables = new(this);
        Methods = new();
    }

    public bool TryGet(string index, out SymbolType t)
    {
        return SymbolTypes.TryGetValue(index, out t);
    }

    public SymbolType PushScalarType(string name)
    {
        SymbolTypes.Add(name, new SymbolType(this, name, SymbolQuantifier.Scalar));
        return SymbolTypes[name];
    }
    public SymbolType PushType(string name, Eto.Parse.Match type)
    {
        SymbolTypes[name] = Tokenize(type);
        return SymbolTypes[name];
    }
}

internal static class StringAccessorExtensions
{
    public static bool IsVectorSwizzling(this string s)
    {
        Span<char> swizzles = stackalloc char[] { 'x', 'y', 'z', 'w', 'r', 'g', 'b', 'a' };
        // foreach(var e in s)
        //     if(!swizzles.Contains(e));
        return s.Length == 1 && swizzles.Contains(s[0]);
    }
    public static bool IsMatrixSwizzling(this string s)
    {
        return s.Length == 3 && s[0] == '_' && char.IsDigit(s[1]) && char.IsDigit(s[2]);
    }
}

