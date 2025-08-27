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
            OperandKind.MemoryAccess => $"{ToEnum<Spv.Specification.MemoryAccessMask>()}",
            OperandKind.MemoryModel => $"{ToEnum<Spv.Specification.MemoryModel>()}",
            OperandKind.MemorySemantics => $"{ToEnum<Spv.Specification.MemorySemanticsMask>()}",
            OperandKind.AccessQualifier => $"{ToEnum<Spv.Specification.AccessQualifier>()}",
            OperandKind.AddressingModel => $"{ToEnum<Spv.Specification.AddressingModel>()}",
            OperandKind.BuiltIn => $"{ToEnum<Spv.Specification.BuiltIn>()}",
            OperandKind.Capability => $"{ToEnum<Spv.Specification.Capability>()}",
            OperandKind.Decoration => $"{ToEnum<Spv.Specification.Decoration>()}",
            OperandKind.Dim => $"{ToEnum<Spv.Specification.Dim>()}",
            OperandKind.ExecutionMode => $"{ToEnum<Spv.Specification.ExecutionMode>()}",
            OperandKind.ExecutionModel => $"{ToEnum<Spv.Specification.ExecutionModel>()}",
            OperandKind.FPFastMathMode => $"{ToEnum<Spv.Specification.FPFastMathModeMask>()}",
            OperandKind.FPRoundingMode => $"{ToEnum<Spv.Specification.FPRoundingMode>()}",
            OperandKind.FragmentShadingRate => $"{ToEnum<Spv.Specification.FragmentShadingRateMask>()}",
            OperandKind.FunctionControl => $"{ToEnum<Spv.Specification.FunctionControlMask>()}",
            OperandKind.FunctionParameterAttribute => $"{ToEnum<Spv.Specification.FunctionParameterAttribute>()}",
            OperandKind.GroupOperation => $"{ToEnum<Spv.Specification.GroupOperation>()}",
            OperandKind.ImageChannelDataType => $"{ToEnum<Spv.Specification.ImageChannelDataType>()}",
            OperandKind.ImageChannelOrder => $"{ToEnum<Spv.Specification.ImageChannelOrder>()}",
            OperandKind.ImageFormat => $"{ToEnum<Spv.Specification.ImageFormat>()}",
            OperandKind.ImageOperands => $"{ToEnum<Spv.Specification.ImageOperandsMask>()}",
            OperandKind.KernelEnqueueFlags => $"{ToEnum<Spv.Specification.KernelEnqueueFlags>()}",
            OperandKind.KernelProfilingInfo => $"{ToEnum<Spv.Specification.KernelProfilingInfoMask>()}",
            OperandKind.LinkageType => $"{ToEnum<Spv.Specification.LinkageType>()}",
            OperandKind.None => "",
            _ => Words[0].ToString()
        };
    }
}