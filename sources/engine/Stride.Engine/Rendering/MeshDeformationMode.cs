// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>Where morph targets and skinning of <see cref="Engine.ModelComponent"/> meshes are evaluated.</summary>
internal enum MeshDeformationMode
{
    /// <summary>Morphs and skinning in the vertex shader of every pass.</summary>
    VertexShader,
    /// <summary>Morphs and skinning of every deformed mesh in compute once per frame, including skinning-only meshes.</summary>
    Compute,
}
