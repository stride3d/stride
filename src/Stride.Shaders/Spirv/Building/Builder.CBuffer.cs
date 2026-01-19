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
            AlignmentRules.CBuffer => ((current.Size + 15) / 16 * 16 * (count - 1) + current.Size, 16),
            AlignmentRules.StructuredBuffer => (current.Size * count, current.Alignment),
        };
        return (symbol) switch
        {
            ScalarType { TypeName: "sbyte" or "byte" } => (1, 1),
            ScalarType { TypeName: "short" or "ushort" } => (2, 2),
            ScalarType { TypeName: "int" or "uint" or "float" or "bool" } => (4, 4),
            ScalarType { TypeName: "long" or "ulong" or "double" } => (8, 8),
            StructuredType s => StructSizeInBuffer(s, alignmentRules),
            VectorType v => MultiplySize(TypeSizeInBuffer(v.BaseType, typeModifier, alignmentRules), v.Size),
            // Note: this is HLSL-style so Rows/Columns meaning is swapped
            // Note: HLSL default is ColumnMajor
            MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None
                => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules), alignmentRules == AlignmentRules.CBuffer ? (4 * (m.Rows - 1)) + m.Columns : m.Rows * m.Columns * 4),
            MatrixType m when typeModifier == TypeModifier.RowMajor
                => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier, alignmentRules), alignmentRules == AlignmentRules.CBuffer ? (4 * (m.Columns - 1)) + m.Rows : m.Rows * m.Columns * 4),
            // Round up to 16 bytes (size of float4)
            ArrayType a => Array(TypeSizeInBuffer(a.BaseType, typeModifier, alignmentRules), a.Size, alignmentRules),
            // TODO: StructureType
        };
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
            maxAlignment = Math.Max(memberSizeAndAlignment.Alignment, maxAlignment);
        }

        var alignment = alignmentRules switch
        {
            AlignmentRules.CBuffer => 16,
            AlignmentRules.StructuredBuffer => maxAlignment,
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
}
