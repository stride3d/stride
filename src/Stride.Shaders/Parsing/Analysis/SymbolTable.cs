using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Parsing.Analysis;


public record struct SemanticErrors(TextLocation Location, string Message);

public partial class SymbolTable : ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];
    public SymbolFrame CurrentFrame => CurrentSymbols[^1];
    public RootSymbolFrame RootSymbols { get; } = new();
    public SymbolFrame Streams { get; } = new();

    public List<SemanticErrors> Errors { get; } = [];

    public List<SymbolFrame> CurrentSymbols { get; } = new();

    public SymbolTable()
    {
        Push(RootSymbols);
    }

    public void Push() => CurrentSymbols.Add(new());

    public void Push(SymbolFrame symbolFrame) => CurrentSymbols.Add(symbolFrame);

    public IExternalShaderLoader ShaderLoader { get; set; }

    public SymbolFrame? Pop()
    {
        var scope = CurrentSymbols?[^1];
        CurrentSymbols?.RemoveAt(CurrentSymbols.Count - 1);
        return scope;
    }

    public void Import(ISymbolProvider symbols)
    {
        foreach (var (name, type) in symbols.DeclaredTypes)
            DeclaredTypes.TryAdd(name, type);
        foreach (var (name, symbol) in symbols.RootSymbols)
            RootSymbols.Add(name, symbol);
    }

    public bool TryFind(string name, out Symbol symbol)
    {

        if (CurrentSymbols is not null)
            for (int i = CurrentSymbols.Count - 1; i >= 0; i--)
                if (CurrentSymbols[i].TryGetValue(name, out symbol))
                    return true;
        return RootSymbols.TryGetValue(name, out symbol);
    }


}