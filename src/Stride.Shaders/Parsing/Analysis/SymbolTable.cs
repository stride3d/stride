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

    public SpirvContext Context { get; init; }

    public RootSymbolFrame RootSymbols { get; }
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

    public SymbolTable(SpirvContext context)
    {
        Context = context;
        RootSymbols = new(context);
        Push(RootSymbols);
    }

    public void Push() => CurrentSymbols.Add(new(Context));

    public void Push(SymbolFrame symbolFrame) => CurrentSymbols.Add(symbolFrame);

    public IExternalShaderLoader ShaderLoader { get; set; }

    public SymbolFrame? Pop()
    {
        var scope = CurrentSymbols?[^1];
        CurrentSymbols?.RemoveAt(CurrentSymbols.Count - 1);
        return scope;
    }

    public bool TryResolveSymbol(string name, out Symbol symbol)
    {

        for (int i = CurrentSymbols.Count - 1; i >= 0; i--)
            if (CurrentSymbols[i].TryGetValue(name, out symbol))
                return true;

        if (CurrentShader != null && CurrentShader.TryResolveSymbol(this, Context, name, out symbol))
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

        if (CurrentShader != null && CurrentShader.TryResolveSymbol(this, Context, name, out var symbol2))
            return symbol2;


        throw new NotImplementedException($"Cannot find symbol {name} in main context (current shader is {CurrentShader?.Name}");
    }
}