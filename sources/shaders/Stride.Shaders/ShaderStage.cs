// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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

/// <summary>
///   Flags enum for combining multiple shader stages.
/// </summary>
[Flags]
[DataContract]
public enum ShaderStageFlags
{
    None     = 0,
    Vertex   = 1 << 0,
    Hull     = 1 << 1,
    Domain   = 1 << 2,
    Geometry = 1 << 3,
    Pixel    = 1 << 4,
    Compute  = 1 << 5,
}

public static class ShaderStageExtensions
{
    public static ShaderStageFlags ToFlag(this ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex   => ShaderStageFlags.Vertex,
        ShaderStage.Hull     => ShaderStageFlags.Hull,
        ShaderStage.Domain   => ShaderStageFlags.Domain,
        ShaderStage.Geometry => ShaderStageFlags.Geometry,
        ShaderStage.Pixel    => ShaderStageFlags.Pixel,
        ShaderStage.Compute  => ShaderStageFlags.Compute,
        _ => ShaderStageFlags.None,
    };

    public static void ForEach(this ShaderStageFlags flags, Action<ShaderStage> action)
    {
        if ((flags & ShaderStageFlags.Vertex) != 0)   action(ShaderStage.Vertex);
        if ((flags & ShaderStageFlags.Hull) != 0)     action(ShaderStage.Hull);
        if ((flags & ShaderStageFlags.Domain) != 0)   action(ShaderStage.Domain);
        if ((flags & ShaderStageFlags.Geometry) != 0) action(ShaderStage.Geometry);
        if ((flags & ShaderStageFlags.Pixel) != 0)    action(ShaderStage.Pixel);
        if ((flags & ShaderStageFlags.Compute) != 0)  action(ShaderStage.Compute);
    }
}
