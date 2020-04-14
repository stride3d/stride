// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Rendering;
using Xenko.Graphics;

namespace Xenko.Shaders
{
    /// <summary>
    /// Binding to a sampler.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("SamplerState {Key} ({Description.Filter})")]
    public class EffectSamplerStateBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectSamplerStateBinding"/> class.
        /// </summary>
        public EffectSamplerStateBinding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectSamplerStateBinding"/> class.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="description">The description.</param>
        public EffectSamplerStateBinding(string keyName, SamplerStateDescription description)
        {
            KeyName = keyName;
            Description = description;
        }

        /// <summary>
        /// The key used to bind this sampler, used internaly.
        /// </summary>
        [DataMemberIgnore]
        public ParameterKey Key;

        /// <summary>
        /// The key name.
        /// </summary>
        public string KeyName;

        /// <summary>
        /// The description of this sampler.
        /// </summary>
        public SamplerStateDescription Description;
    }
}
