// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

public sealed partial class ShaderMember
{
    /// <summary>
    /// The first int, uint or bool matrix in a type (itself, an array element, a struct member). The mixer makes these arrays of
    /// rows, which has no cbuffer or buffer layout yet (DXC's SPIR-V only lays out row_major ones), hence SDSL0195 there.
    /// </summary>
    internal static MatrixType? FindIntegerMatrix(SymbolType? type) => type switch
    {
        MatrixType matrix when !matrix.BaseType.IsFloating() => matrix,
        ArrayType array => FindIntegerMatrix(array.BaseType),
        StructuredType structured => structured.Members.Select(x => FindIntegerMatrix(x.Type)).FirstOrDefault(x => x != null),
        _ => null,
    };
}
