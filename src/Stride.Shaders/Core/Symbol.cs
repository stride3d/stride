using System.Collections.Immutable;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Core;




public enum SymbolKind
{
    Shader,
    Struct,
    Method,
    MethodGroup,
    Variable,
    Constant,
    ConstantGeneric,
    Composition,
    CBuffer,
    TBuffer,
    RGroup,
    SamplerState,
    SamplerComparisonState,
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

public record struct SymbolID(string Name, SymbolKind Kind, Storage Storage = 0, bool IsStage = false);
public record struct StreamInfo(ushort EntryPoint, StreamIO Stream);

public record struct MethodSymbolDefaultParameters(SpirvContext SourceContext, int[] DefaultValues);

public record struct ExternalConstant(SpirvContext SourceContext, int ConstantId);

/// <summary>
/// Defines a symbol.
/// </summary>
/// <param name="GroupMembers">Only used for specific <see cref="Type"/> such as <see cref="FunctionGroupType"/></param>
public record struct Symbol(SymbolID Id, SymbolType Type, int IdRef, int? AccessChain = null, SymbolType MemberAccessWithImplicitThis = null, ImmutableArray<Symbol> GroupMembers = default, MethodSymbolDefaultParameters? MethodDefaultParameters = null, ExternalConstant? ExternalConstant = null, LoadedShaderSymbol? OwnerType = null);



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