using System.Dynamic;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;



public abstract record SymbolType();

public sealed record Scalar(string TypeName) : SymbolType();
public sealed record Vector(Scalar BaseType, int Size) : SymbolType();
public sealed record Matrix(Scalar BaseType, int Rows, int Columns) : SymbolType();
public sealed record Array(SymbolType BaseType, int Size) : SymbolType();
public sealed record Struct(Dictionary<string, SymbolType> Fields) : SymbolType();
public sealed record Buffer(SymbolType BaseType, int Size) : SymbolType();


public abstract record Texture(SymbolType BaseType) : SymbolType();
public sealed record Texture1D(SymbolType BaseType, int Size) : SymbolType();
public sealed record Texture2D(SymbolType BaseType, int Width, int Height) : SymbolType();
public sealed record Texture3D(SymbolType BaseType, int Width, int Height, int Depth) : SymbolType();


public sealed record MixinSymbol(
    string Name, 
    List<Symbol> Components
) : SymbolType()
{
    public T Get<T>(string name)
        where T : SymbolType
    {
        foreach (var e in Components)
            if (e is T r && e.Name == name)
                return r;
        throw new ArgumentException($"{name} not found in Mixin {Name}");
    }
    public bool TryGet<T>(string name, out T value)
        where T : SymbolType
    {
        foreach (var e in Components)
            if (e is T r && e.Name == name)
            {
                value = r;
                return true;
            }
        value = null!;
        return false;
    }
}
