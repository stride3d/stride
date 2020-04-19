// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Convertor
{
    struct SamplerTextureKey : IEquatable<SamplerTextureKey>
    {
        public Variable Sampler;
        public Variable Texture;

        public SamplerTextureKey(Variable sampler, Variable texture)
        {
            Sampler = sampler;
            Texture = texture;
        }

        public bool Equals(SamplerTextureKey other)
        {
            if (Sampler == null && other.Sampler == null)
                return Texture.Equals(other.Texture);
            if (Sampler == null || other.Sampler == null)
                return false;
            return Sampler.Equals(other.Sampler) && Texture.Equals(other.Texture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SamplerTextureKey && Equals((SamplerTextureKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashcode = Sampler == null ? 1 : Sampler.GetHashCode();
                return (hashcode*397) ^ Texture.GetHashCode();
            }
        }
    }
}
