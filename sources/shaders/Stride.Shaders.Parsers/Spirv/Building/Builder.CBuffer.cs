using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.Shaders.Spirv.Building;

partial class SpirvBuilder
{
    public enum AlignmentRules
    {
        CBuffer,
        /// <summary>std430: 3- and 4-element vectors aligned to 16 bytes.</summary>
        StructuredBuffer,
        /// <summary>
        /// Direct3D's structured buffer layout: everything aligned to its scalar size, nothing padded to 16 bytes
        /// (a <c>float3</c> is 12 bytes). Vulkan reads it with <c>scalarBlockLayout</c>. Only used where it differs from std430
        /// (<see cref="StructuredBufferLayout"/>).
        /// </summary>
        StructuredBufferTight,
    }

    public static (int Size, int Alignment) TypeSizeInBuffer(SymbolType symbol, TypeModifier typeModifier, AlignmentRules alignmentRules)
    {
        // Helper to multiply size without changing alignment
        static (int Size, int Alignment) MultiplySize((int Size, int Alignment) current, int count) => (current.Size * count, current.Alignment);

        static (int Size, int Alignment) Array((int Size, int Alignment) current, int count, AlignmentRules alignmentRules) => alignmentRules switch
        {
            // HLSL array size: last element is not padded to stride
            AlignmentRules.CBuffer => ((current.Size + 15) / 16 * 16 * (count - 1) + current.Size, 16),
            AlignmentRules.StructuredBuffer or AlignmentRules.StructuredBufferTight => (current.Size * count, current.Alignment),
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
            // Tight: the matrix vectors follow each other with no padding
            MatrixType m when alignmentRules == AlignmentRules.StructuredBufferTight
                => (MatrixStride(m, typeModifier, alignmentRules) * MatrixLayoutVectors(m, typeModifier).Count, TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules).Alignment),
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
    /// The vectors a matrix is stored as in a buffer, as its layout decoration says (<c>ShaderMixer.DecorateMember</c>):
    /// HLSL <c>column_major</c> (the default) is SPIR-V <c>RowMajor</c>, <c>Rows</c> vectors of <c>Columns</c> elements;
    /// HLSL <c>row_major</c> is SPIR-V <c>ColMajor</c>, <c>Columns</c> vectors of <c>Rows</c> elements.
    /// (A HLSL row is a SPIR-V column, so <c>Columns</c> counts the HLSL rows.)
    /// </summary>
    public static (int VectorSize, int Count) MatrixLayoutVectors(MatrixType m, TypeModifier typeModifier)
        => typeModifier == TypeModifier.RowMajor ? (m.Rows, m.Columns) : (m.Columns, m.Rows);

    /// <summary>
    /// The <c>MatrixStride</c> decoration of a matrix in a buffer: a float4 register in a cbuffer, the std430 stride of
    /// its vectors in a structured buffer.
    /// </summary>
    public static int MatrixStride(MatrixType m, TypeModifier typeModifier, AlignmentRules alignmentRules) => alignmentRules switch
    {
        AlignmentRules.CBuffer => 16,
        AlignmentRules.StructuredBufferTight => TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules).Size * MatrixLayoutVectors(m, typeModifier).VectorSize,
        _ => StorageBufferArrayStride(new VectorType(m.BaseType, MatrixLayoutVectors(m, typeModifier).VectorSize)),
    };

    /// <summary>
    /// Computes std430 size and alignment for a matrix in a StorageBuffer: its vectors (<see cref="MatrixLayoutVectors"/>),
    /// each at its std430 base-alignment stride, so non-square matrices get trailing padding on the short axis.
    /// </summary>
    private static (int Size, int Alignment) StructuredBufferMatrixSize(MatrixType m, TypeModifier typeModifier)
    {
        var (vecDim, vecCount) = MatrixLayoutVectors(m, typeModifier);
        var vecAlignment = StorageBufferBaseAlignment(new VectorType(m.BaseType, vecDim));
        return (MatrixStride(m, typeModifier, AlignmentRules.StructuredBuffer) * vecCount, vecAlignment);
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
            AlignmentRules.StructuredBuffer or AlignmentRules.StructuredBufferTight => maxAlignment,
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
        MatrixType m => StorageBufferBaseAlignment(new VectorType(m.BaseType, MatrixLayoutVectors(m, typeModifier).VectorSize)),
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
    public static int StorageBufferArrayStride(SymbolType elementType, TypeModifier typeModifier = TypeModifier.None, AlignmentRules alignmentRules = AlignmentRules.StructuredBuffer)
    {
        var (size, tightAlignment) = TypeSizeInBuffer(elementType, typeModifier, alignmentRules);
        var alignment = alignmentRules == AlignmentRules.StructuredBufferTight ? tightAlignment : StorageBufferBaseAlignment(elementType, typeModifier);
        return (size + alignment - 1) / alignment * alignment;
    }

    /// <summary>
    /// The layout of a structured buffer's elements: std430, unless it places a component elsewhere than Direct3D does
    /// (3-element vectors, sizes that are not a multiple of 16...): then Direct3D's tight layout, so the same CPU data reads the same.
    /// </summary>
    public static AlignmentRules StructuredBufferLayout(SymbolType elementType)
    {
        var std430 = new List<int>();
        var tight = new List<int>();
        ComponentOffsets(elementType, TypeModifier.None, AlignmentRules.StructuredBuffer, 0, std430);
        ComponentOffsets(elementType, TypeModifier.None, AlignmentRules.StructuredBufferTight, 0, tight);
        return std430.SequenceEqual(tight) && StorageBufferArrayStride(elementType) == StorageBufferArrayStride(elementType, TypeModifier.None, AlignmentRules.StructuredBufferTight)
            ? AlignmentRules.StructuredBuffer
            : AlignmentRules.StructuredBufferTight;
    }

    // The byte offset of every scalar of a value laid out with these rules, as the decorations place it
    // (struct members as ShaderMixer.EmitStructDecorations, arrays at StorageBufferArrayStride)
    static void ComponentOffsets(SymbolType type, TypeModifier typeModifier, AlignmentRules alignmentRules, int offset, List<int> offsets)
    {
        switch (type)
        {
            case ScalarType:
                offsets.Add(offset);
                break;
            case VectorType v:
                var scalarSize = TypeSizeInBuffer(v.BaseType, typeModifier, alignmentRules).Size;
                for (var i = 0; i < v.Size; i++)
                    offsets.Add(offset + i * scalarSize);
                break;
            case MatrixType m:
                var (vectorSize, count) = MatrixLayoutVectors(m, typeModifier);
                var matrixStride = MatrixStride(m, typeModifier, alignmentRules);
                for (var i = 0; i < count; i++)
                    ComponentOffsets(new VectorType(m.BaseType, vectorSize), typeModifier, alignmentRules, offset + i * matrixStride, offsets);
                break;
            case ArrayType a:
                var arrayStride = StorageBufferArrayStride(a.BaseType, typeModifier, alignmentRules);
                for (var i = 0; i < a.Size; i++)
                    ComponentOffsets(a.BaseType, typeModifier, alignmentRules, offset + i * arrayStride, offsets);
                break;
            case StructuredType s:
                var memberOffset = 0;
                foreach (var member in s.Members)
                {
                    var size = ComputeBufferOffset(member.Type, member.TypeModifier, ref memberOffset, alignmentRules).Size;
                    ComponentOffsets(member.Type, member.TypeModifier, alignmentRules, offset + memberOffset, offsets);
                    memberOffset += size;
                }
                break;
            default:
                throw new NotSupportedException($"Unsupported type for structured buffer layout: {type}");
        }
    }
}
