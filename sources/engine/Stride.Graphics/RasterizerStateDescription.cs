// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct RasterizerStateDescription : IEquatable<RasterizerStateDescription>
{
    /// <summary>
    /// Describes a rasterizer state.
    /// </summary>
    public RasterizerStateDescription(CullMode cullMode) : this()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RasterizerStateDescription"/> class.
        /// </summary>
        /// <param name="cullMode">The cull mode.</param>
        /// <summary>
        /// Determines the fill mode to use when rendering (see <see cref="FillMode"/>).
        /// </summary>
        /// <summary>
        /// Indicates triangles facing the specified direction are not drawn (see <see cref="CullMode"/>).
        /// </summary>
        /// <summary>
        /// Determines if a triangle is front- or back-facing. If this parameter is true, then a triangle will be considered front-facing if its vertices are counter-clockwise on the render target and considered back-facing if they are clockwise. If this parameter is false then the opposite is true.
        /// </summary>
        /// <summary>
        /// Depth value added to a given pixel. 
        /// </summary>
        /// <summary>
        /// Gets or sets the depth bias for polygons, which is the amount of bias to apply to the depth of a primitive to alleviate depth testing problems for primitives of similar depth. The default value is 0.
        /// </summary>
        /// <summary>
        /// Scalar on a given pixel's slope. 
        /// </summary>
        /// <summary>
        /// Enable clipping based on distance. 
        /// </summary>
        /// <summary>
        /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        /// <summary>
        /// Multisample level.
        /// </summary>
        /// <summary>
        /// Enable line antialiasing; only applies if doing line drawing and MultisampleEnable is false.
        /// </summary>
        /// <summary>
        /// Sets default values for this instance.
        /// </summary>
        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        SetDefaults();
        CullMode = cullMode;
    }


    public FillMode FillMode;

    public CullMode CullMode;

    public bool FrontFaceCounterClockwise;

    public int DepthBias;

    public float DepthBiasClamp;

    public float SlopeScaleDepthBias;

    public bool DepthClipEnable;

    public bool ScissorTestEnable;

    public MultisampleCount MultisampleCount;

    public bool MultisampleAntiAliasLine;


    public void SetDefaults()
    {
        FillMode = FillMode.Solid;
        CullMode = CullMode.Back;
        FrontFaceCounterClockwise = false;
        DepthClipEnable = true;
        ScissorTestEnable = false;
        MultisampleCount = MultisampleCount.None;
        MultisampleAntiAliasLine = false;
        DepthBias = 0;
        DepthBiasClamp = 0f;
        SlopeScaleDepthBias = 0f;
    }


    public readonly bool Equals(RasterizerStateDescription other)
    {
        return FillMode == other.FillMode
            && CullMode == other.CullMode
            && FrontFaceCounterClockwise == other.FrontFaceCounterClockwise
            && DepthBias == other.DepthBias
            && DepthBiasClamp.Equals(other.DepthBiasClamp)
            && SlopeScaleDepthBias.Equals(other.SlopeScaleDepthBias)
            && DepthClipEnable == other.DepthClipEnable
            && ScissorTestEnable == other.ScissorTestEnable
            && MultisampleCount == other.MultisampleCount
            && MultisampleAntiAliasLine == other.MultisampleAntiAliasLine;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is RasterizerStateDescription rsdesc && Equals(rsdesc);
    }

    public static bool operator ==(RasterizerStateDescription left, RasterizerStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RasterizerStateDescription left, RasterizerStateDescription right)
    {
        return !left.Equals(right);
    }

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FillMode);
        hash.Add(CullMode);
        hash.Add(FrontFaceCounterClockwise);
        hash.Add(DepthBias);
        hash.Add(DepthBiasClamp);
        hash.Add(SlopeScaleDepthBias);
        hash.Add(DepthClipEnable);
        hash.Add(ScissorTestEnable);
        hash.Add(MultisampleCount);
        hash.Add(MultisampleAntiAliasLine);
        return hash.ToHashCode();
    }
}
