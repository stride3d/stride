using System.Runtime.CompilerServices;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Spirv operand representation, used for parsing spirv.
/// </summary>
public ref struct SpvOperand
{
    public string? Name { get; init; }
    public OperandKind Kind { get; init; }
    public OperandQuantifier Quantifier { get; init; }
    public Span<int> Words { get; init; }
    public int Offset { get; init; }

    public SpvOperand(OperandKind kind, OperandQuantifier quantifier, Span<int> words, int idRefOffset = 0)
    {
        Kind = kind;
        Quantifier = quantifier;
        Words = words;
        Offset = idRefOffset;
    }

    public SpvOperand(string? name, OperandKind kind, OperandQuantifier quantifier, Span<int> words, int idRefOffset = 0)
    {
        Name = name;
        Kind = kind;
        Quantifier = quantifier;
        Words = words;
        Offset = idRefOffset;
    }

    public void ReplaceIdResult(int replacement)
    {
        if (Kind == OperandKind.IdResult && replacement > 0)
            Words[0] = replacement;
    }
    public T ToEnum<T>() where T : Enum
    {
        return Unsafe.As<int, T>(ref Words[0]);
    }
    public T To<T>() where T : struct, IFromSpirv<T>
    {
        if (Kind == OperandKind.IdRef && typeof(T) == typeof(IdRef))
        {
            var id = new IdRef(Words[0] + Offset);
            var result = Unsafe.As<IdRef, T>(ref id);
            return result;
        }
        return T.From(Words);
    }

    public override string ToString()
    {
        return Kind switch
        {
            OperandKind.LiteralString => To<LiteralString>().Value,
            OperandKind.IdRef => $"%{Words[0] + Offset}",
            OperandKind.IdResultType => $"%{Words[0] + Offset}",
            OperandKind.PairLiteralIntegerIdRef => $"{Words[0]} %{Words[0] + Offset}",
            OperandKind.MemoryAccess => $"{ToEnum<Stride.Shaders.Spirv.Specification.MemoryAccessMask>()}",
            OperandKind.MemoryModel => $"{ToEnum<Stride.Shaders.Spirv.Specification.MemoryModel>()}",
            OperandKind.MemorySemantics => $"{ToEnum<Stride.Shaders.Spirv.Specification.MemorySemanticsMask>()}",
            OperandKind.AccessQualifier => $"{ToEnum<Stride.Shaders.Spirv.Specification.AccessQualifier>()}",
            OperandKind.AddressingModel => $"{ToEnum<Stride.Shaders.Spirv.Specification.AddressingModel>()}",
            OperandKind.BuiltIn => $"{ToEnum<Stride.Shaders.Spirv.Specification.BuiltIn>()}",
            OperandKind.Capability => $"{ToEnum<Stride.Shaders.Spirv.Specification.Capability>()}",
            OperandKind.Decoration => $"{ToEnum<Stride.Shaders.Spirv.Specification.Decoration>()}",
            OperandKind.Dim => $"{ToEnum<Stride.Shaders.Spirv.Specification.Dim>()}",
            OperandKind.ExecutionMode => $"{ToEnum<Stride.Shaders.Spirv.Specification.ExecutionMode>()}",
            OperandKind.ExecutionModel => $"{ToEnum<Stride.Shaders.Spirv.Specification.ExecutionModel>()}",
            OperandKind.FPFastMathMode => $"{ToEnum<Stride.Shaders.Spirv.Specification.FPFastMathModeMask>()}",
            OperandKind.FPRoundingMode => $"{ToEnum<Stride.Shaders.Spirv.Specification.FPRoundingMode>()}",
            OperandKind.FragmentShadingRate => $"{ToEnum<Stride.Shaders.Spirv.Specification.FragmentShadingRateMask>()}",
            OperandKind.FunctionControl => $"{ToEnum<Stride.Shaders.Spirv.Specification.FunctionControlMask>()}",
            OperandKind.FunctionParameterAttribute => $"{ToEnum<Stride.Shaders.Spirv.Specification.FunctionParameterAttribute>()}",
            OperandKind.GroupOperation => $"{ToEnum<Stride.Shaders.Spirv.Specification.GroupOperation>()}",
            OperandKind.ImageChannelDataType => $"{ToEnum<Stride.Shaders.Spirv.Specification.ImageChannelDataType>()}",
            OperandKind.ImageChannelOrder => $"{ToEnum<Stride.Shaders.Spirv.Specification.ImageChannelOrder>()}",
            OperandKind.ImageFormat => $"{ToEnum<Stride.Shaders.Spirv.Specification.ImageFormat>()}",
            OperandKind.ImageOperands => $"{ToEnum<Stride.Shaders.Spirv.Specification.ImageOperandsMask>()}",
            OperandKind.KernelEnqueueFlags => $"{ToEnum<Stride.Shaders.Spirv.Specification.KernelEnqueueFlags>()}",
            OperandKind.KernelProfilingInfo => $"{ToEnum<Stride.Shaders.Spirv.Specification.KernelProfilingInfoMask>()}",
            OperandKind.LinkageType => $"{ToEnum<Stride.Shaders.Spirv.Specification.LinkageType>()}",
            OperandKind.None => "",
            _ => Words[0].ToString()
        };
    }
}