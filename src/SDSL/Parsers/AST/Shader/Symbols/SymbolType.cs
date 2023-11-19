using System.Numerics;

namespace SDSL.Parsing.AST.Shader.Symbols;

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
        if(Cache.TryGetValue(name, out var t))
            return (Scalar)t;
        else
        {
            Cache[name] = new Scalar(name);
            return (Scalar)Cache[name];
        }
    }
    public static Vector Vector(Scalar baseType, int size)
    {
        var newName = baseType.Name + size;
        if (Cache.TryGetValue(newName, out var t))
            return (Vector)t;
        else
        {
            Cache[newName] = new Vector(newName, baseType, size);
            return (Vector)Cache[newName];
        }
    }
    public static Matrix Matrix(Vector baseType, int columns)
    {
        var newName = baseType.Name + "x" + columns;
        if (Cache.TryGetValue(newName, out var t))
            return (Matrix)t;
        else
        {
            Cache[newName] = new Matrix(newName, baseType, columns);
            return (Matrix)Cache[newName];
        }
    }
    public static Array Array(SymbolType baseType, int? size)
    {
        var newName = baseType.Name + "[]";
        if (Cache.TryGetValue(newName, out var t))
            return (Array)t;
        else
        {
            Cache[newName] = new Array(newName, baseType, size);
            return (Array)Cache[newName];
        }
    }
    public static Struct Struct(string typeName, Dictionary<string, SymbolType> fields)
    {
        if (Cache.TryGetValue(typeName, out var t))
            return (Struct)t;
        else
        {
            Cache[typeName] = new Struct(typeName, fields);
            return (Struct)Cache[typeName];
        }
    }
};

public record StringSymbol() : SymbolType("string");
public record Scalar(string Name) : SymbolType(Name);
public record Vector(string Name, Scalar BaseType, int Size) : SymbolType(Name);
public record Matrix(string Name, Vector BaseType, int Columns) : SymbolType(Name);
public record Array(string Name, SymbolType BaseType, int? Size) : SymbolType(Name);
public record Struct(string Name, Dictionary<string, SymbolType> Fields) : SymbolType(Name);