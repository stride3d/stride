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
    public bool ResolveArraySizes { get; set; } = true;
    public bool ResolveExternalTypes { get; set; } = true;

    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];

    /// <summary>
    /// Maps shader class names to their resolved definitions. Separate from DeclaredTypes
    /// because ShaderDefinition is not a SymbolType.
    /// </summary>
    public Dictionary<string, ShaderDefinition> DeclaredShaders { get; } = [];

    /// <summary>
    /// Maps SPIR-V IDs to resolved shader symbols. Separate from ReverseTypes to avoid mutating cached contexts.
    /// </summary>
    private readonly Dictionary<int, ShaderDefinition> loadedShaders = [];

    public ShaderDefinition? ResolveShader(int id) => loadedShaders.GetValueOrDefault(id);

    public ShaderDefinition? ResolveShader(ShaderSymbol symbol)
    {
        if (Context.Types.TryGetValue(symbol, out var id))
            return ResolveShader(id);
        // Fallback: look up by name in DeclaredShaders (ShaderDefinition is no longer a SymbolType,
        // so it may not have a corresponding entry in Context.Types during compilation)
        if (DeclaredShaders.TryGetValue(symbol.Name, out var result))
            return result;
        // Try with full generic class name (e.g. "LightPointGroup<3>")
        if (symbol.GenericArguments.Length > 0)
            return DeclaredShaders.GetValueOrDefault(symbol.ToClassName());
        return null;
    }

    public void RegisterLoadedShader(int id, ShaderDefinition shader) => loadedShaders[id] = shader;

    public SpirvContext Context { get; init; }

    public RootSymbolFrame RootSymbols { get; }
    public List<SemanticError> Errors { get; } = [];
    public List<SemanticError> Warnings { get; } = [];
    public List<SemanticError> Infos { get; } = [];

    // Used by Identifier.ResolveSymbol
    public SymbolFrame CurrentFrame => CurrentSymbols[^1];
    // Used by Identifier.ResolveSymbol
    public List<SymbolFrame> CurrentSymbols { get; } = new();

    // Only valid during compilation (not during ShaderMixin phase)
    public ShaderDefinition? CurrentShader { get; set; }
    public List<ShaderMacro> CurrentMacros { get; set; } = new();
    // Only valid during compilation (not during ShaderMixin phase)
    public List<ShaderClassInstantiation> InheritedShaders { get; } = new();

    public SymbolTable(SpirvContext context, IExternalShaderLoader shaderLoader)
    {
        Context = context;
        RootSymbols = new();
        Push(RootSymbols);
        ShaderLoader = shaderLoader;
    }

    public void Push() => CurrentSymbols.Add(new());

    public void Push(SymbolFrame symbolFrame) => CurrentSymbols.Add(symbolFrame);

    public IExternalShaderLoader ShaderLoader { get; }

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
                // Check function groups
                if (symbol2.Value.Type is FunctionGroupType)
                {
                    foreach (var symbol3 in symbol2.Value.GroupMembers)
                    {
                        if (symbol3.IdRef == id)
                        {
                            symbol = symbol3;
                            return true;
                        }
                    }
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

    public void AddWarning(SemanticError warning)
    {
        Warnings.Add(warning);
    }

    public void AddInfo(SemanticError info)
    {
        Infos.Add(info);
    }
}
