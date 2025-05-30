namespace Stride.Shaders.Core;

public class SymbolFrame()
{
    readonly Dictionary<SymbolID, Symbol> symbols = [];

    public Symbol this[string name, SymbolKind kind]
    {
        get => symbols[new(name, kind)];
        set => symbols[new(name, kind)] = value;
    }
    public Symbol this[SymbolID symbolID]
    {
        get => symbols[symbolID];
        set => symbols[symbolID] = value;
    }

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
    public bool TryGetValue(string name, SymbolKind kind, Storage storage, out Symbol symbol)
        => symbols.TryGetValue(new(name, kind, storage), out symbol);

    public Dictionary<SymbolID, Symbol>.Enumerator GetEnumerator() => symbols.GetEnumerator();
}

public sealed class RootSymbolFrame : SymbolFrame
{
}