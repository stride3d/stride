// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

internal readonly struct PipelineStateDescriptionWithHash(in PipelineStateDescription state) : IEquatable<PipelineStateDescriptionWithHash>
{
    public readonly int Hash = state.GetHashCode();
    public readonly PipelineStateDescription State = state;


    public bool Equals(PipelineStateDescriptionWithHash other)
    {
        return Hash == other.Hash
            && (State is null) == (other.State is null)
            && (State?.Equals(other.State) ?? true);
    }

    public override bool Equals(object obj)
    {
        return obj is PipelineStateDescriptionWithHash other && Equals(other);
    }

    public override int GetHashCode() => Hash;

    public static bool operator ==(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
    {
        return !left.Equals(right);
    }
}
