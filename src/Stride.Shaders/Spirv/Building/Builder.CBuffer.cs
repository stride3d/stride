using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Shaders.Spirv.Building;

partial class SpirvBuilder
{
    public static (int Size, int Alignment) TypeSizeInBuffer(SymbolType symbol, TypeModifier typeModifier)
    {
        // Helper to multiply size without changing alignment
        static (int Size, int Alignment) MultiplySize((int Size, int Alignment) current, int count) => (current.Size * count, current.Alignment);
        return (symbol) switch
        {
            ScalarType { TypeName: "sbyte" or "byte" } => (1, 1),
            ScalarType { TypeName: "short" or "ushort" } => (2, 2),
            ScalarType { TypeName: "int" or "uint" or "float" or "bool" } => (4, 4),
            ScalarType { TypeName: "long" or "ulong" or "double" } => (8, 8),
            VectorType v => MultiplySize(TypeSizeInBuffer(v.BaseType, typeModifier), v.Size),
            // Note: HLSL default is ColumnMajor, review that for GLSL/Vulkan later
            MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier), ((4 * m.Columns - 1) + m.Rows)),
            MatrixType m when typeModifier == TypeModifier.RowMajor => MultiplySize(TypeSizeInBuffer(m.BaseType, typeModifier), ((4 * m.Rows - 1) + m.Columns)),
            // Round up to 16 bytes (size of float4)
            ArrayType a => ((TypeSizeInBuffer(a.BaseType, typeModifier).Size + 15) / 16 * 16 * a.Size, 16),
            // TODO: StructureType
        };
    }

    //
    // Computes the size of a member type, including its alignment and array size.
    // It does so recursively for structs, and handles different parameter classes.
    //
    public static int ComputeCBufferOffset(SymbolType type, TypeModifier typeModifier, ref int constantBufferOffset)
    {
        (var size, var alignment) = TypeSizeInBuffer(type, typeModifier);

        // Align to float4 if it is bigger than leftover space in current float4
        if (constantBufferOffset / 16 != (constantBufferOffset + size - 1) / 16)
            alignment = 16;

        // Align offset and store it as member offset
        constantBufferOffset = (constantBufferOffset + alignment - 1) / alignment * alignment;

        return size;
    }
}
