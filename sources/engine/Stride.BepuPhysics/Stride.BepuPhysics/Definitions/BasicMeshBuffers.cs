// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions;

internal struct BasicMeshBuffers
{
    public VertexPosition3[] Vertices = [];
    public int[] Indices = [];

    public BasicMeshBuffers()
    {
    }
}
