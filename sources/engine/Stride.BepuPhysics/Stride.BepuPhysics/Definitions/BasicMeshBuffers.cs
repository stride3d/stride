// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions;

internal struct BasicMeshBuffers
{
    public VertexPosition3[] Vertices = [];
    public int[] Indices = [];

    /// <summary> Largest distance between this mesh and the exact shape it stands for, in the mesh's units; 0 when its faces are the shape's own </summary>
    public float MaxDeviation;

    public BasicMeshBuffers()
    {
    }
}
