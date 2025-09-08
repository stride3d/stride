// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics.Semantics;

public struct PositionSemantic : IFloat3Semantic
{
    public static string Name => VertexElementUsage.Position;
}

public struct NormalSemantic : IFloat3Semantic
{
    public static string Name => VertexElementUsage.Normal;
}

public struct ColorSemantic : IFloat4Semantic
{
    public static string Name => VertexElementUsage.Color;
}

public struct TangentSemantic : IFloat4Semantic
{
    public static string Name => VertexElementUsage.Tangent;
}

public struct BiTangentSemantic : IFloat4Semantic
{
    public static string Name => VertexElementUsage.BiTangent;
}

public struct TextureCoordinateSemantic : IFloat2Semantic
{
    public static string Name => VertexElementUsage.TextureCoordinate;
}

public struct BlendIndicesSemantic : IUShort4Semantic
{
    public static string Name => VertexElementUsage.BlendIndices;
}

public struct BlendWeightSemantic : IFloat4Semantic
{
    public static string Name => VertexElementUsage.BlendWeight;
}

/// <summary>
/// A semantic extension to allow users to provide non default destination datatypes
/// </summary>
/// <example>
/// Reading positions of a mesh into a Half3 array
/// <code>
/// <![CDATA[
/// var positions = new Half3[count];
/// helper.Read<Relaxed<PositionSemantic>, Half3>(positions);
/// ]]>
/// </code>
/// </example>
/// <typeparam name="T">The actual semantic used, for example <see cref="PositionSemantic"/></typeparam>
public struct Relaxed<T> : 
    IFloat4Semantic, 
    IFloat3Semantic, 
    IFloat2Semantic,
    IHalf4Semantic, 
    IHalf3Semantic, 
    IHalf2Semantic,
    IUShort4Semantic,
    IByte4Semantic,
    IUNorm4Semantic
    where T : ISemantic
{
    public static string Name => T.Name;
}
