namespace Stride.Shaders.Core;




public enum SymbolKind
{
    Shader,
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

public enum Storage : ushort
{
    None,
    Uniform,
    UniformConstant,
    Stream,
    Function,
    Generic,
}

public enum StreamIO : byte
{
    Input,
    Output
}

public record struct SymbolID(string Name, int IdRef, SymbolKind Kind, Storage Storage = 0);
public record struct StreamInfo(ushort EntryPoint, StreamIO Stream);
public record struct Symbol(SymbolID Id, SymbolType Type, object? Data = null);



public record struct MixinParentSymbol();
public record struct MixinChildSymbol();
public record struct StructSymbol();
public record struct MethodSymbol();
public record struct VariableSymbol();
public record struct ConstantSymbol();
public record struct ConstantGenericSymbol();
public record struct CompositionSymbol();
public record struct CBufferSymbol();
public record struct TBufferSymbol();
public record struct RGroupSymbol();