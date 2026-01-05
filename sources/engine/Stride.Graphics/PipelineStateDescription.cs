// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Describes the configuration of the graphics pipeline that will be encapsulated by a <see cref="PipelineState"/> object.
///   This includes Shaders, input layout, Render States, and output settings.
/// </summary>
/// <seealso cref="PipelineState"/>
public sealed class PipelineStateDescription : IEquatable<PipelineStateDescription>
{
    // Root Signature

    /// <summary>
    ///   A definition of the Shader resources and their bindings, including Constant Buffers, Textures, and Samplers
    ///   that will be accessed by the Shader programs.
    /// </summary>
    public RootSignature? RootSignature;

    // Effect / Shader

    /// <summary>
    ///   Compiled Shader programs (vertex, pixel, geometry, etc.) that define the programmable stages of the graphics pipeline.
    /// </summary>
    public EffectBytecode? EffectBytecode;

    // Rendering States

    /// <summary>
    ///   A description of the Blend State, which controls how the output color and alpha values of rendered pixels
    ///   are blended with existing pixels in the bound Render Targets.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    ///   A 32-bit mask that determines which samples in a multi-sampled Render Target are updated during rendering operations.
    /// </summary>
    public uint SampleMask = 0xFFFFFFFF;

    /// <summary>
    ///   A description of the Rasterizer State, which controls how primitives are rasterized into pixels,
    ///   including settings for culling, fill mode, depth bias, and scissor testing.
    /// </summary>
    public RasterizerStateDescription RasterizerState;

    /// <summary>
    ///   A description of the Depth-Stencil State, which controls how Depth and Stencil Buffers are used during rendering,
    ///   including comparison functions, write masks, and stencil operations.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState;

    // Input layout

    /// <summary>
    ///   A description of the input layout for the Vertex Buffer, which defines how vertex data is structured.
    ///   The array describes per-vertex attributes such as position, normal, texture coordinates,
    ///   and their respective formats.
    /// </summary>
    public InputElementDescription[]? InputElements;

    /// <summary>
    ///   Specifies how vertices should be interpreted to form primitives (points, lines, triangles, etc.)
    /// </summary>
    public PrimitiveType PrimitiveType;

    // Output

    /// <summary>
    ///   A description of the output configuration for the graphics pipeline, such as the format and count of Render Targets,
    ///   along with the Depth-Stencil Buffer format used for rendering output.
    /// </summary>
    public RenderOutputDescription Output;


    /// <summary>
    ///   Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>A new object that is a copy of this instance.</returns>
    public PipelineStateDescription Clone()
    {
        return new PipelineStateDescription
        {
            RootSignature = RootSignature,
            EffectBytecode = EffectBytecode,
            BlendState = BlendState,
            SampleMask = SampleMask,
            RasterizerState = RasterizerState,
            DepthStencilState = DepthStencilState,

            InputElements = (InputElementDescription[]?) InputElements?.Clone(),

            PrimitiveType = PrimitiveType,

            Output = Output
        };
    }

    /// <summary>
    ///   Sets default values for this Pipeline State Description.
    /// </summary>
    /// <remarks>
    ///   For more information about the default values, see the individual state descriptions:
    ///   <see cref="BlendStateDescription.Default"/>, <see cref="RasterizerStateDescription.Default"/>, and
    ///   <see cref="DepthStencilStateDescription.Default"/>.
    /// </remarks>
    public void SetDefaults()
    {
        BlendState = BlendStateDescription.Default;
        RasterizerState = RasterizerStateDescription.Default;
        DepthStencilState = DepthStencilStateDescription.Default;
    }


    /// <inheritdoc/>
    public bool Equals(PipelineStateDescription? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (!(RootSignature == other.RootSignature
            && EffectBytecode == other.EffectBytecode
            && BlendState.Equals(other.BlendState)
            && SampleMask == other.SampleMask
            && RasterizerState.Equals(other.RasterizerState)
            && DepthStencilState.Equals(other.DepthStencilState)
            && PrimitiveType == other.PrimitiveType
            && Output == other.Output))
            return false;

        if ((InputElements is not null) != (other.InputElements is not null))
            return false;

        if (InputElements is not null)
        {
            if (InputElements.Length != other.InputElements!.Length)
                return false;

            for (int i = 0; i < InputElements.Length; ++i)
            {
                if (!InputElements[i].Equals(other.InputElements[i]))
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PipelineStateDescription pipelineStateDescription && Equals(pipelineStateDescription);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(RootSignature);
        hashCode.Add(EffectBytecode);
        hashCode.Add(BlendState);
        hashCode.Add(SampleMask);
        hashCode.Add(RasterizerState);
        hashCode.Add(DepthStencilState);

        if (InputElements is not null)
        {
            foreach (var inputElement in InputElements)
                hashCode.Add(inputElement);
        }

        hashCode.Add(PrimitiveType);
        hashCode.Add(Output);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(PipelineStateDescription left, PipelineStateDescription right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PipelineStateDescription left, PipelineStateDescription right)
    {
        return !Equals(left, right);
    }
}
