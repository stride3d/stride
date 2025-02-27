namespace Stride.Shaders.Core;




public enum SymbolKind
{
    MixinParent,
    MixinChild,
    Struct,
    Method,
    Variable,
    Constant,
    ConstantGeneric,
    Composition,
    CBuffer,
    TBuffer,
    RGroup
}

public record struct SymbolID(string Name, SymbolKind Kind);
public record struct Symbol(SymbolID Id, SymbolType Type);