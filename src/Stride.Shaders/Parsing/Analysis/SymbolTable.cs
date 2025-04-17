using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.Analysis;


public record struct SemanticErrors(TextLocation Location, string Message);

public partial class SymbolTable : ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];
    public SymbolFrame CurrentFrame => CurrentFunctionSymbols[^1];
    public RootSymbolFrame RootSymbols { get; } = new();
    public SymbolFrame Streams { get; } = new();
    public SortedList<string, List<SymbolFrame>> FunctionSymbols { get; } = [];

    public List<SemanticErrors> Errors { get; } = [];

    public List<SymbolFrame>? CurrentFunctionSymbols { get; internal set; }

    public void Push() => CurrentFunctionSymbols?.Add(new());

    public SymbolFrame? Pop()
    {
        var scope = CurrentFunctionSymbols?[^1];
        CurrentFunctionSymbols?.RemoveAt(CurrentFunctionSymbols.Count - 1);
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

        if(CurrentFunctionSymbols is null)
            return RootSymbols.TryGetValue(name, kind, out symbol);
        
        for (int i = CurrentFunctionSymbols.Count - 1; i >= 0; i--)
            if (CurrentFunctionSymbols[i].TryGetValue(name, kind, out symbol))
                return true;
        return RootSymbols.TryGetValue(name, kind, out symbol);
    }


}