// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Utility structure encapsulating the description for a <see cref="PipelineState"/> and a hash that allows
///   Stride to easily determine equality.
/// </summary>
/// <seealso cref="PipelineState"/>
/// <seealso cref="PipelineStateDescription"/>
internal readonly struct PipelineStateDescriptionWithHash(PipelineStateDescription state) : IEquatable<PipelineStateDescriptionWithHash>
{
    public readonly int Hash = state.GetHashCode();
    public readonly PipelineStateDescription State = state;


    /// <inheritdoc/>
    public readonly bool Equals(PipelineStateDescriptionWithHash other)
    {
        return Hash == other.Hash
            && (State is null) == (other.State is null)
            && (State?.Equals(other.State) ?? true);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is PipelineStateDescriptionWithHash other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Hash;

    public static bool operator ==(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
    {
        return !left.Equals(right);
    }
}
