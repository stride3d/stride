// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.SpriteStudio.Runtime
{
    [DataContract("SpriteStudioNodeLinkComponent")]
    [Display("SpriteStudio node link", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SpriteStudioNodeLinkProcessor))]
    [ComponentOrder(1400)]
    [ComponentCategory("Sprites")]
    public sealed class SpriteStudioNodeLinkComponent : EntityComponent
    {
        /// <summary>
        /// Gets or sets the model which contains the hierarchy to use.
        /// </summary>
        /// <value>
        /// The model which contains the hierarchy to use.
        /// </value>
        /// <userdoc>The reference to the target entity to which attach the current entity. If null, parent will be used.</userdoc>
        [Display("Target (Parent if not set)")]
        public SpriteStudioComponent Target { get; set; }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        /// <value>
        /// The name of the node.
        /// </value>
        /// <userdoc>The name of node of the model of the target entity to which attach the current entity.</userdoc>
        public string NodeName { get; set; }
    }
}
