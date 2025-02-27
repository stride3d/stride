namespace Stride.Shaders.Core;

public interface ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; }
    public SymbolFrame RootSymbols { get; }
}
