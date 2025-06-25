// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Shaders;

namespace Stride.Graphics;

public class PipelineStateDescription : IEquatable<PipelineStateDescription>
{
    public RootSignature RootSignature;

    public EffectBytecode EffectBytecode;

    public BlendStateDescription BlendState;

    public uint SampleMask = 0xFFFFFFFF;

    public RasterizerStateDescription RasterizerState;

    public DepthStencilStateDescription DepthStencilState;


    public InputElementDescription[] InputElements;

    public PrimitiveType PrimitiveType;


    public RenderOutputDescription Output;


    public unsafe PipelineStateDescription Clone()
    {
        return new PipelineStateDescription
        {
            RootSignature = RootSignature,
            EffectBytecode = EffectBytecode,
            BlendState = BlendState,
            SampleMask = SampleMask,
            RasterizerState = RasterizerState,
            DepthStencilState = DepthStencilState,

            InputElements = (InputElementDescription[]) InputElements.Clone(),

            PrimitiveType = PrimitiveType,

            Output = Output
        };
    }

    public void SetDefaults()
    {
        BlendState.SetDefaults();
        RasterizerState.SetDefaults();
        DepthStencilState.SetDefaults();
    }


    public bool Equals(PipelineStateDescription other)
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
            if (InputElements.Length != other.InputElements.Length)
                return false;

            for (int i = 0; i < InputElements.Length; ++i)
            {
                if (!InputElements[i].Equals(other.InputElements[i]))
                    return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        return obj is PipelineStateDescription pipelineStateDescription && Equals(pipelineStateDescription);
    }

    public override int GetHashCode()
    {
        HashCode hashCode1 = new();
        hashCode1.Add(RootSignature);
        hashCode1.Add(EffectBytecode);
        hashCode1.Add(BlendState);
        hashCode1.Add(SampleMask);
        hashCode1.Add(RasterizerState);
        hashCode1.Add(DepthStencilState);

        if (InputElements is not null)
        {
            foreach (var inputElement in InputElements)
                hashCode1.Add(inputElement);
        }

        hashCode1.Add(PrimitiveType);
        hashCode1.Add(Output);
        return hashCode1.ToHashCode();
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
