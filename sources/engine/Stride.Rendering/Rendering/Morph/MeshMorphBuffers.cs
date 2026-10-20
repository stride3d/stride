// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering;

/// <summary>
/// GPU inputs of a <see cref="RenderMesh"/> whose morph targets are applied in the vertex shader (<c>TransformationMorph</c>).
/// </summary>
public sealed class MeshMorphBuffers
{
    // Bits of the MorphTargets shader macro; keep in sync with TransformationMorph.sdsl.
    /// <summary>Flag: morph targets are applied (positions).</summary>
    public const int Enabled = 1;

    /// <summary>Flag: normal deltas are applied.</summary>
    public const int Normal = 2;

    /// <summary>Flag: tangent deltas are applied; only together with <see cref="Normal"/>.</summary>
    public const int Tangent = 4;

    /// <summary>Packed <see cref="MeshMorphEntry"/> values, shared by every instance of the mesh.</summary>
    public Buffer Entries;

    /// <summary>First entry of each vertex, with an end sentinel, shared by every instance of the mesh.</summary>
    public Buffer VertexOffsets;

    /// <summary>One weight per morph target.</summary>
    public Buffer Weights;

    /// <summary>Combination of <see cref="Enabled"/>, <see cref="Normal"/> and <see cref="Tangent"/>; the shader permutation.</summary>
    public int Flags;

    /// <summary>Number of morph targets, the length of <see cref="Weights"/>.</summary>
    public int TargetCount;
}
