using Stride.Shaders.Core;

namespace Stride.Shaders.Parsing.Analysis;


public record struct SemanticErrors(TextLocation Location, string Message);

// TODO : make sure that symbol checking is separated based on symbol kind
public partial class SymbolTable : ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];
    public SymbolFrame CurrentTable => Symbols[^1];
    public SymbolFrame RootSymbols => Symbols[0];
    public List<SymbolFrame> Symbols { get; } = [new()];

    public List<SemanticErrors> Errors { get; } = [];


    public void Push() => Symbols.Add(new());
    public SymbolFrame Pop()
    {
        var scope = Symbols[^1];
        Symbols.Remove(scope);
        return scope;
    }

    public void Import(ISymbolProvider symbols)
    {
        foreach (var (name, type) in symbols.DeclaredTypes)
            DeclaredTypes.TryAdd(name, type);
        foreach (var (name, symbol) in symbols.RootSymbols)
            RootSymbols.Add(name, symbol);
    }

    public bool TryFind(string name, SymbolKind kind, out Symbol symbol)
    {
        for(int i = 0; i < Symbols.Count; i--)
            if(Symbols[i].TryGetValue(name, kind, out symbol))
                return true;
        symbol = default;
        return false;
    }

    
}