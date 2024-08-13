namespace Stride.Shaders.Parsing.SDSL.Analysis;



public abstract record SymbolType();
public record Scalar() : SymbolType();
public record Vector(int Size) : SymbolType();
public record Matrix(int Rows, int Columns) : SymbolType();
public record Array(int Size) : SymbolType();
public record Struct(Dictionary<string, SymbolType> Fields) : SymbolType();
public record Buffer(SymbolType BaseType) : SymbolType();
public record Texture(SymbolType BaseType) : SymbolType();