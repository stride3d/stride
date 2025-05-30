namespace Stride.Shaders.Core;

public class SymbolFrame()
{
    readonly Dictionary<string, Symbol> symbols = [];

    public Symbol this[string name]
    {
        get => symbols[name];
        set => symbols[name] = value;
    }

    public void Add(string name, Symbol symbol)
        => symbols.Add(name, symbol);
    public void Remove(string name)
        => symbols.Remove(name);
    public bool ContainsKey(string name) => symbols.ContainsKey(name);
    public bool ContainsValue(Symbol symbol) => symbols.ContainsValue(symbol);
    public bool TryGetValue(string name, out Symbol symbol)
        => symbols.TryGetValue(name, out symbol);

    public Dictionary<string, Symbol>.Enumerator GetEnumerator() => symbols.GetEnumerator();
}

public sealed class RootSymbolFrame : SymbolFrame
{
}