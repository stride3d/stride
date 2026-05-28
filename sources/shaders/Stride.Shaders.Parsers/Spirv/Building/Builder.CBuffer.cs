using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Shaders.Spirv.Building;

partial class SpirvBuilder
{
    public enum AlignmentRules
    {
        CBuffer,
        StructuredBuffer,
    }

    public static (int Size, int Alignment) TypeSizeInBuffer(SymbolType symbol, TypeModifier typeModifier, AlignmentRules alignmentRules)
    {
        // Helper to multiply size without changing alignment
        static (int Size, int Alignment) MultiplySize((int Size, int Alignment) current, int count) => (current.Size * count, current.Alignment);

        static (int Size, int Alignment) Array((int Size, int Alignment) current, int count, AlignmentRules alignmentRules) => alignmentRules switch
        {
            // HLSL array size: last element is not padded to stride
            AlignmentRules.CBuffer => ((current.Size + 15) / 16 * 16 * (count - 1) + current.Size, 16),
            AlignmentRules.StructuredBuffer => (current.Size * count, current.Alignment),
            _ => throw new NotSupportedException($"Unsupported AlignmentRules value: {alignmentRules}"),
        };
        return (symbol) switch
        {
            ScalarType { Type: Scalar.Int or Scalar.UInt or Scalar.Float or Scalar.Boolean } => (4, 4),
            ScalarType { Type: Scalar.Int64 or Scalar.UInt64 or Scalar.Double } => (8, 8),
            StructuredType s => StructSizeInBuffer(s, alignmentRules),
            // StructuredBuffer uses std430 vector alignment (2×scalar for vec2, 4×scalar for vec3/vec4)
            // to satisfy Vulkan's relaxed block layout rule that a vector must not straddle a
            // 16-byte boundary. CBuffer keeps scalar alignment — ComputeBufferOffset handles the
            // "vector crossing 16-byte boundary" bump separately for that path.
            VectorType v when alignmentRules == AlignmentRules.StructuredBuffer
                => (TypeSizeInBuffer(v.BaseType, typeModifier, alignmentRules).Size * v.Size,
                    TypeSizeInBuffer(v.BaseType, typeModifier, alignmentRules).Alignment * (v.Size == 2 ? 2 : 4)),
            VectorType v => MultiplySize(TypeSizeInBuffer(v.BaseType, typeModifier, alignmentRules), v.Size),
            // Note: this is HLSL-style so Rows/Columns meaning is swapped
            // Note: HLSL default is ColumnMajor
            // StructuredBuffer uses std430 strict matrix layout: each column (ColumnMajor) or row
            // (RowMajor) is padded to its std430 base alignment, matching how the Vulkan validator
            // expects matrix layout under relaxed block layout.
            MatrixType m when alignmentRules == AlignmentRules.StructuredBuffer
                => StructuredBufferMatrixSize(m, typeModifier),
            MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None
                => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules), (4 * (m.Rows - 1)) + m.Columns),
            MatrixType m when typeModifier == TypeModifier.RowMajor
                => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules), (4 * (m.Columns - 1)) + m.Rows),
            // Round up to 16 bytes (size of float4)
            ArrayType a => Array(TypeSizeInBuffer(a.BaseType, typeModifier, alignmentRules), a.Size, alignmentRules),
            // TODO: StructureType
            _ => throw new NotSupportedException($"Unsupported type for buffer layout: {symbol}"),
        };
    }

    /// <summary>
    /// Computes std430 size and alignment for a matrix in a StorageBuffer.
    /// ColumnMajor: matrix is an array of <c>Columns</c> column-vectors of dimension <c>Rows</c>.
    /// RowMajor: matrix is an array of <c>Rows</c> row-vectors of dimension <c>Columns</c>.
    /// Each element vector is laid out at its std430 base-alignment stride, so non-square matrices
    /// get trailing padding on the short axis.
    /// </summary>
    private static (int Size, int Alignment) StructuredBufferMatrixSize(MatrixType m, TypeModifier typeModifier)
    {
        var (vecDim, vecCount) = typeModifier == TypeModifier.RowMajor
            ? (m.Columns, m.Rows)
            : (m.Rows, m.Columns);
        var scalarSize = TypeSizeInBuffer(m.BaseType, typeModifier, AlignmentRules.StructuredBuffer).Size;
        var vecSize = scalarSize * vecDim;
        var vecAlignment = StorageBufferBaseAlignment(new VectorType(m.BaseType, vecDim));
        var vecStride = (vecSize + vecAlignment - 1) / vecAlignment * vecAlignment;
        return (vecStride * vecCount, vecAlignment);
    }

    private static (int, int) StructSizeInBuffer(StructuredType s, AlignmentRules alignmentRules)
    {
        var offset = 0;
        var maxAlignment = 0;

        // Apply same rules as inside a cbuffer
        foreach (var member in s.Members)
        {
            var memberSizeAndAlignment = ComputeBufferOffset(member.Type, member.TypeModifier, ref offset, alignmentRules);
            offset += memberSizeAndAlignment.Size;
            // SPIR-V/spirv-cross requires that no field falls within stride*count of an array.
            // Pad offset past the full array stride range so subsequent fields don't overlap.
            PadOffsetAfterArray(member.Type, member.TypeModifier, offset - memberSizeAndAlignment.Size, ref offset, alignmentRules);
            maxAlignment = Math.Max(memberSizeAndAlignment.Alignment, maxAlignment);
        }

        var alignment = alignmentRules switch
        {
            AlignmentRules.CBuffer => 16,
            AlignmentRules.StructuredBuffer => maxAlignment,
            _ => throw new NotSupportedException($"Unsupported AlignmentRules value: {alignmentRules}"),
        };

        return (offset, alignment);
    }

    //
    // Computes the size of a member type, including its alignment and array size.
    // It does so recursively for structs, and handles different parameter classes.
    //
    public static (int Size, int Alignment) ComputeBufferOffset(SymbolType type, TypeModifier typeModifier, ref int constantBufferOffset, AlignmentRules alignmentRules)
    {
        (var size, var alignment) = TypeSizeInBuffer(type, typeModifier, alignmentRules);

        // Align to float4 if it is bigger than leftover space in current float4
        if (alignmentRules == AlignmentRules.CBuffer)
        {
            if (constantBufferOffset / 16 != (constantBufferOffset + size - 1) / 16)
                alignment = 16;
        }

        // Align offset and store it as member offset
        constantBufferOffset = (constantBufferOffset + alignment - 1) / alignment * alignment;

        return (size, alignment);
    }

    /// <summary>
    /// After advancing past an array member, ensure the offset is past stride*count of the array.
    /// SPIR-V/spirv-cross cannot express fields that fall within an array's stride*count range,
    /// even if those bytes are padding in the last element (which HLSL allows).
    /// </summary>
    public static void PadOffsetAfterArray(SymbolType type, TypeModifier typeModifier, int memberOffset, ref int constantBufferOffset, AlignmentRules alignmentRules)
    {
        if (alignmentRules == AlignmentRules.CBuffer && type is ArrayType a)
        {
            var elementSize = TypeSizeInBuffer(a.BaseType, typeModifier, alignmentRules).Size;
            var stride = (elementSize + 15) / 16 * 16;
            var paddedEnd = memberOffset + stride * a.Size;
            if (constantBufferOffset < paddedEnd)
                constantBufferOffset = paddedEnd;
        }
    }

    /// <summary>
    /// Computes the std430 base alignment of a type as required by Vulkan's storage buffer layout
    /// (vec2 → 2×scalar, vec3/vec4 → 4×scalar, struct → max member alignment). Used to round the
    /// ArrayStride of a [RW]StructuredBuffer element type so the SPIR-V validates under relaxed
    /// block layout. Relaxed rules allow scalar-aligned offsets for vector members, but an array
    /// of structs still needs its stride aligned to the struct's base alignment.
    /// </summary>
    public static int StorageBufferBaseAlignment(SymbolType type, TypeModifier typeModifier = TypeModifier.None) => type switch
    {
        ScalarType { Type: Scalar.Int or Scalar.UInt or Scalar.Float or Scalar.Boolean or Scalar.Half } => 4,
        ScalarType { Type: Scalar.Int64 or Scalar.UInt64 or Scalar.Double } => 8,
        VectorType { Size: 2, BaseType: var bt } => 2 * StorageBufferBaseAlignment(bt),
        VectorType { Size: 3 or 4, BaseType: var bt } => 4 * StorageBufferBaseAlignment(bt),
        MatrixType m when typeModifier == TypeModifier.RowMajor
            => StorageBufferBaseAlignment(new VectorType(m.BaseType, m.Columns)),
        MatrixType m
            => StorageBufferBaseAlignment(new VectorType(m.BaseType, m.Rows)),
        ArrayType a => StorageBufferBaseAlignment(a.BaseType, typeModifier),
        StructuredType s => MaxMemberAlignment(s),
        _ => throw new NotSupportedException($"Unsupported type for storage buffer alignment: {type}"),
    };

    static int MaxMemberAlignment(StructuredType s)
    {
        var max = 4;
        foreach (var member in s.Members)
            max = Math.Max(max, StorageBufferBaseAlignment(member.Type, member.TypeModifier));
        return max;
    }

    /// <summary>
    /// Returns the ArrayStride required for <paramref name="elementType"/> when used as the element
    /// of a [RW]StructuredBuffer's runtime array. The value is the packed size (via
    /// <see cref="TypeSizeInBuffer"/>) rounded up to the type's std430 base alignment, so that the
    /// emitted SPIR-V validates under relaxed block layout.
    /// </summary>
    public static int StorageBufferArrayStride(SymbolType elementType, TypeModifier typeModifier = TypeModifier.None)
    {
        var size = TypeSizeInBuffer(elementType, typeModifier, AlignmentRules.StructuredBuffer).Size;
        var alignment = StorageBufferBaseAlignment(elementType, typeModifier);
        return (size + alignment - 1) / alignment * alignment;
    }
}
