namespace Stride.Shaders.Core;


public readonly struct SymbolFrame
{
    readonly Dictionary<SymbolID, Symbol> symbols;

    public SymbolFrame()
    {
        symbols = [];
    }

    public Symbol this[string name, SymbolKind kind] => symbols[new(name, kind)];
    
    public void Add(SymbolID name, Symbol symbol)
        => symbols.Add(name, symbol);
    public void Add(string name, SymbolKind kind, SymbolType type)
        => symbols.Add(new(name, kind), new(new(name, kind), type));
    public bool TryAdd(string name, SymbolKind kind, SymbolType type)
        => symbols.TryAdd(new(name, kind), new(new(name, kind), type));
    public void Remove(string name, SymbolKind kind)
        => symbols.Remove(new(name, kind));
    public bool ContainsKey(SymbolID name) => symbols.ContainsKey(name);
    public bool ContainsValue(Symbol symbol) => symbols.ContainsValue(symbol);
    public bool TryGetValue(string name, SymbolKind kind, out Symbol symbol)
        => symbols.TryGetValue(new(name, kind), out symbol);

    public Dictionary<SymbolID, Symbol>.Enumerator GetEnumerator() => symbols.GetEnumerator();
}