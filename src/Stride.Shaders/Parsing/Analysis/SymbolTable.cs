using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Stride.Shaders.Parsing.Analysis;


public record struct SemanticErrors(TextLocation Location, string Message);

public partial class SymbolTable : ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];

    public RootSymbolFrame RootSymbols { get; } = new();
    public List<SemanticErrors> Errors { get; } = [];

    // Used by Identifier.ResolveSymbol
    public SymbolFrame CurrentFrame => CurrentSymbols[^1];
    // Used by Identifier.ResolveSymbol
    public List<SymbolFrame> CurrentSymbols { get; } = new();

    // Only valid during compilation (not during ShaderMixin phase)
    public LoadedShaderSymbol? CurrentShader { get; set; }
    public List<ShaderMacro> CurrentMacros { get; set; }
    // Only valid during compilation (not during ShaderMixin phase)
    public List<ShaderClassInstantiation> InheritedShaders { get; set; }

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

    public bool TryResolveSymbol(string name, out Symbol symbol)
    {

        for (int i = CurrentSymbols.Count - 1; i >= 0; i--)
            if (CurrentSymbols[i].TryGetValue(name, out symbol))
                return true;
        symbol = default;
        return false;
    }

    public Symbol ResolveSymbol(string name)
    {
        for (int i = CurrentSymbols.Count - 1; i >= 0; --i)
        {
            if (CurrentSymbols[i].TryGetValue(name, out var symbol))
            {
                return symbol;
            }
        }

        throw new NotImplementedException($"Cannot find symbol {name}.");
    }
}