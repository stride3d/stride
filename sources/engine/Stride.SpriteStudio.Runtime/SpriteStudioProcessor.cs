// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.SpriteStudio.Runtime
{
    public class SpriteStudioProcessor : EntityProcessor<SpriteStudioComponent>
    {
        protected override void OnEntityComponentAdding(Entity entity, SpriteStudioComponent component, SpriteStudioComponent data)
        {
            PrepareNodes(data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SpriteStudioComponent component, SpriteStudioComponent data)
        {
            data.Nodes.Clear();
        }

        public override void Update(GameTime time)
        {
            foreach (var data in ComponentDatas)
            {
                var spriteStudioComponent = data.Value;
                if (!spriteStudioComponent.Enabled) continue;

                spriteStudioComponent.ValidState = PrepareNodes(spriteStudioComponent);
            }
        }

        public override void Draw(RenderContext context)
        {
            foreach (var componentData in ComponentDatas)
            {
                var component = componentData.Value;
                if (!component.ValidState) continue;
                component.RootNode.UpdateTransformation();
            }
        }

        internal static bool PrepareNodes(SpriteStudioComponent component)
        {
            var sheet = component.Sheet;
            if (component.CurrentSheet != sheet) // sheet changed? force pre-process
            {
                component.RootNode = null;
                component.Nodes.Clear();
            }

            var assetNodes = sheet?.NodesInfo;
            if (assetNodes == null) return false;

            if (component.RootNode == null)
            {
                component.RootNode = InitializeNodes(component);
                component.CurrentSheet = sheet;
            }

            return (component.RootNode != null);
        }

        private static SpriteStudioNodeState InitializeNodes(SpriteStudioComponent spriteStudioComponent)
        {
            var nodes = spriteStudioComponent.Sheet?.NodesInfo;
            if (nodes == null)
                return null;

            //check if the sheet name dictionary has already been populated
            if (spriteStudioComponent.Sheet.Sprites == null)
            {
                spriteStudioComponent.Sheet.Sprites = new Sprite[spriteStudioComponent.Sheet.SpriteSheet.Sprites.Count];
                for (int i = 0; i < spriteStudioComponent.Sheet.SpriteSheet.Sprites.Count; i++)
                {
                    spriteStudioComponent.Sheet.Sprites[i] = spriteStudioComponent.Sheet.SpriteSheet.Sprites[i];
                }
            }

            foreach (var node in nodes)
            {
                var nodeState = new SpriteStudioNodeState
                {
                    Position = node.BaseState.Position,
                    RotationZ = node.BaseState.RotationZ,
                    Priority = node.BaseState.Priority,
                    Scale = node.BaseState.Scale,
                    Transparency = node.BaseState.Transparency,
                    Hide = node.BaseState.Hide,
                    BaseNode = node,
                    HFlipped = node.BaseState.HFlipped,
                    VFlipped = node.BaseState.VFlipped,
                    SpriteId = node.BaseState.SpriteId,
                    BlendColor = node.BaseState.BlendColor,
                    BlendType = node.BaseState.BlendType,
                    BlendFactor = node.BaseState.BlendFactor
                };

                nodeState.Sprite = nodeState.SpriteId != -1 ? spriteStudioComponent.Sheet.Sprites[nodeState.SpriteId] : null;

                spriteStudioComponent.Nodes.Add(nodeState);
            }

            SpriteStudioNodeState rootNode = null;
            for (var i = 0; i < nodes.Count; i++)
            {
                var nodeState = spriteStudioComponent.Nodes[i];
                var nodeAsset = nodes[i];

                if (nodeAsset.ParentId == -1)
                {
                    rootNode = nodeState;
                }
                else
                {
                    nodeState.ParentNode = spriteStudioComponent.Nodes.FirstOrDefault(x => x.BaseNode.Id == nodeAsset.ParentId);
                }

                foreach (var subNode in spriteStudioComponent.Nodes.Where(subNode => subNode.BaseNode.ParentId == nodeAsset.Id))
                {
                    nodeState.ChildrenNodes.Add(subNode);
                }
            }

            return rootNode;
        }
    }
}
