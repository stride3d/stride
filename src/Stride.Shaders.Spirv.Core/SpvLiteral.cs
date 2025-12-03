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
    public readonly T ToEnum<T>() where T : Enum
        => Unsafe.As<int, T>(ref Words[0]);

    public readonly T ToLiteral<T>()
    {
        using var lit = new LiteralValue<T>(Words);
        return lit.Value;
    }
    public readonly LiteralArray<T> ToLiteralArray<T>() where T : struct
        => LiteralArray<T>.From(Words);

    public readonly bool TryToLiteral<T>(out LiteralValue<T> literal)
    {
        literal = default;
        (bool r, literal) = (literal, Kind) switch
        {
            (LiteralValue<(int, int)>, OperandKind kind) when kind.ToString().StartsWith("Pair") => (true, LiteralValue<T>.From(Words)),
            (LiteralValue<int>, OperandKind kind) when kind.ToString().StartsWith("Id") => (true, LiteralValue<T>.From(Words)),
            (LiteralValue<int>, OperandKind.LiteralInteger or OperandKind.LiteralExtInstInteger or OperandKind.LiteralSpecConstantOpInteger)
                => (true, LiteralValue<T>.From(Words)),
            (LiteralValue<Half> or LiteralValue<float> or LiteralValue<double>, OperandKind.LiteralFloat) => (true, LiteralValue<T>.From(Words)),
            (LiteralValue<string>, OperandKind.LiteralString) => (true, LiteralValue<T>.From(Words)),
            _ => (false, default)
        };
        return true;
    }
    public readonly bool TryToArray<T>(out LiteralArray<T> literal) where T : struct
    {
        literal = default;
        (bool r, literal) = (literal, Kind) switch
        {
            (LiteralArray<(int, int)>, OperandKind kind) when kind.ToString().StartsWith("Pair") => (true, LiteralArray<T>.From(Words)),
            (LiteralArray<int>, OperandKind.IdRef or OperandKind.LiteralInteger)
                => (true, LiteralArray<T>.From(Words)),
            _ => (false, default) // No other types supported yet
        };
        return r;
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
            OperandKind.MemoryAccess => $"{ToEnum<Specification.MemoryAccessMask>()}",
            OperandKind.MemoryModel => $"{ToEnum<Specification.MemoryModel>()}",
            OperandKind.MemorySemantics => $"{ToEnum<Specification.MemorySemanticsMask>()}",
            OperandKind.AccessQualifier => $"{ToEnum<Specification.AccessQualifier>()}",
            OperandKind.AddressingModel => $"{ToEnum<Specification.AddressingModel>()}",
            OperandKind.BuiltIn => $"{ToEnum<Specification.BuiltIn>()}",
            OperandKind.Capability => $"{ToEnum<Specification.Capability>()}",
            OperandKind.Decoration => $"{ToEnum<Specification.Decoration>()}",
            OperandKind.Dim => $"{ToEnum<Specification.Dim>()}",
            OperandKind.ExecutionMode => $"{ToEnum<Specification.ExecutionMode>()}",
            OperandKind.ExecutionModel => $"{ToEnum<Specification.ExecutionModel>()}",
            OperandKind.FPFastMathMode => $"{ToEnum<Specification.FPFastMathModeMask>()}",
            OperandKind.FPRoundingMode => $"{ToEnum<Specification.FPRoundingMode>()}",
            OperandKind.FragmentShadingRate => $"{ToEnum<Specification.FragmentShadingRateMask>()}",
            OperandKind.FunctionControl => $"{ToEnum<Specification.FunctionControlMask>()}",
            OperandKind.FunctionParameterAttribute => $"{ToEnum<Specification.FunctionParameterAttribute>()}",
            OperandKind.GroupOperation => $"{ToEnum<Specification.GroupOperation>()}",
            OperandKind.ImageChannelDataType => $"{ToEnum<Specification.ImageChannelDataType>()}",
            OperandKind.ImageChannelOrder => $"{ToEnum<Specification.ImageChannelOrder>()}",
            OperandKind.ImageFormat => $"{ToEnum<Specification.ImageFormat>()}",
            OperandKind.ImageOperands => $"{ToEnum<Specification.ImageOperandsMask>()}",
            OperandKind.KernelEnqueueFlags => $"{ToEnum<Specification.KernelEnqueueFlags>()}",
            OperandKind.KernelProfilingInfo => $"{ToEnum<Specification.KernelProfilingInfoMask>()}",
            OperandKind.LinkageType => $"{ToEnum<Specification.LinkageType>()}",
            OperandKind.None => "",
            _ => Words[0].ToString()
        };
    }
}