using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;



public abstract record SymbolType();
public sealed record MixinSymbol(ShaderClass Shader) : SymbolType();
public sealed record Scalar() : SymbolType();
public sealed record Vector(int Size) : SymbolType();
public sealed record Matrix(int Rows, int Columns) : SymbolType();
public sealed record Array(int Size) : SymbolType();
public sealed record Struct(Dictionary<string, SymbolType> Fields) : SymbolType();
public sealed record Buffer(SymbolType BaseType, int Size) : SymbolType();
public abstract record Texture(SymbolType BaseType) : SymbolType();
public sealed record Texture1D(SymbolType BaseType, int Size) : SymbolType();
public sealed record Texture2D(SymbolType BaseType, int Width, int Height) : SymbolType();
public sealed record Texture3D(SymbolType BaseType, int Width, int Height, int Depth) : SymbolType();