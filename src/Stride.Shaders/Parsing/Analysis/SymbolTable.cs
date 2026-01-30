using System.Diagnostics.CodeAnalysis;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Stride.Shaders.Parsing.Analysis;


public record struct SemanticError(TextLocation Location, string Message)
{
    override public string ToString() => $"{Location}: {Message}";
}

public partial class SymbolTable : ISymbolProvider
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];

    public SpirvContext Context { get; init; }

    public RootSymbolFrame RootSymbols { get; }
    public List<SemanticError> Errors { get; } = [];

    // Used by Identifier.ResolveSymbol
    public SymbolFrame CurrentFrame => CurrentSymbols[^1];
    // Used by Identifier.ResolveSymbol
    public List<SymbolFrame> CurrentSymbols { get; } = new();

    // Only valid during compilation (not during ShaderMixin phase)
    public LoadedShaderSymbol? CurrentShader { get; set; }
    public List<ShaderMacro> CurrentMacros { get; set; }
    // Only valid during compilation (not during ShaderMixin phase)
    public List<ShaderClassInstantiation> InheritedShaders { get; set; }

    public SymbolTable(SpirvContext context)
    {
        Context = context;
        RootSymbols = new();
        Push(RootSymbols);
    }

    public void Push() => CurrentSymbols.Add(new());

    public void Push(SymbolFrame symbolFrame) => CurrentSymbols.Add(symbolFrame);

    public IExternalShaderLoader ShaderLoader { get; set; }

    public SymbolFrame Pop()
    {
        var scope = CurrentSymbols[^1];
        CurrentSymbols.RemoveAt(CurrentSymbols.Count - 1);
        return scope;
    }

    public bool TryResolveSymbol(string name, [MaybeNullWhen(false)] out Symbol symbol)
    {
        for (int i = CurrentSymbols.Count - 1; i >= 0; i--)
            if (CurrentSymbols[i].TryGetValue(name, out symbol))
                return true;

        if (CurrentShader != null && CurrentShader.TryResolveSymbol(name, out symbol))
            return true;

        symbol = null;
        return false;
    }

    public bool TryResolveSymbol(int id, [MaybeNullWhen(false)] out Symbol symbol)
    {
        for (int i = CurrentSymbols.Count - 1; i >= 0; --i)
        {
            foreach (var symbol2 in CurrentSymbols[i])
            {
                if (symbol2.Value.IdRef == id)
                {
                    symbol = symbol2.Value;
                    return true;
                }
            }
        }

        if (CurrentShader != null && CurrentShader.TryResolveSymbol(id, out symbol))
            return true;

        symbol = null;
        return false;
    }
    
    public Symbol ResolveSymbol(int id)
    {
        if (!TryResolveSymbol(id, out var symbol))
            throw new NotImplementedException($"Cannot find symbol with ID {id} in main context (current shader is {CurrentShader?.Name}");
        return symbol;
    }

    public Symbol ResolveSymbol(string name)
    {
        if (!TryResolveSymbol(name, out var symbol))
            throw new NotImplementedException($"Cannot find symbol {name} in main context (current shader is {CurrentShader?.Name}");
        return symbol;
    }

    public void AddError(SemanticError error)
    {
        Errors.Add(error);
    }
}