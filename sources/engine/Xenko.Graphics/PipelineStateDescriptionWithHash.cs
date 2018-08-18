// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    internal struct PipelineStateDescriptionWithHash : IEquatable<PipelineStateDescriptionWithHash>
    {
        public readonly int Hash;
        public readonly PipelineStateDescription State;

        public PipelineStateDescriptionWithHash(PipelineStateDescription state)
        {
            Hash = state.GetHashCode();
            State = state;
        }

        public bool Equals(PipelineStateDescriptionWithHash other)
        {
            return Hash == other.Hash && State.Equals(other.State);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PipelineStateDescriptionWithHash && Equals((PipelineStateDescriptionWithHash)obj);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        public static bool operator ==(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PipelineStateDescriptionWithHash left, PipelineStateDescriptionWithHash right)
        {
            return !left.Equals(right);
        }
    }
}
