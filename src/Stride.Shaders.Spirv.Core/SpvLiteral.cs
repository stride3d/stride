using System.Data.SqlTypes;
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

    public readonly T To<T>()
    {
        T tmp = default!;
        if (tmp is bool or byte or sbyte or short or ushort or int or uint or float)
            tmp = Unsafe.As<int, T>(ref Words[0]);
        else if (tmp is long or double or ValueTuple<int, int>)
        {
            var value = Words[0] << 16 | Words[1];
            tmp = Unsafe.As<int, T>(ref value);
        }
        else if (tmp is null && typeof(T) == typeof(string))
        {
            using var lit = new LiteralValue<string>(Words);
            var result = lit.Value;
            Unsafe.As<T, string>(ref tmp) = result;
        }
        else if (tmp is Enum)
        {
            tmp = Unsafe.As<int, T>(ref Words[0]);
        }
        else if (typeof(T).Name.Contains("LiteralArray"))
        {
            if (tmp is LiteralArray<sbyte>)
            {
                var result = LiteralArray<sbyte>.From(Words);
                return Unsafe.As<LiteralArray<sbyte>, T>(ref result);
            }
            else if (tmp is LiteralArray<byte>)
            {
                var result = LiteralArray<byte>.From(Words);
                return Unsafe.As<LiteralArray<byte>, T>(ref result);
            }
            else if (tmp is LiteralArray<short>)
            {
                var result = LiteralArray<short>.From(Words);
                return Unsafe.As<LiteralArray<short>, T>(ref result);
            }
            else if (tmp is LiteralArray<int>)
            {
                var result = LiteralArray<int>.From(Words);
                return Unsafe.As<LiteralArray<int>, T>(ref result);
            }
            else if (tmp is LiteralArray<long>)
            {
                var result = LiteralArray<long>.From(Words);
                return Unsafe.As<LiteralArray<long>, T>(ref result);
            }
            else if (tmp is LiteralArray<byte>)
            {
                var result = LiteralArray<byte>.From(Words);
                return Unsafe.As<LiteralArray<byte>, T>(ref result);
            }
            else if (tmp is LiteralArray<ushort>)
            {
                var result = LiteralArray<ushort>.From(Words);
                return Unsafe.As<LiteralArray<ushort>, T>(ref result);
            }
            else if (tmp is LiteralArray<uint>)
            {
                var result = LiteralArray<uint>.From(Words);
                return Unsafe.As<LiteralArray<uint>, T>(ref result);
            }
            else if (tmp is LiteralArray<ulong>)
            {
                var result = LiteralArray<ulong>.From(Words);
                return Unsafe.As<LiteralArray<ulong>, T>(ref result);
            }
            else if (tmp is LiteralArray<float>)
            {
                var result = LiteralArray<float>.From(Words);
                return Unsafe.As<LiteralArray<float>, T>(ref result);
            }
            else if (tmp is LiteralArray<double>)
            {
                var result = LiteralArray<double>.From(Words);
                return Unsafe.As<LiteralArray<double>, T>(ref result);
            }
            else if (tmp is LiteralArray<bool>)
            {
                var result = LiteralArray<bool>.From(Words);
                return Unsafe.As<LiteralArray<bool>, T>(ref result);
            }
            else if (tmp is LiteralArray<(int, int)>)
            {
                var result = LiteralArray<(int, int)>.From(Words);
                return Unsafe.As<LiteralArray<(int, int)>, T>(ref result);
            }
            else throw new NotImplementedException("Can't convert operand to type " + typeof(T));
        }
        else throw new NotImplementedException($"Can't convert operand to type {typeof(T)}");
        return tmp;
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