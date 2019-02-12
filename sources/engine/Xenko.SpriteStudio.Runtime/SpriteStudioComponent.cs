// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Engine.Design;
using Xenko.Core.Serialization;
using Xenko.Rendering;
using Xenko.SpriteStudio.Runtime;
using Xenko.Updater;

namespace Xenko.Engine
{
    [DataContract("SpriteStudioComponent")]
    [Display("SpriteStudio", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SpriteStudioProcessor))]
    [DefaultEntityComponentRenderer(typeof(SpriteStudioRendererProcessor))]
    [DataSerializerGlobal(null, typeof(List<SpriteStudioNodeState>))]
    [ComponentOrder(9900)]
    public sealed class SpriteStudioComponent : ActivableEntityComponent
    {
        [DataMember(1)]
        public SpriteStudioSheet Sheet { get; set; }

        /// <summary>
        /// The render group for this component.
        /// </summary>
        [DataMember(10)]
        [Display("Render group")]
        [DefaultValue(RenderGroup.Group0)]
        public RenderGroup RenderGroup { get; set; }

        [DataMemberIgnore]
        public SpriteStudioNodeState RootNode;

        [DataMemberIgnore]
        public SpriteStudioSheet CurrentSheet;

        [DataMemberIgnore]
        public bool ValidState;

        [DataMemberIgnore, DataMemberUpdatable]
        public List<SpriteStudioNodeState> Nodes { get; } = new List<SpriteStudioNodeState>();
    }
}
