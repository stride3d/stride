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

public readonly struct SymbolType
{

    public static SymbolType Void => new(null, "void", SymbolQuantifier.Void);

    public SymbolTable TypeTable { get; init; }
    public string Name { get; init; }
    public SymbolQuantifier Quantifier { get; init; }
    public Vector3? Size { get; init; }
    public SortedList<string, string>? Fields { get; init; }
    public SortedList<string, string?>? Semantics { get; init; }


    public SymbolType(SymbolTable table, string name, SymbolQuantifier quantifier, Vector3? size = null, SortedList<string, string>? fields = null, SortedList<string, string?>? semantics = null)
    {
        TypeTable = table;
        Name = name;
        Quantifier = quantifier;
        Size = size;
        Fields = fields;
        Semantics = semantics;
    }
    public static bool operator !=(SymbolType lhs, SymbolType rhs)
    {
        return 
            lhs.TypeTable != rhs.TypeTable
            || lhs.Name != rhs.Name
            || lhs.Size != rhs.Size;
    }
    public static bool operator ==(SymbolType lhs, SymbolType rhs)
    {
        return 
            lhs.TypeTable == rhs.TypeTable
            || lhs.Name == rhs.Name
            || lhs.Size == rhs.Size;
    }

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

public class SymbolTable
{
    public Dictionary<string, SymbolType> SymbolTypes;

    public SymbolType this[string index]
    {
        get => SymbolTypes[index];
        set => SymbolTypes[index] = value;
    }

    public SymbolTable()
    {
        SymbolTypes = new();
    }

    public bool TryGet(string index, out SymbolType t)
    {
        return SymbolTypes.TryGetValue(index, out t);
    }
}

file static class StringAccessorExtensions
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

