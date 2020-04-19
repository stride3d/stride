// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Rendering;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Represents an effect compile request done to the <see cref="EffectSystem"/>.
    /// </summary>
    [DataContract("EffectCompileRequest")]
    [DataSerializerGlobal(null, typeof(KeyValuePair<EffectCompileRequest, bool>))]
    [NonIdentifiableCollectionItems]
    public class EffectCompileRequest : IEquatable<EffectCompileRequest>
    {
        public string EffectName;
        public CompilerParameters UsedParameters;

        public EffectCompileRequest()
        {
        }

        public EffectCompileRequest(string effectName, CompilerParameters usedParameters)
        {
            EffectName = effectName;
            UsedParameters = usedParameters;
        }

        public bool Equals(EffectCompileRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(EffectName, other.EffectName) && ShaderMixinObjectId.Compute(EffectName, UsedParameters) == ShaderMixinObjectId.Compute(other.EffectName, other.UsedParameters);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EffectCompileRequest)obj);
        }

        public override int GetHashCode()
        {
            return ShaderMixinObjectId.Compute(EffectName, UsedParameters).GetHashCode();
        }
    }
}
