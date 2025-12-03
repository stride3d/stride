using Stride.Shaders.Parsing.Analysis;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Stride.Shaders.Core;

public class SymbolFrame()
{
    readonly Dictionary<string, Symbol> symbols = [];

    readonly List<ShaderSymbol> implicitShaders = [];

    public Symbol this[string name]
    {
        get => symbols[name];
        set => symbols[name] = value;
    }

    public void AddImplicitShader(ShaderSymbol shaderSymbol)
    {
        implicitShaders.Add(shaderSymbol);
    }

    public void Add(string name, Symbol symbol)
    {
        if (symbol.Type is FunctionType && TryGetValue(name, out var existingSymbol))
        {
            // If there is already a function symbol with same name, let's create or add to a group.
            if (existingSymbol.Type is FunctionType)
                existingSymbol = new Symbol(new(name, SymbolKind.MethodGroup, FunctionFlags: existingSymbol.Id.FunctionFlags), new FunctionGroupType(), 0, GroupMembers: [existingSymbol]);

            existingSymbol.GroupMembers = existingSymbol.GroupMembers.Add(symbol);

            symbols[name] = existingSymbol;
        }
        else
        {
            symbols.Add(name, symbol);
        }
    }

    public void Remove(string name)
        => symbols.Remove(name);
    public bool ContainsKey(string name) => symbols.ContainsKey(name);
    public bool ContainsValue(Symbol symbol) => symbols.ContainsValue(symbol);
    public bool TryGetValue(string name, out Symbol symbol)
    {
        if (symbols.TryGetValue(name, out symbol))
            return true;

        foreach (var implicitShader in implicitShaders)
        {
            if (implicitShader.TryResolveSymbol(name, out symbol))
                return true;
        }

        return false;
    }

    public Dictionary<string, Symbol>.Enumerator GetEnumerator() => symbols.GetEnumerator();
}

public sealed class RootSymbolFrame : SymbolFrame
{
}