using System.Numerics;

namespace SDSL.Symbols;

public abstract record SymbolType(string Name)
{
    static Dictionary<string, SymbolType> Cache = new();
    public static SymbolType Void { get; } = new Scalar("void");

    public static StringSymbol String()
    {
        if (Cache.TryGetValue("string", out var t))
            return (StringSymbol)t;
        else
        {
            Cache["string"] = new StringSymbol();
            return (StringSymbol)Cache["string"];
        }
    }
    public static Scalar Scalar(string name)
    {
        if (Cache.TryGetValue(name, out var t))
            return (Scalar)t;
        else
        {
            Cache[name] = new Scalar(name);
            return (Scalar)Cache[name];
        }
    }
    public static VectorSymbol Vector(Scalar baseType, int size)
    {
        var newName = baseType.Name + size;
        if (Cache.TryGetValue(newName, out var t))
            return (VectorSymbol)t;
        else
        {
            Cache[newName] = new VectorSymbol(newName, baseType, size);
            return (VectorSymbol)Cache[newName];
        }
    }
    public static MatrixSymbol Matrix(VectorSymbol baseType, int columns)
    {
        var newName = baseType.Name + "x" + columns;
        if (Cache.TryGetValue(newName, out var t))
            return (MatrixSymbol)t;
        else
        {
            Cache[newName] = new MatrixSymbol(newName, baseType, columns);
            return (MatrixSymbol)Cache[newName];
        }
    }
    public static ArraySymbol Array(SymbolType baseType, int? size)
    {
        var newName = baseType.Name + "[]";
        if (Cache.TryGetValue(newName, out var t))
            return (ArraySymbol)t;
        else
        {
            Cache[newName] = new ArraySymbol(newName, baseType, size);
            return (ArraySymbol)Cache[newName];
        }
    }
    public static StructSymbol Struct(string typeName, Dictionary<string, SymbolType> fields)
    {
        if (Cache.TryGetValue(typeName, out var t))
            return (StructSymbol)t;
        else
        {
            Cache[typeName] = new StructSymbol(typeName, fields);
            return (StructSymbol)Cache[typeName];
        }
    }
};

public record StringSymbol() : SymbolType("string");
public record Scalar(string Name) : SymbolType(Name);
public record VectorSymbol(string Name, Scalar BaseType, int Size) : SymbolType(Name);
public record MatrixSymbol(string Name, VectorSymbol BaseType, int Columns) : SymbolType(Name);
public record ArraySymbol(string Name, SymbolType BaseType, int? Size) : SymbolType(Name);
public record StructSymbol(string Name, Dictionary<string, SymbolType> Fields) : SymbolType(Name);