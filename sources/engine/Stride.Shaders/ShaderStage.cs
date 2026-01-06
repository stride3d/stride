// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

/// <summary>
///   Specifies a particular shader stage.
/// </summary>
[DataContract]
public enum ShaderStage
{
    /// <summary>
    ///   No shader stage defined.
    /// </summary>
    None = 0,

    /// <summary>
    ///   The Vertex Shader stage.
    /// </summary>
    Vertex = 1,

    /// <summary>
    ///   The Hull Shader stage.
    /// </summary>
    Hull = 2,

    /// <summary>
    ///   The Domain Shader stage.
    /// </summary>
    Domain = 3,

    /// <summary>
    ///   The Geometry Shader stage.
    /// </summary>
    Geometry = 4,

    /// <summary>
    ///   The Pixel Shader stage.
    /// </summary>
    Pixel = 5,

    /// <summary>
    ///   The Compute Shader stage.
    /// </summary>
    Compute = 6
}
