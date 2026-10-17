// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// SPIR-V has floating-point matrices only. A class keeps an int, uint or bool matrix as an OpTypeMatrix (so it keeps its
    /// SDSL type when the class is imported); here it becomes an array of its columns (the HLSL rows), as in DXC. The array
    /// has the same shape, so the access chains, OpCompositeExtract / Insert / Construct and constants of the matrix stay valid.
    /// </summary>
    private static void LowerIntegerMatrices(SpirvContext context)
    {
        for (var index = 0; index < context.Count; index++)
        {
            if (context[index].Op != Op.OpTypeMatrix)
                continue;

            var words = context[index].Data.Memory.Span;
            var (resultId, columnType, columnCount) = (words[1], words[2], words[3]);
            if (context.ReverseTypes[columnType] is not VectorType { BaseType: { } baseType } || baseType.IsFloating())
                continue;

            // The array length is a constant, declared before the array
            var uintType = context.GetOrRegister(ScalarType.UInt);
            MoveBefore(context, Op.OpTypeInt, uintType, ref index);
            var length = context.CompileConstant((uint)columnCount).Id;
            MoveBefore(context, Op.OpConstant, length, ref index);

            context.Replace(index, new OpTypeArray(resultId, columnType, length));
        }
    }

    /// <summary>
    /// Moves the OpTypeInt or OpConstant that declares <paramref name="id"/> (it only depends on earlier instructions) before
    /// <paramref name="index"/> when it comes after it.
    /// </summary>
    private static void MoveBefore(SpirvContext context, Op op, int id, ref int index)
    {
        var resultIndex = op == Op.OpTypeInt ? 1 : 2;
        for (var i = index + 1; i < context.Count; i++)
        {
            var words = context[i].Data.Memory.Span;
            if (context[i].Op == op && words[resultIndex] == id)
            {
                var data = context[i].Data;
                context.RemoveAt(i, dispose: false);
                context.Insert(index++, data);
                return;
            }
        }
    }
}
