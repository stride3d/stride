// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;

namespace Xenko.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Base interface for all computer color nodes.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class ComputeKeyedBase : ComputeNode
    {
        /// <summary>
        /// Gets or sets a custom key associated to this node.
        /// </summary>
        /// <value>The key.</value>
        [DataMemberIgnore]
        [DefaultValue(null)]
        public ParameterKey Key { get; set; }

        /// <summary>
        /// Gets or sets the used key.
        /// </summary>
        /// <value>The used key.</value>
        [DataMemberIgnore]
        public ParameterKey UsedKey { get; set; }
    }
}
