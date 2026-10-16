// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

/// <summary>
/// Overload resolution of calls to shader methods, ranked like DXC.
/// </summary>
/// <remarks>
/// Each argument adds one unit for each kind of conversion it needs, and each kind has its own bits in the score, so one
/// conversion of a worse kind weighs more than any number of milder ones. From the mildest to the worst: scalar splat,
/// promotion (half to float, float to double, int to int64), integer sign change, other element conversion, truncation.
/// For the element kinds, the parameter side counts less than the argument side: float to int costs less than int to float,
/// uint to int less than int to uint. The lowest score wins; two overloads with the same score are ambiguous, even when the
/// conversions differ in size (float3 to float and float3 to float2 are both one truncation).
/// This follows DXC's HLSLExternalSource::ScoreCast (FXC gives the same results). Intrinsics keep their own ranking
/// (<see cref="OverloadScore"/>): DXC matches them with templates instead.
/// </remarks>
public partial class MethodCall
{
    // Same order as DXC's score bits, from the mildest to the worst
    private enum ConversionKind
    {
        Splat,
        ParameterPromotion,
        ArgumentPromotion,
        ParameterSignChange,
        ArgumentSignChange,
        ParameterConversion,
        ArgumentConversion,
        Truncation,
    }

    // 7 bits per kind, so 127 arguments of a kind still weigh less than one of the next kind.
    // The 2 lowest bits are below every kind: Stride uses them for streams conversions.
    private static ulong Weight(ConversionKind kind) => 1UL << (2 + 7 * (int)kind);

    private enum MismatchKind
    {
        None,
        ArgumentCount,
        ArgumentType,
        NotWritable,
    }

    private readonly record struct Mismatch(MismatchKind Kind, int Index = -1);

    /// <summary>
    /// Ranks a shader method against the argument types of a call.
    /// </summary>
    /// <returns>The score (lower is better), or null when the method can't take these arguments.</returns>
    internal static ulong? RankOverload(FunctionType functionType, int defaultParameters, SymbolType[] argumentTypes)
        => RankOverload(functionType, defaultParameters, argumentTypes, out _);

    private static ulong? RankOverload(FunctionType functionType, int defaultParameters, SymbolType[] argumentTypes, out Mismatch mismatch)
    {
        if (argumentTypes.Length > functionType.ParameterTypes.Count || argumentTypes.Length < functionType.ParameterTypes.Count - defaultParameters)
        {
            mismatch = new(MismatchKind.ArgumentCount);
            return null;
        }

        ulong score = 0;
        for (var index = 0; index < argumentTypes.Length; index++)
        {
            var parameter = functionType.ParameterTypes[index];
            var isOutput = (parameter.Modifiers & ParameterModifiers.Out) != 0;

            // out/inout parameters require a writable reference (PointerType)
            if (isOutput && argumentTypes[index] is not PointerType)
            {
                mismatch = new(MismatchKind.NotWritable, index);
                return null;
            }

            var argumentType = argumentTypes[index].GetValueType();
            var parameterType = parameter.Type.GetValueType();
            // An out value is also converted back after the call: DXC counts both ways
            if (ConversionScore(argumentType, parameterType) is not { } inScore
                || (isOutput ? ConversionScore(parameterType, argumentType) : 0) is not { } outScore)
            {
                mismatch = new(MismatchKind.ArgumentType, index);
                return null;
            }

            score += inScore + outScore;
        }

        mismatch = default;
        return score;
    }

    /// <summary>
    /// The cost of converting a value for a parameter, or null if it can't be converted.
    /// </summary>
    private static ulong? ConversionScore(SymbolType from, SymbolType to)
    {
        if (from == to)
            return 0;

        // Note: order is Input, Streams, Output
        if (from is StreamsType { Kind: var sourceKind } && to is StreamsType { Kind: var targetKind })
            return targetKind > sourceKind ? 1 : null;

        // Arrays, structures and resources (buffers, textures, samplers...) only take their own type
        if (from is ArrayType || to is ArrayType)
            return from is ArrayType && to is ArrayType && SpirvBuilder.CanConvertScore(from, to) != int.MaxValue ? 0 : null;
        if (!IsNumeric(from) || !IsNumeric(to))
        {
            // Structured buffers compare their member list by reference: compare their kind and element type instead
            return from.GetType() == to.GetType() && from is StructuredType && from.ToId() == to.ToId() ? 0 : null;
        }

        // Which conversions are possible doesn't change: only their ranking
        if (SpirvBuilder.CanConvertScore(from, to) == int.MaxValue)
            return null;

        ulong score = 0;
        var fromCount = ElementCount(from);
        var toCount = ElementCount(to);
        if (fromCount < toCount)
            score += Weight(ConversionKind.Splat);
        else if (toCount < fromCount)
            score += Weight(ConversionKind.Truncation);

        var fromElement = ((ScalarType)from.GetElementType()).Type;
        var toElement = ((ScalarType)to.GetElementType()).Type;
        if (fromElement != toElement)
        {
            // Both sides are compared to the type they combine into, as for a binary operation
            var combined = Combine(toElement, fromElement);
            score += ElementConversion(combined, toElement, ConversionKind.ParameterPromotion, ConversionKind.ParameterSignChange, ConversionKind.ParameterConversion);
            score += ElementConversion(fromElement, combined, ConversionKind.ArgumentPromotion, ConversionKind.ArgumentSignChange, ConversionKind.ArgumentConversion);
        }

        return score;
    }

    private static bool IsNumeric(SymbolType type) => type is ScalarType or VectorType or MatrixType;

    private static int ElementCount(SymbolType type) => type switch
    {
        VectorType v => v.Size,
        MatrixType m => m.Rows * m.Columns,
        _ => 1,
    };

    private static ulong ElementConversion(Scalar from, Scalar to, ConversionKind promotion, ConversionKind signChange, ConversionKind conversion)
    {
        if (from == to)
            return 0;
        if (IsPromotion(from, to))
            return Weight(promotion);
        if (IsSignChange(from, to))
            return Weight(signChange);
        return Weight(conversion);
    }

    private static bool IsPromotion(Scalar from, Scalar to) => (from, to) switch
    {
        (Scalar.Half, Scalar.Float or Scalar.Double) => true,
        (Scalar.Float, Scalar.Double) => true,
        (Scalar.Int or Scalar.UInt, Scalar.Int64 or Scalar.UInt64) => true,
        _ => false,
    };

    private static bool IsSignChange(Scalar from, Scalar to) => (from, to) switch
    {
        (Scalar.Int, Scalar.UInt) or (Scalar.UInt, Scalar.Int) => true,
        (Scalar.Int64, Scalar.UInt64) or (Scalar.UInt64, Scalar.Int64) => true,
        _ => false,
    };

    /// <summary>
    /// The element type two element types combine into (DXC's CombineBasicTypes): the floating one if only one is,
    /// otherwise the larger, and for integers unsigned if either is.
    /// </summary>
    private static Scalar Combine(Scalar a, Scalar b)
    {
        if (a == b)
            return a;

        var aFloating = a is Scalar.Half or Scalar.Float or Scalar.Double;
        var bFloating = b is Scalar.Half or Scalar.Float or Scalar.Double;
        if (aFloating && bFloating)
            return Bits(a) >= Bits(b) ? a : b;
        if (aFloating)
            return a;
        if (bFloating)
            return b;

        // bool is an integer without size: it takes the other side's
        var unsigned = a is Scalar.UInt or Scalar.UInt64 || b is Scalar.UInt or Scalar.UInt64;
        return Math.Max(Bits(a), Bits(b)) == 64
            ? (unsigned ? Scalar.UInt64 : Scalar.Int64)
            : (unsigned ? Scalar.UInt : Scalar.Int);
    }

    private static int Bits(Scalar scalar) => scalar switch
    {
        Scalar.Half => 16,
        Scalar.Int64 or Scalar.UInt64 or Scalar.Double => 64,
        Scalar.Boolean => 0,
        _ => 32,
    };

    private static string FormatSignature(string name, IEnumerable<SymbolType> types)
        => $"{name}({string.Join(", ", types.Select(t => t.GetValueType()))})";

    private static string FormatSignature(string name, FunctionType functionType)
        => FormatSignature(name, functionType.ParameterTypes.Select(p => p.Type));

    private static string FormatArgumentCount(FunctionType functionType, int defaultParameters)
    {
        var count = functionType.ParameterTypes.Count;
        return defaultParameters > 0 ? $"{count - defaultParameters} to {count} arguments" : count == 1 ? "1 argument" : $"{count} arguments";
    }

    /// <summary>
    /// The error for a call that no overload can take, with the reason for each overload.
    /// </summary>
    private string FormatNoOverloadError(IEnumerable<Symbol> overloads, SymbolType[] argumentTypes)
    {
        var candidates = overloads.Select(x =>
        {
            var functionType = (FunctionType)x.Type;
            var defaultParameters = x.MethodDefaultParameters?.DefaultValues.Length ?? 0;
            RankOverload(functionType, defaultParameters, argumentTypes, out var mismatch);
            var reason = mismatch.Kind switch
            {
                MismatchKind.ArgumentCount => $"takes {FormatArgumentCount(functionType, defaultParameters)}",
                MismatchKind.NotWritable => $"argument {mismatch.Index + 1} ({Arguments.Values[mismatch.Index]}) can't be written back (out or inout parameter)",
                _ => $"argument {mismatch.Index + 1} can't be converted from {argumentTypes[mismatch.Index].GetValueType()} to {functionType.ParameterTypes[mismatch.Index].Type.GetValueType()}",
            };
            return $"\n  {FormatSignature(Name, functionType)}: {reason}";
        });
        return string.Format(SDSLErrorMessages.SDSL0162, FormatSignature(Name, argumentTypes), string.Concat(candidates));
    }

    private string FormatAmbiguousError(IEnumerable<Symbol> overloads, SymbolType[] argumentTypes)
        => string.Format(SDSLErrorMessages.SDSL0163, FormatSignature(Name, argumentTypes),
            string.Concat(overloads.Select(x => $"\n  {FormatSignature(Name, (FunctionType)x.Type)}")));
}
