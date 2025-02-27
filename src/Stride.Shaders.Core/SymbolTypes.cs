using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic;

namespace Stride.Shaders.Core;



public abstract record SymbolType()
{
    public static bool TryGetNumeric(string name, out SymbolType? result)
    {
        if(Scalar.Types.TryGetValue(name, out var s))
        {
            result = s;
            return true;
        }
        else if(Vector.Types.TryGetValue(name, out var v))
        {
            result = v;
            return true;
        }
        else if(Matrix.Types.TryGetValue(name, out var m))
        {
            result = m;
            return true;
        }
        else if (name == "void")
        {
            result = Scalar.From("void");
            return true;
        }
        else
        {
            result = null;
            return true;
        }
    }
}

public sealed record Undefined(string TypeName) : SymbolType()
{
    public override string ToString()
    {
        return TypeName;
    }
}
public sealed partial record Scalar(string TypeName) : SymbolType()
{
    public override string ToString()
    {
        return TypeName;
    }
}
public sealed partial record Vector(Scalar BaseType, int Size) : SymbolType()
{    
    public override string ToString()
    {
        return $"{BaseType}{Size}";
    }
}
public sealed partial record Matrix(Scalar BaseType, int Rows, int Columns) : SymbolType()
{
    public override string ToString()
    {
        return $"{BaseType}{Rows}x{Columns}";
    }
}
public sealed record Array(SymbolType BaseType, int Size) : SymbolType()
{
    public override string ToString()
    {
        return $"{BaseType}[{Size}]";
    }
}
public sealed record Struct(string Name, Dictionary<string, SymbolType> Fields) : SymbolType()
{
    public override string ToString()
    {
        return $"{Name}{{{string.Join(", ", Fields.Select(x => $"{x.Value} {x.Key}"))}}}";
    }
}
public sealed record Buffer(SymbolType BaseType, int Size) : SymbolType()
{
    public override string ToString()
    {
        return $"Buffer<{BaseType}, {Size}>";
    }
}


public abstract record Texture(SymbolType BaseType) : SymbolType()
{
    public override string ToString()
    {
        return $"Texture<{BaseType}>";
    }
}
public sealed record Texture1D(SymbolType BaseType, int Size) : Texture(BaseType)
{
    public override string ToString()
    {
        return $"Texture<{BaseType}, {Size}>";
    }
}
public sealed record Texture2D(SymbolType BaseType, int Width, int Height) : Texture(BaseType)
{
    public override string ToString()
    {
        return $"Texture<{BaseType}, {Width}, {Height}>";
    }
}
public sealed record Texture3D(SymbolType BaseType, int Width, int Height, int Depth) : Texture(BaseType)
{
    public override string ToString()
    {
        return $"Texture<{BaseType}, {Width}, {Height}, {Depth}>";
    }
}


public sealed record BufferSymbol(string Name, List<Symbol> Symbols) : SymbolType;
public sealed record ParamsSymbol(string Name, List<Symbol> Symbols) : SymbolType;
public sealed record EffectSymbol(string Name, List<Symbol> Symbols) : SymbolType;
public sealed record ShaderSymbol(string Name, List<Symbol> Components) : SymbolType
{
    public Symbol Get(string name, SymbolKind kind)
    {
        foreach (var e in Components)
            if (e.Id.Kind == kind && e.Id.Name == name)
                return e;
        throw new ArgumentException($"{name} not found in Mixin {Name}");
    }
    public bool TryGet(string name, SymbolKind kind, out Symbol? value)
    {
        foreach (var e in Components)
            if (e.Id.Kind == kind && e.Id.Name == name)
            {
                value = e;
                return true;
            }
        value = null!;
        return false;
    }
}
