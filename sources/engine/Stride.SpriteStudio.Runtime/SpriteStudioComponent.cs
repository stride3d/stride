// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.SpriteStudio.Runtime;
using Stride.Updater;

namespace Stride.Engine
{
    [DataContract("SpriteStudioComponent")]
    [Display("SpriteStudio", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SpriteStudioProcessor))]
    [DefaultEntityComponentRenderer(typeof(SpriteStudioRendererProcessor))]
    [DataSerializerGlobal(null, typeof(List<SpriteStudioNodeState>))]
    [ComponentOrder(9900)]
    [ComponentCategory("Sprites")]
    public sealed class SpriteStudioComponent : ActivableEntityComponent, IEntityComponentBounds
    {
        [DataMember(1)]
        public SpriteStudioSheet Sheet { get; set; }

        /// <inheritdoc />
        /// <remarks>The union of the visible node sprites in their current pose.</remarks>
        [DataMemberIgnore]
        public BoundingBox LocalBounds
        {
            get
            {
                if (!SpriteStudioProcessor.PrepareNodes(this))
                    return BoundingBox.Empty;

                RootNode.UpdateTransformation();

                var bounds = BoundingBox.Empty;
                foreach (var node in Nodes)
                {
                    if (node.Sprite == null || node.Hide != 0)
                        continue;

                    var halfSize = new Vector3(node.Sprite.Size.X / 2f, node.Sprite.Size.Y / 2f, 0f);
                    var nodeBounds = new BoundingBoxExt(-halfSize, halfSize);
                    nodeBounds.Transform(node.ModelTransform);
                    bounds = BoundingBox.Merge(bounds, new BoundingBox(nodeBounds.Minimum, nodeBounds.Maximum));
                }
                return bounds;
            }
        }

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
